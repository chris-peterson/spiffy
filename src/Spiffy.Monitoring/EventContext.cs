using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Spiffy.Monitoring
{
    public class EventContext : IDisposable
    {
        public EventContext(string component, string operation) 
        {
            GlobalEventContext.Instance.CopyTo(this);
            _timestamp = DateTime.UtcNow;

            SetToInfo();
            Initialize(component, operation);
            // reserve this spot for later...
            this["TimeElapsed"] = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public EventContext() : this(null, null)
        {
            string component = "[Unknown]";
            string operation = "[Unknown]";

            StackFrame stackFrame = null;

#if NET4_0
            stackFrame = new StackFrame(1, false);
#else
            var stackTrace = (StackTrace)Activator.CreateInstance(typeof(StackTrace));
            var frames = stackTrace.GetFrames();

            foreach (var f in frames)
            {
                var assembly = f.GetMethod().DeclaringType.GetTypeInfo().Assembly;
                if (!FrameworkAssembly(assembly))
                {
                    stackFrame = f;
                    break;
                }
            }
#endif
            var method = stackFrame?.GetMethod();
            if (method != null)
            {
                var declaringType = method.DeclaringType;
                if (declaringType != null)
                {
                    component = declaringType.Name;
                }
                operation = method.Name;
            }

            Initialize(component, operation);
        }

        protected bool FrameworkAssembly(Assembly assembly)
        {
            return assembly == AssemblyFor<object>() ||
               assembly == AssemblyFor<EventContext>();
        }

        protected Assembly AssemblyFor<T>()
        {
#if NET4_0
            return typeof(T).Assembly;
#else
            return typeof(T).GetTypeInfo().Assembly;
#endif
        }

        public string Component { get; protected set; }
        public string Operation { get; protected set; }
        public Level Level { get; protected set; }

        internal readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        internal readonly Dictionary<string, AutoTimer> _timers = new Dictionary<string, AutoTimer>();

        internal readonly object _valuesSyncObject = new object();
        internal readonly object _timersSyncObject = new object();
        
        internal readonly DateTime _timestamp;
        internal readonly AutoTimer _timer = new AutoTimer();

        public IDisposable Time(string key)
        {
            key = string.Format("TimeElapsed_{0}", key);

            lock (_timersSyncObject)
            {
                if (!_timers.ContainsKey(key))
                {
                    _timers[key] = new AutoTimer();
                }

                return _timers[key];
            }
        }

        public object this[string key]
        {
            get
            {
                lock (_valuesSyncObject)
                {
                    return _values[key];
                }
            }
            set
            {
                lock (_valuesSyncObject)
                {
                    _values[key] = value;
                }
            }
        }

        public bool Contains(string key)
        {
            return _values.ContainsKey(key);
        }

        public void AppendToValue(string key, string content, string delimiter)
        {
            lock (_valuesSyncObject)
            {
                if (_values.ContainsKey(key))
                {
                    _values[key] = string.Join(delimiter,
                        new[] {_values[key].ToString(), content});
                }
                else
                {
                    _values.Add(key, content);
                }
            }
        }

        public void SetLevel(Level level)
        {
            this["Level"] = Level = level;
        }

        public void SetToInfo()
        {
            SetLevel(Level.Info);
        }

        public void SetToError(string reason = null)
        {
            SetLevel(Level.Error);
            if (reason != null)
            {
                this["ErrorReason"] = reason;
            }
        }

        public void SetToWarning(string reason = null)
        {
            SetLevel(Level.Warning);
            if (reason != null)
            {
                this["WarningReason"] = reason;
            }
        }

        protected volatile bool _disposed = false;
        protected virtual void Dispose(bool disposing) {
            if (!_disposed)
            {
                // Base class has nothing special to dispose, so disposing value is not used
                this["TimeElapsed"] = GetTimeFor(_timer.TotalMilliseconds);
                DefaultLoggingFacade.Instance.Log(Level, GetFormattedMessage());
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EventContext() {
            Dispose(false);
        }

        public void Initialize(string component, string operation)
        {
            Component = component;
            Operation = operation;
            this["Component"] = Component;
            this["Operation"] = Operation;
        }

        protected virtual string GetFormattedMessage(string loggerId = null)
        {
            Dictionary<string, string> kvps;
            
            lock (_valuesSyncObject)
            {
                kvps = _values.ToDictionary(
                    kvp => kvp.Key,
                    kvp => GetValue(kvp.Value));
            }

            foreach (var kvp in GetTimeValues())
            {
                kvps.Add(kvp.Key, kvp.Value);
            }

            if(!string.IsNullOrWhiteSpace(loggerId)) {
                kvps.Add("loggerId", loggerId);
            }

            GenerateKeysIfNecessary(kvps);

            ReplaceKeysThatHaveSpaces(kvps);
            ReplaceKeysThatHaveDots(kvps);
            EncapsulateValuesIfNecessary(kvps);

            return string.Format("{0} {1}",
                                 GetSplunkFormattedTime(),
                                 GetKeyValuePairsAsDelimitedString(kvps));
        }

        protected static void EncapsulateValuesIfNecessary(Dictionary<string, string> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs
                .Where(k => !k.Value.StartsWithQuote()
                    && (k.Value.ContainsWhitespace() || k.Value.Contains('&')))
                .ToList())
            {
                keyValuePairs[kvp.Key] = kvp.Value.WrappedInQuotes();
            }
        }

        protected static void ReplaceKeysThatHaveSpaces(Dictionary<string, string> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs
                .Where(k => k.Key.ContainsWhitespace())
                .ToList())
            {
                keyValuePairs.Remove(kvp.Key);
                keyValuePairs[kvp.Key.RemoveWhitespace()] = kvp.Value;
            }
        }

        protected static void ReplaceKeysThatHaveDots(Dictionary<string, string> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs
                .Where(k => k.Key.Contains("."))
                .ToList())
            {
                keyValuePairs.Remove(kvp.Key);
                keyValuePairs[kvp.Key.Replace(".", "_")] = kvp.Value;
            }
        }

        protected void GenerateKeysIfNecessary(Dictionary<string, string> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs
                .Where(k => k.Key.IsNullOrWhiteSpace())
                .ToList())
            {
                keyValuePairs.Remove(kvp.Key);
                keyValuePairs[string.Format("GeneratedKey({0})", Guid.NewGuid())] = kvp.Value;
            }
        }

        protected static string GetKeyValuePairsAsDelimitedString(Dictionary<string, string> keyValuePairs)
        {
            return string.Join(" ", keyValuePairs.Select(kvp =>
                string.Format("{0}={1}", kvp.Key, kvp.Value)).ToArray());
        }

        protected static string GetValue(object value)
        {
            if (value == null)
            {
                return "{null}";
            }

            var valueStr = value.ToString();

            // there are a certain few fields that Splunk chokes on in values.
            // escape them individually to minimize visual noise (as opposed to doing a full encode) 
            valueStr = valueStr.Replace("=", ":");
            valueStr = valueStr.Replace("\"", "''");
            return valueStr;
        }

        protected string GetSplunkFormattedTime()
        {
            return _timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffK").WrappedInBrackets();
        }

        protected IEnumerable<KeyValuePair<string, string>> GetTimeValues()
        {
            Dictionary<string, string> timings;

            lock (_timersSyncObject)
            {
                timings = _timers.ToDictionary(
                    kvp => kvp.Key,
                    kvp => GetTimeFor(kvp.Value.TotalMilliseconds));
            }

            return timings;
        }

        protected static string GetTimeFor(double milliseconds)
        {
            return string.Format("{0:F1}", milliseconds);
        }
    }
}
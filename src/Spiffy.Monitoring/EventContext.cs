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

        public EventContext() : this(null, null)
        {
            string component = "[Unknown]";
            string operation = "[Unknown]";

            var stackTrace = (StackTrace)Activator.CreateInstance(typeof(StackTrace));
            var frames = stackTrace.GetFrames();

            StackFrame stackFrame = null;
            foreach (var f in frames)
            {
                var assembly = f.GetMethod().DeclaringType.GetTypeInfo().Assembly;
                if (!FrameworkAssembly(assembly))
                {
                    stackFrame = f;
                    break;
                }
            }

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

        bool FrameworkAssembly(Assembly assembly)
        {
            return assembly == typeof(Activator).GetTypeInfo().Assembly || 
               assembly == typeof(EventContext).GetTypeInfo().Assembly;
        }

        public string Component { get; private set; }
        public string Operation { get; private set; }
        public Level Level { get; private set; }

        readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        readonly Dictionary<string, AutoTimer> _timers = new Dictionary<string, AutoTimer>();

        readonly object _valuesSyncObject = new object();
        readonly object _timersSyncObject = new object();
        
        readonly DateTime _timestamp;
        readonly AutoTimer _timer = new AutoTimer();

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

        private void SetToInfo()
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

        void SetLevel(Level level)
        {
            this["Level"] = Level = level;
        }

        public void Dispose()
        {
            this["TimeElapsed"] = GetTimeFor(_timer.TotalMilliseconds);
            LoggingFacade.Log(Level, GetFormattedMessage());
        }

        private void Initialize(string component, string operation)
        {
            Component = component;
            Operation = operation;
            this["Component"] = Component;
            this["Operation"] = Operation;
        }

        private string GetFormattedMessage()
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

            GenerateKeysIfNecessary(kvps);

            ReplaceKeysThatHaveSpaces(kvps);
            ReplaceKeysThatHaveDots(kvps);
            EncapsulateValuesIfNecessary(kvps);

            return string.Format("{0} {1}",
                                 GetSplunkFormattedTime(),
                                 GetKeyValuePairsAsDelimitedString(kvps));
        }

        private static void EncapsulateValuesIfNecessary(Dictionary<string, string> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs
                .Where(k => !k.Value.StartsWithQuote()
                    && (k.Value.ContainsWhitespace() || k.Value.Contains('&')))
                .ToList())
            {
                keyValuePairs[kvp.Key] = kvp.Value.WrappedInQuotes();
            }
        }

        private static void ReplaceKeysThatHaveSpaces(Dictionary<string, string> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs
                .Where(k => k.Key.ContainsWhitespace())
                .ToList())
            {
                keyValuePairs.Remove(kvp.Key);
                keyValuePairs[kvp.Key.RemoveWhitespace()] = kvp.Value;
            }
        }

        private static void ReplaceKeysThatHaveDots(Dictionary<string, string> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs
                .Where(k => k.Key.Contains("."))
                .ToList())
            {
                keyValuePairs.Remove(kvp.Key);
                keyValuePairs[kvp.Key.Replace(".", "_")] = kvp.Value;
            }
        }

        private void GenerateKeysIfNecessary(Dictionary<string, string> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs
                .Where(k => k.Key.IsNullOrWhiteSpace())
                .ToList())
            {
                keyValuePairs.Remove(kvp.Key);
                keyValuePairs[string.Format("GeneratedKey({0})", Guid.NewGuid())] = kvp.Value;
            }
        }

        private static string GetKeyValuePairsAsDelimitedString(Dictionary<string, string> keyValuePairs)
        {
            return string.Join(" ", keyValuePairs.Select(kvp =>
                string.Format("{0}={1}", kvp.Key, kvp.Value)).ToArray());
        }

        private static string GetValue(object value)
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

        private string GetSplunkFormattedTime()
        {
            return _timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffK").WrappedInBrackets();
        }

        private IEnumerable<KeyValuePair<string, string>> GetTimeValues()
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

        private static string GetTimeFor(double milliseconds)
        {
            return string.Format("{0:F1}", milliseconds);
        }
    }
}
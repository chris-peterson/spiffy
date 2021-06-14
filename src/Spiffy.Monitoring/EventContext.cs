using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Spiffy.Monitoring
{
    partial class EventContext : ITimedContext
    {
        const string TimeElapsedKey = "TimeElapsed";
        public EventContext(string component, string operation) 
        {
            GlobalEventContext.Instance.CopyTo(this);
            _timestamp = DateTime.UtcNow;

            SetToInfo();
            Initialize(component, operation);
            // reserve this spot for later...
            this[TimeElapsedKey] = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public EventContext() : this(null, null)
        {
            string component = "[Unknown]";
            string operation = "[Unknown]";

            StackFrame stackFrame = null;

            var stackTrace = (StackTrace)Activator.CreateInstance(typeof(StackTrace));
            var frames = stackTrace.GetFrames();

            foreach (var f in frames)
            {
                var assembly = f.GetMethod().DeclaringType?.GetTypeInfo().Assembly;
                if (assembly != null && !FrameworkAssembly(assembly))
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

        public double ElapsedMilliseconds => _timer.ElapsedMilliseconds;

        bool FrameworkAssembly(Assembly assembly)
        {
            return assembly == AssemblyFor<object>() ||
               assembly == AssemblyFor<EventContext>();
        }

        Assembly AssemblyFor<T>()
        {
            return typeof(T).GetTypeInfo().Assembly;
        }

        public string Component { get; private set; }
        public string Operation { get; private set; }
        public Level Level { get; private set; }

        readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        readonly Dictionary<string, uint> _counts = new Dictionary<string, uint>();

        readonly object _valuesSyncObject = new object();
        readonly object _countsSyncObject = new object();
        
        readonly DateTime _timestamp;
        readonly AutoTimer _timer = new AutoTimer();

        public IDisposable Time(string key)
        {
            return Timers.Accumulate(key);
        }

        public TimerCollection Timers { get; } = new TimerCollection();

        public void Count(string key)
        {
            lock (_countsSyncObject)
            {
                if (_counts.ContainsKey(key))
                {
                    _counts[key]++;
                }
                else
                {
                    _counts[key] = 1;
                }
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

        /// <summary>
        /// Data that is attached to the EventContext but is not published.  This could be useful for
        /// stashing relevant contextual information, e.g. metric labels.
        /// </summary>
        public ConcurrentDictionary<string, string> PrivateData { get; } = new ConcurrentDictionary<string, string>();

        public void AddValues(params KeyValuePair<string, object>[] values)
        {
            lock (_valuesSyncObject)
            {
                foreach (var kvp in values)
                {
                    _values[kvp.Key] = kvp.Value;
                }
            }
        }

        public void AddValues(IEnumerable<KeyValuePair<string, object>> values)
        {
            AddValues(values.ToArray());
        }

        public bool Contains(string key)
        {
            lock (_valuesSyncObject)
            {
                return _values.ContainsKey(key);
            }
        }

        public void AppendToValue(string key, string content, string delimiter)
        {
            lock (_valuesSyncObject)
            {
                if (_values.ContainsKey(key))
                {
                    _values[key] = string.Join(delimiter, _values[key].ToString(), content);
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

        public bool IsSuppressed { get; private set; }

        public void Suppress()
        {
            IsSuppressed = true;
        }

        volatile bool _disposed = false;

        public void Dispose()
        {
            if (!_disposed)
            {
                if(!IsSuppressed)
                {
                    var logActions = Configuration.GetLoggingActions();

                    if (logActions.Any())
                    {
                        var logEvent = Render();
                        foreach (var logAction in logActions)
                        {
                            try
                            {
                                logAction(logEvent);
                            }
                            // ReSharper disable once EmptyGeneralCatchClause -- intentionally squashed
                            catch
                            {
                            }
                        }
                    }
                }
                _disposed = true;
            }
        }

        public void Initialize(string component, string operation)
        {
            Component = component;
            Operation = operation;
            this["Component"] = Component;
            this["Operation"] = Operation;
        }

        private LogEvent Render()
        {
            Dictionary<string, string> kvps;
            
            lock (_valuesSyncObject)
            {
                kvps = _values.ToDictionary(
                    kvp => kvp.Key,
                    kvp => GetValue(kvp.Value));
            }

            foreach (var kvp in GetCountValues())
            {
                kvps.Add(kvp.Key, kvp.Value);
            }

            foreach (var kvp in GetTimeValues())
            {
                kvps.Add(kvp.Key, kvp.Value);
            }

            GenerateKeysIfNecessary(kvps);

            ReplaceKeysThatHaveWhiteSpace(kvps);
            ReplaceKeysThatHaveDots(kvps);
            EncapsulateValuesIfNecessary(kvps);

            var timeElapsedMs = _timer.ElapsedMilliseconds;
            var formattedTimeElapsed = GetTimeFor(timeElapsedMs);
            this[TimeElapsedKey] = formattedTimeElapsed;
            kvps[TimeElapsedKey] = formattedTimeElapsed;

            return new LogEvent(
                Level,
                _timestamp,
                TimeSpan.FromMilliseconds(timeElapsedMs),
                GetSplunkFormattedTime(),
                GetKeyValuePairsAsDelimitedString(kvps),
                kvps,
                PrivateData);
        }

        private static void EncapsulateValuesIfNecessary(Dictionary<string, string> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs
                .Where(k => !k.Value.StartsWithQuote() && (
                    k.Value.ContainsWhiteSpace() ||
                    k.Value.Contains(',') ||
                    k.Value.Contains('&')))
                .ToList())
            {
                keyValuePairs[kvp.Key] = kvp.Value.WrappedInQuotes();
            }
        }

        private static void ReplaceKeysThatHaveWhiteSpace(Dictionary<string, string> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs
                .Where(k => k.Key.ContainsWhiteSpace())
                .ToList())
            {
                keyValuePairs.Remove(kvp.Key);
                keyValuePairs[kvp.Key.RemoveWhiteSpace()] = kvp.Value;
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
            return string.Join(" ", keyValuePairs
                // Values with length over DeprioritizedValueLength will come later (true > false).
                .OrderBy(pair => pair.Value.Length > Configuration.DeprioritizedValueLength)
                .Select(kvp =>
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

            if (Configuration.RemoveNewLines)
            {
                valueStr = valueStr
                    .Replace("\r", String.Empty)
                    .Replace("\n", "\\n");
            }

            return valueStr;
        }

        private string GetSplunkFormattedTime()
        {
            return _timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffK").WrappedInBrackets();
        }

        private IEnumerable<KeyValuePair<string, string>> GetCountValues()
        {
            var counts = new Dictionary<string, string>();

            lock (_countsSyncObject)
            {
                foreach (var kvp in _counts)
                {
                    counts[$"{kvp.Key}"] = kvp.Value.ToString();
                }
            }

            return counts;
        }

        private IEnumerable<KeyValuePair<string, string>> GetTimeValues()
        {
            var times = new Dictionary<string, string>();

            foreach (var kvp in Timers.ShallowClone())
            {
                times[$"{TimeElapsedKey}_{kvp.Key}"] = GetTimeFor(kvp.Value.ElapsedMilliseconds);
                if (kvp.Value.Count > 1)
                {
                    times[$"Count_{kvp.Key}"] = kvp.Value.Count.ToString();
                }
            }

            return times;
        }

        private static string GetTimeFor(double milliseconds)
        {
            return $"{milliseconds:F1}";
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Spiffy.Monitoring
{
    partial class EventContext : ITimedContext
    {
        const string TimeElapsedKey = "TimeElapsed";
        public EventContext(string component, string operation) 
        {
            SetToInfo();

            GlobalEventContext.Instance.CopyTo(this);
            _timestamp = DateTime.UtcNow;

            Initialize(component, operation);
            // initialize time elapsed field so that it shows up in a consistent order in log entries
            this[TimeElapsedKey] = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public EventContext() : this(null, null)
        {
            Assembly AssemblyFor<T>() => typeof(T).GetTypeInfo().Assembly;
            bool FrameworkAssembly(Assembly assembly) =>
                assembly == AssemblyFor<object>() ||
                assembly == AssemblyFor<EventContext>();

            string component = "[Unknown]";
            string operation = "[Unknown]";

            StackFrame stackFrame = null;

            var stackTrace = EnhancedStackTrace.Current();
            var frames = stackTrace.GetFrames();

            if (frames != null)
            {
                foreach (var f in frames)
                {
                    var assembly = f.GetMethod().DeclaringType?.GetTypeInfo().Assembly;
                    if (assembly != null && !FrameworkAssembly(assembly))
                    {
                        stackFrame = f;
                        break;
                    }
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

        string _component;
        public string Component
        {
            get => _component;
            set => this["Component"] = _component = value;
        }

        string _operation;
        public string Operation
        {
            get => _operation;
            set => this["Operation"] = _operation = value;
        }
        public Level Level { get; private set; }

        readonly ConcurrentDictionary<string, (uint Order, object Value)> _values =
            new ConcurrentDictionary<string, (uint Order, object Value)>();
        readonly ConcurrentDictionary<string, (uint Order, uint Value)> _counts =
            new ConcurrentDictionary<string, (uint Order, uint Value)>();

        readonly DateTime _timestamp;
        DateTime? _customTimestamp;
        public DateTime? CustomTimestamp { set {_customTimestamp = value; } }
        private DateTime Timestamp => _customTimestamp ?? _timestamp;

        readonly AutoTimer _timer = new AutoTimer();
        volatile uint _fieldCounter;

        public IDisposable Time(string key)
        {
            return Timers.Accumulate(key);
        }

        public TimerCollection Timers { get; } = new TimerCollection();

        public void Count(string key)
        {
            _counts.AddOrUpdate(
                key, (GetNextFieldCounter(), 1),
                (k, v) => (v.Order, v.Value + 1)
            );
        }

        public object this[string key]
        {
            get => _values[key].Value;
            set => Set(key, value);
        }

        public void Set(string key, object value, FieldConflict behavior = FieldConflict.Overwrite)
        {
            switch (behavior)
            {
                case FieldConflict.Overwrite:
                    _values.AddOrUpdate(key, (GetNextFieldCounter(), value),
                        (k, existingValue) => (existingValue.Order,
                            value));
                    break;
                case FieldConflict.Ignore:
                    _values.GetOrAdd(key, (GetNextFieldCounter(), value));
                    break;
            }
        }

        public void TrySet(string key, Func<object> valueFunction, FieldConflict behavior = FieldConflict.Overwrite)
        {
            try
            {
                var value = valueFunction();
                Set(key, value, behavior);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Data that is attached to the EventContext but is not published.  This could be useful for
        /// stashing relevant contextual information, e.g. metric labels.
        /// </summary>
        public ConcurrentDictionary<string, string> PrivateData { get; } = new ConcurrentDictionary<string, string>();

        public void AddValues(params KeyValuePair<string, object>[] values)
        {
            foreach (var kvp in values)
            {
                this[kvp.Key] = kvp.Value;
            }
        }

        public void AddValues(IEnumerable<KeyValuePair<string, object>> values)
        {
            AddValues(values.ToArray());
        }

        public bool Contains(string key)
        {
            return _values.ContainsKey(key);
        }

        public void AppendToValue(string key, string content, string delimiter)
        {
            _values.AddOrUpdate(key, (GetNextFieldCounter(), content),
                (k, existingValue) => (existingValue.Order,
                    string.Join(delimiter, existingValue.Value?.ToString(), content)));
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

        // ReSharper disable once RedundantDefaultMemberInitializer
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
        }

        internal LogEvent Render()
        {
            var kvps = _values
                .OrderBy(x => x.Value.Order)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => GetValue(kvp.Value.Value));


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
                Timestamp,
                TimeSpan.FromMilliseconds(timeElapsedMs),
                GetSplunkFormattedTime(),
                GetKeyValuePairsAsDelimitedString(kvps),
                kvps,
                PrivateData);
        }

        static void EncapsulateValuesIfNecessary(IDictionary<string, string> keyValuePairs)
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
                keyValuePairs[$"GeneratedKey({Guid.NewGuid()})"] = kvp.Value;
            }
        }

        private static string GetKeyValuePairsAsDelimitedString(Dictionary<string, string> keyValuePairs)
        {
            return string.Join(" ", keyValuePairs
                .OrderBy(pair => pair.Value.Length <= Configuration.DeprioritizedValueLength ? 0 : 1)
                .Select(kvp =>
                    $"{kvp.Key}={kvp.Value}").ToArray());
        }

        static string GetValue(object value)
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
            return Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffK").WrappedInBrackets();
        }

        private IDictionary<string, string> GetCountValues()
        {
            return _counts.ToDictionary(
                k => k.Key,
                v => v.Value.Value.ToString());
        }

        private IEnumerable<KeyValuePair<string, string>> GetTimeValues()
        {
            var times = new Dictionary<string, string>();

            foreach (var kvp in Timers
                .ShallowClone()
                .OrderBy(x => x.Key))
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

        uint GetNextFieldCounter()
        {
            unchecked
            {
                return _fieldCounter++;
            }
        }
    }
}

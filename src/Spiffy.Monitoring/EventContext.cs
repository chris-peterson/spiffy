using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Spiffy.Monitoring.Config.Formatting;
using Spiffy.Monitoring.Config.Naming;

namespace Spiffy.Monitoring
{
    partial class EventContext : ITimedContext
    {
        private readonly Configuration _config;

        internal EventContext(string component, string operation, Configuration config)
        {
            _config = config ?? Configuration.Default;

            SetToInfo();

            GlobalEventContext.Instance.CopyTo(this);
            _timestamp = DateTime.UtcNow;

            Initialize(component, operation);
            // initialize time elapsed field so that it shows up in a consistent order in log entries
            this[FieldName.Get(Field.TimeElapsed)] = 0;
        }

        public EventContext() : this(null, null)
        {
            var enhancedStackTrace = new EnhancedStackTrace(new StackTrace(skipFrames: 1));

            var caller = enhancedStackTrace
                .First(f => f.MethodInfo.DeclaringType != null)
                .MethodInfo;

            Initialize(caller.DeclaringType.Name, caller.Name);
        }

        public EventContext(string component, string operation) : this(component, operation, null)
        {
        }

        public double ElapsedMilliseconds => _timer.ElapsedMilliseconds;

        public string Component
        {
            get => this[FieldName.Get(Field.Component)] as string;
            set => this[FieldName.Get(Field.Component)] = value;
        }

        public string Operation
        {
            get => this[FieldName.Get(Field.Operation)] as string;
            set => this[FieldName.Get(Field.Operation)] = value;
        }

        public Level Level { get; private set; }

        readonly ConcurrentDictionary<string, (uint Order, object Value)> _values = new();
        readonly ConcurrentDictionary<string, (uint Order, uint Value)> _counts = new();

        readonly DateTime _timestamp;
        DateTime? _customTimestamp;
        public DateTime? CustomTimestamp { set => _customTimestamp = value; }
        private DateTime Timestamp => _customTimestamp ?? _timestamp;

        readonly AutoTimer _timer = new AutoTimer();
        volatile uint _fieldCounter;

        internal IFieldNameLookup FieldName => _config.FieldNameLookup;

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
            get => _values.TryGetValue(key, out var value) ? value.Value : string.Empty;
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
            this[FieldName.Get(Field.Level)] = Level = level;
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
                this[FieldName.Get(Field.ErrorReason)] = reason;
            }
        }

        public void SetToWarning(string reason = null)
        {
            SetLevel(Level.Warning);
            if (reason != null)
            {
                this[FieldName.Get(Field.WarningReason)] = reason;
            }
        }

        public bool IsSuppressed { get; private set; }

        public void Suppress()
        {
            IsSuppressed = true;
        }

        public void SuppressFields(params string [] fields)
        {
            foreach (var field in fields)
            {
                _values.TryRemove(field, out _);
            }
        }

        // ReSharper disable once RedundantDefaultMemberInitializer
        volatile bool _disposed = false;

        public void Dispose()
        {
            if (!_disposed)
            {
                var beforeLoggingActions = _config.BeforeLoggingActions;
                foreach (var action in beforeLoggingActions)
                {
                    action(this);
                }
                if (!IsSuppressed)
                {
                    var logActions = _config.LoggingActions;
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

        private LogEvent Render()
        {
            IEnumerable<KeyValuePair<string, (uint Order, object Value)>> values = _values;
            if (_config.CustomNullValue == null)
            {
                values = values.Where(kvp => kvp.Value.Value != null);
            }
            var kvps = values
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
            this[FieldName.Get(Field.TimeElapsed)] = formattedTimeElapsed;
            kvps[FieldName.Get(Field.TimeElapsed)] = formattedTimeElapsed;
            PrivateData["MetricsKey"] = $"{Component}/{Operation}";

            return new LogEvent(
                Level,
                Timestamp,
                TimeSpan.FromMilliseconds(timeElapsedMs),
                RenderTimestamp(),
                GetKeyValuePairsAsDelimitedString(kvps),
                kvps,
                PrivateData);
        }
        
        void EncapsulateValuesIfNecessary(IDictionary<string, string> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs)
            {
                if (kvp.Value.RequiresEncapsulation(out var preferredQuote))
                {
                    switch (_config.SpecialValueFormatting)
                    {
                        case SpecialValueFormatting.UseAlternateQuotes:
                            keyValuePairs[kvp.Key] = kvp.Value.WrappedInQuotes(preferredQuote);
                            break;
                        case SpecialValueFormatting.UseAndEscapeDoubleQuotes:
                            keyValuePairs[kvp.Key] = kvp.Value.Replace("\"", "\\\"").WrappedInQuotes('"');
                            break;
                    }
                }
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
                .Where(k => string.IsNullOrWhiteSpace(k.Key))
                .ToList())
            {
                keyValuePairs.Remove(kvp.Key);
                keyValuePairs[$"GeneratedKey({Guid.NewGuid()})"] = kvp.Value;
            }
        }

        private string GetKeyValuePairsAsDelimitedString(Dictionary<string, string> keyValuePairs)
        {
            return string.Join(" ", keyValuePairs
                .OrderBy(pair => pair.Value.Length <= _config.DeprioritizedValueLength ? 0 : 1)
                .Select(kvp =>
                    $"{kvp.Key}={kvp.Value}").ToArray());
        }

        string GetValue(object value)
        {
            if (value == null)
            {
                return _config.CustomNullValue;
            }

            var str = value.ToString();

            switch (_config.NewlineFormatting)
            {
                case NewlineFormatting.Remove:
                    str = str
                        .Replace("\r", string.Empty)
                        .Replace("\n", "\\n");
                    break;
                case NewlineFormatting.Preserve:
                default:
                    break;
            }

            return str;
        }

        private string RenderTimestamp()
        {
            string ts = Timestamp.ToString(_config.TimestampFormatString);
        
            switch (_config.TimestampNaming)
            {
                case TimestampNaming.UseUnnamedFieldInBrackets:
                    ts = ts.WrappedInBrackets();
                    break;
                case TimestampNaming.UseTimestampField:
                    ts = $"{FieldName.Get(Field.Timestamp)}={ts}";
                    break;
            }
            return ts;
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
                times[$"{FieldName.Get(Field.TimeElapsed)}_{kvp.Key}"] = GetTimeFor(kvp.Value.ElapsedMilliseconds);
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

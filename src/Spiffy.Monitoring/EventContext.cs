using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
            _timestamp = DateTime.UtcNow;

            SetCore(FieldName.Get(Field.Level), Level = Level.Info);
            GlobalEventContext.Instance.CopyToCore(this);
            SetCore(FieldName.Get(Field.Component), component);
            SetCore(FieldName.Get(Field.Operation), operation);
            SetCore(FieldName.Get(Field.TimeElapsed), 0);
        }

        public EventContext() : this(null, null)
        {
            var stackTrace = new StackTrace(skipFrames: 1);
            var frames = stackTrace.GetFrames();
            if (frames != null)
            {
                foreach (var frame in frames)
                {
                    var method = frame.GetMethod();
                    if (method?.DeclaringType != null)
                    {
                        var caller = ResolveCaller(method);
                        Initialize(caller.Component, caller.Operation);
                        break;
                    }
                }
            }
        }

        // Compiler-generated types (async state machines, lambdas) have a declaring type
        // like "Namespace.OuterClass+<MethodName>d__5". We resolve back to the outer class
        // and original method name.
        static readonly Regex GeneratedTypePattern = new Regex(
            @"^<(\w+)>[a-z]__\d+$", RegexOptions.Compiled);

        static (string Component, string Operation) ResolveCaller(MethodBase method)
        {
            var declaringType = method.DeclaringType;

            if (declaringType.DeclaringType != null)
            {
                var match = GeneratedTypePattern.Match(declaringType.Name);
                if (match.Success)
                {
                    return (declaringType.DeclaringType.Name, match.Groups[1].Value);
                }
            }

            return (declaringType.Name, method.Name);
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

        private const int InitialCapacity = 16;
        private string[] _keys = new string[InitialCapacity];
        private object[] _vals = new object[InitialCapacity];
        private int _count;
        private Dictionary<string, uint> _counts;

        readonly DateTime _timestamp;
        DateTime? _customTimestamp;
        public DateTime? CustomTimestamp { set => _customTimestamp = value; }
        private DateTime Timestamp => _customTimestamp ?? _timestamp;

        readonly AutoTimer _timer = new AutoTimer();

        internal IFieldNameLookup FieldName => _config.FieldNameLookup;

        public IDisposable Time(string key)
        {
            return Timers.Accumulate(key);
        }

        private TimerCollection _timers;
        public TimerCollection Timers => _timers ??= new TimerCollection();

        public void Count(string key)
        {
            lock (this)
            {
                _counts ??= new Dictionary<string, uint>();
                if (_counts.TryGetValue(key, out var val))
                    _counts[key] = val + 1;
                else
                    _counts[key] = 1;
            }
        }

        public object this[string key]
        {
            get
            {
                lock (this)
                {
                    var idx = FindKey(key);
                    return idx >= 0 ? _vals[idx] : string.Empty;
                }
            }
            set => Set(key, value);
        }

        public void Set(string key, object value, FieldConflict behavior = FieldConflict.Overwrite)
        {
            lock (this)
            {
                SetCore(key, value, behavior);
            }
        }

        internal void SetCore(string key, object value, FieldConflict behavior = FieldConflict.Overwrite)
        {
            var idx = FindKey(key);
            switch (behavior)
            {
                case FieldConflict.Overwrite:
                    if (idx >= 0)
                    {
                        _vals[idx] = value;
                    }
                    else
                    {
                        AppendEntry(key, value);
                    }
                    break;
                case FieldConflict.Ignore:
                    if (idx < 0)
                    {
                        AppendEntry(key, value);
                    }
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindKey(string key)
        {
            for (int i = 0; i < _count; i++)
            {
                if (string.Equals(_keys[i], key, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendEntry(string key, object value)
        {
            if (_count == _keys.Length)
            {
                var newLen = _keys.Length * 2;
                var newKeys = new string[newLen];
                var newVals = new object[newLen];
                Array.Copy(_keys, newKeys, _count);
                Array.Copy(_vals, newVals, _count);
                _keys = newKeys;
                _vals = newVals;
            }
            _keys[_count] = key;
            _vals[_count] = value;
            _count++;
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
        private ConcurrentDictionary<string, string> _privateData;
        public ConcurrentDictionary<string, string> PrivateData => _privateData ??= new ConcurrentDictionary<string, string>();

        public void AddValues(params KeyValuePair<string, object>[] values)
        {
            foreach (var kvp in values)
            {
                this[kvp.Key] = kvp.Value;
            }
        }

        public void AddValues(IEnumerable<KeyValuePair<string, object>> values)
        {
            foreach (var kvp in values)
            {
                this[kvp.Key] = kvp.Value;
            }
        }

        public bool Contains(string key)
        {
            lock (this)
            {
                return FindKey(key) >= 0;
            }
        }

        public void AppendToValue(string key, string content, string delimiter)
        {
            lock (this)
            {
                var idx = FindKey(key);
                if (idx >= 0)
                {
                    _vals[idx] = string.Join(delimiter, _vals[idx]?.ToString(), content);
                }
                else
                {
                    AppendEntry(key, content);
                }
            }
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
            lock (this)
            {
                foreach (var field in fields)
                {
                    var idx = FindKey(field);
                    if (idx >= 0)
                    {
                        _keys[idx] = null;
                        _vals[idx] = null;
                    }
                }
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
                    if (logActions.Length > 0)
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

        [ThreadStatic]
        private static StringBuilder t_sb;

        private LogEvent Render()
        {
            var countsCount = _counts?.Count ?? 0;
            var kvps = new Dictionary<string, string>(_count + countsCount + 8);
            var hasCustomNull = _config.CustomNullValue != null;
            for (int i = 0; i < _count; i++)
            {
                var key = _keys[i];
                if (key != null && (hasCustomNull || _vals[i] != null))
                {
                    kvps[NormalizeKey(key)] = GetValue(_vals[i]);
                }
            }

            if (_counts != null)
            {
                foreach (var kvp in _counts)
                {
                    kvps[NormalizeKey(kvp.Key)] = kvp.Value.ToString();
                }
            }

            if (_timers != null)
                GetTimeValues(kvps);

            EncapsulateValuesIfNecessary(kvps);

            var timeElapsedMs = _timer.ElapsedMilliseconds;
            var formattedTimeElapsed = GetTimeFor(timeElapsedMs);
            SetCore(FieldName.Get(Field.TimeElapsed), formattedTimeElapsed);
            kvps[FieldName.Get(Field.TimeElapsed)] = formattedTimeElapsed;
            IDictionary<string, string> privateData = _privateData ?? (IDictionary<string, string>)new Dictionary<string, string>(1);
            privateData["MetricsKey"] = string.Concat(Component, "/", Operation);

            return new LogEvent(
                Level,
                Timestamp,
                TimeSpan.FromMilliseconds(timeElapsedMs),
                RenderTimestamp(),
                GetKeyValuePairsAsDelimitedString(kvps),
                kvps,
                privateData);
        }

        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Concat("GeneratedKey(", Guid.NewGuid().ToString(), ")");

            bool hasWhitespace = false;
            bool hasDots = false;
            for (int i = 0; i < key.Length; i++)
            {
                if (char.IsWhiteSpace(key[i])) hasWhitespace = true;
                else if (key[i] == '.') hasDots = true;
            }

            if (!hasWhitespace && !hasDots) return key;

            if (hasWhitespace)
                key = key.RemoveWhiteSpace();
            if (hasDots)
                key = key.Replace(".", "_");
            return key;
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

        private string GetKeyValuePairsAsDelimitedString(Dictionary<string, string> keyValuePairs)
        {
            var deprioritizedLength = _config.DeprioritizedValueLength;
            var sb = t_sb ??= new StringBuilder(512);
            sb.Clear();

            foreach (var kvp in keyValuePairs)
            {
                if (kvp.Value.Length <= deprioritizedLength)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(kvp.Key).Append('=').Append(kvp.Value);
                }
            }

            foreach (var kvp in keyValuePairs)
            {
                if (kvp.Value.Length > deprioritizedLength)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(kvp.Key).Append('=').Append(kvp.Value);
                }
            }

            return sb.ToString();
        }

        string GetValue(object value)
        {
            if (value == null)
            {
                return _config.CustomNullValue;
            }

            var str = value is string s ? s : value.ToString();

            if (_config.NewlineFormatting == NewlineFormatting.Remove)
            {
                str = str
                    .Replace("\r", string.Empty)
                    .Replace("\n", "\\n");
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

        private void GetTimeValues(Dictionary<string, string> target)
        {
            _timers.WriteTimerValues(target, FieldName.Get(Field.TimeElapsed));
        }

        private static readonly string ZeroTime = "0.0";

        private static string GetTimeFor(double milliseconds)
        {
            return milliseconds == 0.0 ? ZeroTime : milliseconds.ToString("F1");
        }

    }
}

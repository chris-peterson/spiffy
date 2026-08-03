using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;

namespace Spiffy.Monitoring.Aws
{
    public class ResponseLoggingApi
    {
        internal ResponseLoggingOption ResponseLoggingOption { get; private set; } = ResponseLoggingOption.OnError;

        public void Always()
        {
            ResponseLoggingOption = ResponseLoggingOption.Always;
        }

        public void OnlyErrors()
        {
            ResponseLoggingOption = ResponseLoggingOption.OnError;
        }

        public void Never()
        {
            ResponseLoggingOption = ResponseLoggingOption.Never;
        }
    }

    /// <summary>
    /// The kinds of message the AWS SDK reports on every call that say nothing a reader can act on.
    /// Every kind is filtered by default; see <see cref="NoiseFiltersApi"/>.
    /// </summary>
    public enum SdkNoise
    {
        /// <summary>Reported for all configurations (legacy/standard/etc).</summary>
        DefaultConfigurationMode,

        /// <summary>Routine DynamoDB usage, once per table description the SDK loads, cached or not.</summary>
        TableDescription,

        /// <summary>One line per unset variable, on every client construction.</summary>
        UnsetEnvironmentVariables,

        /// <summary>One per call, restating the SDK's own user agent.</summary>
        UserAgentHeader,

        /// <summary>Internal buffer growth while (de)serializing.</summary>
        BufferResizing,

        /// <summary>Internal retry bookkeeping, one per handled error.</summary>
        RetryBookkeeping,

        /// <summary>Request signing, one per call, carrying the request path.</summary>
        RequestCanonicalization
    }

    public class NoiseFiltersApi
    {
        // Ordered so Prefixes is deterministic.  A kind can cover more than one prefix -- the
        // SDK reports canonicalization as both "Single encoded" and "Double encoded".
        static readonly KeyValuePair<SdkNoise, string[]>[] KnownNoise =
        {
            Known(SdkNoise.DefaultConfigurationMode, "Resolved DefaultConfigurationMode for RegionEndpoint"),
            Known(SdkNoise.TableDescription, "Description for table"),
            Known(SdkNoise.UnsetEnvironmentVariables, "The environment variable "),
            Known(SdkNoise.UserAgentHeader, "User-Agent Header"),
            Known(SdkNoise.BufferResizing, "Resizing buffer"),
            Known(SdkNoise.RetryBookkeeping, "Retry check: "),
            Known(SdkNoise.RequestCanonicalization, "Single encoded ", "Double encoded ")
        };

        static KeyValuePair<SdkNoise, string[]> Known(SdkNoise kind, params string[] prefixes)
        {
            return new KeyValuePair<SdkNoise, string[]>(kind, prefixes);
        }

        readonly HashSet<SdkNoise> _kinds = new HashSet<SdkNoise>(KnownNoise.Select(known => known.Key));
        readonly List<string> _prefixes = new List<string>();

        // Only Information and Verbose are filtered against these; Warning and above always
        // reach the log regardless of what's configured here.
        internal IReadOnlyList<string> Prefixes =>
            KnownNoise
                .Where(known => _kinds.Contains(known.Key))
                .SelectMany(known => known.Value)
                .Concat(_prefixes)
                .ToArray();

        /// <summary>
        /// Also filters <paramref name="kinds"/>, on top of whatever is already filtered.
        /// </summary>
        public void Add(params SdkNoise[] kinds)
        {
            foreach (var kind in kinds)
            {
                _kinds.Add(kind);
            }
        }

        /// <summary>
        /// Also filters messages starting with <paramref name="prefixes"/> -- for noise spiffy
        /// doesn't know about, which <see cref="SdkNoise"/> can't name.
        /// </summary>
        public void Add(params string[] prefixes)
        {
            _prefixes.AddRange(prefixes);
        }

        /// <summary>
        /// Stops filtering <paramref name="kinds"/>, so those messages are logged again.
        /// </summary>
        public void Remove(params SdkNoise[] kinds)
        {
            foreach (var kind in kinds)
            {
                _kinds.Remove(kind);
            }
        }

        /// <summary>
        /// Filters nothing, so every message the SDK reports is logged.
        /// </summary>
        public void Clear()
        {
            _kinds.Clear();
            _prefixes.Clear();
        }
    }

    public class AwsConfigurationApi
    {
        public ResponseLoggingApi LogResponses { get; } = new ResponseLoggingApi();
        public NoiseFiltersApi NoiseFilters { get; } = new NoiseFiltersApi();
    }
}

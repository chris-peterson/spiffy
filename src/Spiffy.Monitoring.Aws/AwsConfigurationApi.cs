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

    public class SuppressedMessagesApi
    {
        // The SDK reports these at Information/Verbose on every call, and none of them
        // is actionable.  Anything the SDK reports at Warning or above is never suppressed.
        internal IReadOnlyList<string> Prefixes { get; private set; } = new[]
        {
            // emitted for all configurations (legacy/standard/etc)
            "Resolved DefaultConfigurationMode for RegionEndpoint",
            // routine DynamoDB usage, once per cached table description
            "Description for table",
            // one per unset variable, on every client construction
            "The environment variable ",
            // one per call, restating the SDK's own user agent
            "User-Agent Header",
            // internal buffer growth
            "Resizing buffer",
            // internal retry bookkeeping, one per handled error
            "Retry check: ",
            // request signing, one per call, carrying the request path
            "Single encoded ",
            "Double encoded "
        };

        /// <summary>
        /// Suppresses messages starting with <paramref name="prefixes"/>, in addition to whatever
        /// is already suppressed (the defaults, unless <see cref="Only"/> or <see cref="None"/> ran first).
        /// </summary>
        public void Add(params string[] prefixes)
        {
            Prefixes = Prefixes.Concat(prefixes ?? Array.Empty<string>()).ToArray();
        }

        /// <summary>
        /// Suppresses only messages starting with <paramref name="prefixes"/>, discarding the defaults.
        /// </summary>
        public void Only(params string[] prefixes)
        {
            Prefixes = (prefixes ?? Array.Empty<string>()).ToArray();
        }

        /// <summary>
        /// Emits every message the SDK reports, including the noise the defaults suppress.
        /// </summary>
        public void None()
        {
            Prefixes = Array.Empty<string>();
        }
    }

    public class AwsConfigurationApi
    {
        public ResponseLoggingApi LogResponses { get; } = new ResponseLoggingApi();
        public SuppressedMessagesApi SuppressMessages { get; } = new SuppressedMessagesApi();
    }
}

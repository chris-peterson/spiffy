using System;
using System.Text.RegularExpressions;

namespace Spiffy.Monitoring
{
    static class StackTraceCleanup
    {
        // Bounded because the patterns below are quadratic on a long run of characters
        // the class matches with no following '.' -- an input we don't control.
        static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

        // "Namespace.Class.<MethodName>d__12.MoveNext()" => "async Namespace.Class.MethodName()"
        static readonly Regex AsyncStateMachinePattern = new Regex(
            @"([\w.+`\[\],]+)\.<(\w+)>d__\d+\.MoveNext\(\)",
            RegexOptions.Compiled, MatchTimeout);

        // "Namespace.Class.<MethodName>b__3_0()" => "Namespace.Class.MethodName { lambda }()"
        static readonly Regex LambdaPattern = new Regex(
            @"([\w.+`\[\],]+)\.<(\w+)>b__\d+_\d+\(\)",
            RegexOptions.Compiled, MatchTimeout);

        // "Namespace.Class.<MethodName>g__LocalFunc|0_0()" => "Namespace.Class.MethodName { LocalFunc }()"
        static readonly Regex LocalFunctionPattern = new Regex(
            @"([\w.+`\[\],]+)\.<(\w+)>g__(\w+)\|\d+_\d+\(\)",
            RegexOptions.Compiled, MatchTimeout);

        /// <summary>
        /// Rewrites compiler-generated frame names -- async state machines, lambdas and local
        /// functions -- back to the method the caller wrote.
        /// </summary>
        internal static string Simplify(string stackTrace)
        {
            // Every pattern below requires a "<name>" frame, so a trace without '<'
            // -- the common case -- can skip all three.
            if (string.IsNullOrEmpty(stackTrace) || stackTrace.IndexOf('<') < 0)
            {
                return stackTrace;
            }

            try
            {
                var result = AsyncStateMachinePattern.Replace(stackTrace, "async $1.$2()");
                result = LambdaPattern.Replace(result, "$1.$2 { lambda }()");
                return LocalFunctionPattern.Replace(result, "$1.$2 { $3 }()");
            }
            catch (RegexMatchTimeoutException)
            {
                return stackTrace;
            }
        }
    }
}

using System.Text.RegularExpressions;

namespace Spiffy.Monitoring
{
    static class StackTraceCleanup
    {
        /// <summary>
        /// Cleans up compiler-generated noise from stack traces.
        /// On net8.0+, the runtime already produces clean async traces, so this is a no-op.
        /// On netstandard2.0, applies regex to simplify async state machine and lambda frames.
        /// </summary>
        internal static string Simplify(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return stackTrace;

#if NET8_0_OR_GREATER
            return stackTrace;
#else
            // "Namespace.Class.<MethodName>d__12.MoveNext()" => "async Namespace.Class.MethodName()"
            var result = AsyncStateMachinePattern.Replace(stackTrace, "async $1$2()");

            // "Namespace.Class.<MethodName>b__3_0()" => "Namespace.Class.MethodName { lambda }()"
            result = LambdaPattern.Replace(result, "$1$2 { lambda }()");

            // "Namespace.Class.<MethodName>g__LocalFunc|0_0()" => "Namespace.Class.MethodName { LocalFunc }()"
            result = LocalFunctionPattern.Replace(result, "$1$2 { $3 }()");

            return result;
#endif
        }

#if !NET8_0_OR_GREATER
        // Matches: <MethodName>d__digits.MoveNext()
        static readonly Regex AsyncStateMachinePattern = new Regex(
            @"([\w.+`\[\],]+)\.<(\w+)>d__\d+\.MoveNext\(\)",
            RegexOptions.Compiled);

        // Matches: <MethodName>b__digits_digits()
        static readonly Regex LambdaPattern = new Regex(
            @"([\w.+`\[\],]+)\.<(\w+)>b__\d+_\d+\(\)",
            RegexOptions.Compiled);

        // Matches: <MethodName>g__LocalFuncName|digits_digits()
        static readonly Regex LocalFunctionPattern = new Regex(
            @"([\w.+`\[\],]+)\.<(\w+)>g__(\w+)\|\d+_\d+\(\)",
            RegexOptions.Compiled);
#endif
    }
}

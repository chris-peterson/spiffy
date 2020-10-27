namespace Spiffy.Monitoring.BuiltIn
{
    public class BuiltInTargetsConfigurationApi
    {
        internal bool TraceEnabled { get; private set; } = false;
        public BuiltInTargetsConfigurationApi Trace()
        {
            TraceEnabled = true;
            return this;
        }

        internal bool ConsoleEnabled { get; private set; } = false;
        public BuiltInTargetsConfigurationApi Console()
        {
            ConsoleEnabled = true;
            return this;
        }
    }
}

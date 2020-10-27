namespace Spiffy.Monitoring.BuiltIn
{
    public class BuiltInConfigurationApi
    {
        internal bool TraceEnabled { get; private set; } = false;
        public BuiltInConfigurationApi Trace()
        {
            TraceEnabled = true;
            return this;
        }

        internal bool ConsoleEnabled { get; private set; } = false;
        public BuiltInConfigurationApi Console()
        {
            ConsoleEnabled = true;
            return this;
        }
    }
}

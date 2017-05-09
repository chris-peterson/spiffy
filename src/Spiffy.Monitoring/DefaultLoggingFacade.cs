namespace Spiffy.Monitoring
{
    public class DefaultLoggingFacade: LoggingFacade
    {
        private DefaultLoggingFacade()
        {}

        static ILoggingFacade _instance;

        public static ILoggingFacade Instance
        {
            get { return _instance ?? (_instance = LoggingFacadeFactory.Create()); }
        }
    }
}
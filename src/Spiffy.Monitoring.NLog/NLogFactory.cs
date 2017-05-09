using System;

namespace Spiffy.Monitoring
{
    public static class NLogFactory
    {
        /// <summary>
        /// Creates an instance of an NLog enabled LoggingFacade
        /// </summary>
        /// <remarks>
        /// If using more than one NLog facade in an application, remember to give them unique names
        /// </remarks>
        public static ILoggingFacade Create(Action<NLogConfigurationApi> configure = null, string name = null) 
        {
            var config = new NLogConfigurationApi();
            if(null != configure)
            {
                configure(config);
            }

            var logger = NLog.SetupNLog(config, name);

            var loggingFacade = LoggingFacadeFactory.Create((level, message) =>
            {
                var nLogLevel = NLog.LevelToNLogLevel(level);
                logger.Log(nLogLevel, message);
            });
            
            return loggingFacade;
        }
    }
}

using Amazon;

namespace Spiffy.Monitoring.Aws
{
    public enum Response
    {
        OnErrors,
        Always
    }
    public class AwsConfigurationApi
    {
        public AwsConfigurationApi Log(Response response)
        {
            switch (response)
            {
                case Response.Always:
                    AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.Always;
                    break;
                case Response.OnErrors:
                    AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.OnError;
                    break;
            }

            return this;
        }
    }
}

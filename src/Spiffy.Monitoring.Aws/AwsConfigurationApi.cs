using System;
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
    }
    public class AwsConfigurationApi
    {
        public ResponseLoggingApi LogResponses { get; } = new ResponseLoggingApi();
    }
}

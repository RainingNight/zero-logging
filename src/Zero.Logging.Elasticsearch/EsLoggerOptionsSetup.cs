using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Zero.Logging.Elasticsearch
{
    public class EsLoggerOptionsSetup : ConfigureFromConfigurationOptions<EsLoggerOptions>
    {
        public EsLoggerOptionsSetup(ILoggerProviderConfiguration<EsLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration)
        {

        }
    }
}

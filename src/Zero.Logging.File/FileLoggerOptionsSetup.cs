using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Zero.Logging.File
{
    public class FileLoggerOptionsSetup : ConfigureFromConfigurationOptions<FileLoggerOptions>
    {
        public FileLoggerOptionsSetup(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration)
        {

        }
    }
}

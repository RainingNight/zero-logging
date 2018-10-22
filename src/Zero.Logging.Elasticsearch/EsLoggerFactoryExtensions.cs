using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Zero.Logging.Elasticsearch;

namespace Microsoft.Extensions.Logging
{
    public static class EsLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds a file logger named 'Elasticsearch' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddElasticsearch(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, EsLoggerProvider>());
            builder.Services.AddSingleton<IConfigureOptions<EsLoggerOptions>, EsLoggerOptionsSetup>();
            builder.Services.AddSingleton<IOptionsChangeTokenSource<EsLoggerOptions>, LoggerProviderOptionsChangeTokenSource<EsLoggerOptions, EsLoggerProvider>>();

            //builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<EsLoggerOptions>, EsLoggerOptionsSetup>());
            //builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<EsLoggerOptions>, LoggerProviderOptionsChangeTokenSource<EsLoggerOptions, EsLoggerProvider>>());
            return builder;
        }


        /// <summary>
        /// Adds a file logger named 'File' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure"></param>
        public static ILoggingBuilder AddElasticsearch(this ILoggingBuilder builder, Action<EsLoggerOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddElasticsearch();
            builder.Services.Configure(configure);
            return builder;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Elasticsearch.Net;
using Zero.Logging.Batching;

namespace Zero.Logging.Elasticsearch
{
    internal class ElasticsearchHelper
    {
        private static readonly Regex _indexFormatRegex = new Regex(@"^(.*)(?:\{0\:.+\})(.*)$");
        private readonly EsLoggerOptions _options;
        private readonly Func<LogMessage, string> _indexDecider;
        private readonly bool _registerTemplateOnStartup;
        private readonly string _templateName;
        private readonly string _templateMatchString;

        private readonly ElasticLowLevelClient _client;

        public static ElasticsearchHelper Create(EsLoggerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            return new ElasticsearchHelper(options);
        }

        private ElasticsearchHelper(EsLoggerOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.IndexFormat)) throw new ArgumentException("options.IndexFormat");
            if (string.IsNullOrWhiteSpace(options.TypeName)) throw new ArgumentException("options.TypeName");
            if (string.IsNullOrWhiteSpace(options.TemplateName)) throw new ArgumentException("options.TemplateName");

            _options = options;
            _templateName = options.TemplateName;
            _templateMatchString = _indexFormatRegex.Replace(options.IndexFormat, @"$1*$2");
            _indexDecider = options.IndexDecider ?? (logMsg => string.Format(options.IndexFormat, logMsg.Timestamp));

            if (!string.IsNullOrEmpty(options.ElasticsearchUrl))
            {
                options.ConnectionPool = new SingleNodeConnectionPool(new Uri(options.ElasticsearchUrl));
            }

            var configuration = new ConnectionConfiguration(options.ConnectionPool, options.Connection, options.Serializer)
                .RequestTimeout(options.ConnectionTimeout);
            if (options.ModifyConnectionSettings != null)
                configuration = options.ModifyConnectionSettings(configuration);

            configuration.ThrowExceptions();

            _client = new ElasticLowLevelClient(configuration);
            _registerTemplateOnStartup = options.AutoRegisterTemplate;
            TemplateRegistrationSuccess = !_registerTemplateOnStartup;
        }

        public EsLoggerOptions Options => _options;
        public IElasticLowLevelClient Client => _client;

        public bool TemplateRegistrationSuccess { get; private set; }

        public string Serialize(object o)
        {
            return _client.Serializer.SerializeToString(o, SerializationFormatting.None);
        }

        public string GetIndexForEvent(LogMessage e, DateTimeOffset offset)
        {
            if (!TemplateRegistrationSuccess) return string.Format(_options.DeadLetterIndexName, offset);
            return _indexDecider(e);
        }

        public void RegisterTemplateIfNeeded()
        {
            if (!_registerTemplateOnStartup) return;

            try
            {
                if (!_options.OverwriteTemplate)
                {
                    var templateExistsResponse = _client.IndicesExistsTemplateForAll<DynamicResponse>(_templateName);
                    if (templateExistsResponse.HttpStatusCode == 200)
                    {
                        TemplateRegistrationSuccess = true;

                        return;
                    }
                }

                var result = _client.IndicesPutTemplateForAll<DynamicResponse>(_templateName, GetTempatePostData());

                if (!result.Success)
                {
                    ((IElasticsearchResponse)result).TryGetServerErrorReason(out var serverError);
                    Console.WriteLine("Unable to create the template. {0}", serverError);
                    TemplateRegistrationSuccess = false;
                }
                else
                    TemplateRegistrationSuccess = true;

            }
            catch (Exception ex)
            {
                TemplateRegistrationSuccess = false;
                Console.WriteLine("Failed to create the template. {0}", ex);
            }
        }

        private PostData GetTempatePostData()
        {
            //PostData no longer exposes an implict cast from object.  Previously it supported that and would inspect the object Type to
            //determine if it it was a litteral string to write directly or if it was an object that it needed to serialse.  Now the onus is 
            //on us to tell it what type we are passing otherwise if the user specified the template as a json string it would be serialised again.
            var template = GetTemplateData();
            if (template is string)
            {
                return PostData.String((string)template);
            }
            else
            {
                return PostData.Serializable(template);
            }
        }

        private object GetTemplateData()
        {
            if (_options.GetTemplateContent != null)
                return _options.GetTemplateContent();

            var settings = new Dictionary<string, string>
            {
                {"index.refresh_interval", "5s"}
            };

            if (_options.NumberOfShards.HasValue)
                settings.Add("number_of_shards", _options.NumberOfShards.Value.ToString());

            if (_options.NumberOfReplicas.HasValue)
                settings.Add("number_of_replicas", _options.NumberOfReplicas.Value.ToString());

            return ElasticsearchTemplateProvider.GetTemplate(settings, _templateMatchString);

        }
    }
}

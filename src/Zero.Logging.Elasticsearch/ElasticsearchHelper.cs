using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Elasticsearch.Net;
using Zero.Logging.Batching;

namespace Zero.Logging.Elasticsearch
{
    internal class ElasticsearchHelper
    {
        private readonly ElasticLowLevelClient _client;

        private readonly Func<LogMessage, string> _indexDecider;
        private readonly bool _registerTemplateOnStartup;
        private readonly string _templateName;
        private readonly string _templateMatchString;

        private static readonly Regex _indexFormatRegex = new Regex(@"^(.*)(?:\{0\:.+\})(.*)$");

        public static ElasticsearchHelper Create(EsLoggerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            return new ElasticsearchHelper(options);
        }

        private ElasticsearchHelper(EsLoggerOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ElasticsearchUrl)) throw new ArgumentException("options.ElasticsearchUrl");
            if (string.IsNullOrWhiteSpace(options.IndexFormat)) throw new ArgumentException("options.IndexFormat");
            if (string.IsNullOrWhiteSpace(options.TypeName)) throw new ArgumentException("options.TypeName");
            if (string.IsNullOrWhiteSpace(options.TemplateName)) throw new ArgumentException("options.TemplateName");

            _templateName = options.TemplateName;
            _templateMatchString = _indexFormatRegex.Replace(options.IndexFormat, @"$1*$2");
            _indexDecider = options.IndexDecider ?? (logMsg => string.Format(options.IndexFormat, logMsg.Timestamp));

            Options = options;

            IConnectionPool pool;
            if (options.ElasticsearchUrl.Contains(";"))
            {
                var urls = options.ElasticsearchUrl.Split(';').ToList();
                pool = new StaticConnectionPool(urls.Select(_ => new Uri(_)));
            }
            else
            {
                pool = new SingleNodeConnectionPool(new Uri(options.ElasticsearchUrl));
            }

            var configuration = new ConnectionConfiguration(pool, options.Connection, options.Serializer).RequestTimeout(options.ConnectionTimeout);

            if (options.ModifyConnectionSettings != null) configuration = options.ModifyConnectionSettings(configuration);

            configuration.ThrowExceptions();

            _client = new ElasticLowLevelClient(configuration);

            _registerTemplateOnStartup = options.AutoRegisterTemplate;
            TemplateRegistrationSuccess = !_registerTemplateOnStartup;
        }

        public EsLoggerOptions Options { get; }

        public IElasticLowLevelClient Client => _client;

        public bool TemplateRegistrationSuccess { get; private set; }


        public string Serialize(object o)
        {
            return _client.Serializer.SerializeToString(o, SerializationFormatting.None);
        }

        public string GetIndexForEvent(LogMessage e, DateTimeOffset offset)
        {
            if (!TemplateRegistrationSuccess && Options.RegisterTemplateFailure == RegisterTemplateRecovery.IndexToDeadletterIndex)
            {
                return string.Format(Options.DeadLetterIndexName, offset);
            }
            return _indexDecider(e);
        }

        public void RegisterTemplateIfNeeded()
        {
            if (!_registerTemplateOnStartup) return;

            try
            {
                if (!Options.OverwriteTemplate)
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

                if (Options.RegisterTemplateFailure == RegisterTemplateRecovery.Throw)
                    throw;
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
            if (Options.GetTemplateContent != null)
                return Options.GetTemplateContent();

            var settings = new Dictionary<string, string>
            {
                {"index.refresh_interval", "5s"}
            };

            if (Options.NumberOfShards.HasValue)
                settings.Add("number_of_shards", Options.NumberOfShards.Value.ToString());

            if (Options.NumberOfReplicas.HasValue)
                settings.Add("number_of_replicas", Options.NumberOfReplicas.Value.ToString());

            return GetTemplateESv6(settings, _templateMatchString);

        }

        private static object GetTemplateESv6(Dictionary<string, string> settings, string templateMatchString)
        {
            return new
            {
                template = templateMatchString,
                settings,
                mappings = new
                {
                    _default_ = new
                    {
                        dynamic_templates = new List<object>
                        {
                            //when you use serilog as an adaptor for third party frameworks
                            //where you have no control over the log message they typically
                            //contain {0} ad infinitum, we force numeric property names to
                            //contain strings by default.
                            {
                                new
                                {
                                    numerics_in_fields = new
                                    {
                                        path_match = @"fields\.[\d+]$",
                                        match_pattern = "regex",
                                        mapping = new
                                        {
                                            type = "text",
                                            index = true,
                                            norms = false
                                        }
                                    }
                                }
                            },
                            {
                                new
                                {
                                    string_fields = new
                                    {
                                        match = "*",
                                        match_mapping_type = "string",
                                        mapping = new
                                        {
                                            type = "text",
                                            index = true,
                                            norms = false,
                                            fields = new
                                            {
                                                raw = new
                                                {
                                                    type = "keyword",
                                                    index = true,
                                                    ignore_above = 256
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        properties = new Dictionary<string, object>
                        {
                            {"message", new {type = "text", index = "true"}},
                            {
                                "exceptions", new
                                {
                                    type = "nested",
                                    properties = new Dictionary<string, object>
                                    {
                                        {"Depth", new {type = "integer"}},
                                        {"RemoteStackIndex", new {type = "integer"}},
                                        {"HResult", new {type = "integer"}},
                                        {"StackTraceString", new {type = "text", index = "true"}},
                                        {"RemoteStackTraceString", new {type = "text", index = "true"}},
                                        {
                                            "ExceptionMessage", new
                                            {
                                                type = "object",
                                                properties = new Dictionary<string, object>
                                                {
                                                    {"MemberType", new {type = "integer"}},
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

    }
}

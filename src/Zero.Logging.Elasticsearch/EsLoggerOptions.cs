using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Zero.Logging.Commom;

namespace Zero.Logging.Elasticsearch
{
    public class EsLoggerOptions : BatchingLoggerOptions
    {
        public const string DefaultNode = "http://localhost:9200";
        public const string DefaultIndexFormat = "logstash-{0:yyyy.MM.dd}";
        public const string DefaultDeadLetterIndexName = "deadletter-{0:yyyy.MM.dd}";
        public const string DefaultTypeName = "logmessage";
        public const string DefaultTemplateName = "zero-logging-template";
        public static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Configures the elasticsearch log defaults.
        /// </summary>
        public EsLoggerOptions()
        {
            IndexFormat = DefaultIndexFormat;
            DeadLetterIndexName = DefaultDeadLetterIndexName;
            TypeName = DefaultTypeName;
            TemplateName = DefaultTemplateName;
            ConnectionTimeout = DefaultConnectionTimeout;
            ConnectionPool = new SingleNodeConnectionPool(new Uri(DefaultNode));
        }

        /// <summary>
        /// Configures the elasticsearch log.
        /// </summary>
        /// <param name="connectionPool">The connectionpool to use to write events to.</param>
        public EsLoggerOptions(IConnectionPool connectionPool) : this()
        {
            ConnectionPool = connectionPool;
        }

        /// <summary>
        /// Configures the elasticsearch log.
        /// </summary>
        /// <param name="nodes">The nodes to write to.</param>
        public EsLoggerOptions(IEnumerable<Uri> nodes) : this()
        {
            nodes = nodes != null && nodes.Any(n => n != null)
                ? nodes.Where(n => n != null)
                : new[] { new Uri("http://localhost:9200") };
            if (nodes.Count() == 1)
                ConnectionPool = new SingleNodeConnectionPool(nodes.First());
            else
                ConnectionPool = new StaticConnectionPool(nodes);
        }

        /// <summary>
        /// Configures the elasticsearch log
        /// </summary>
        /// <param name="node">The node to write to</param>
        public EsLoggerOptions(Uri node) : this(new[] { node })
        {
        }

        #region Template
        /// <summary>
        /// Auto register an index template for the logs in elasticsearch.
        /// </summary>
        public bool AutoRegisterTemplate { get; set; }

        ///<summary>
        /// When using the <see cref="AutoRegisterTemplate"/> feature this allows you to override the default template name.
        /// Defaults to: zero-logging-template
        /// </summary>
        public string TemplateName { get; set; }

        /// <summary>
        /// When using the <see cref="AutoRegisterTemplate"/> feature, this allows you to override the default template content.
        /// </summary>
        public Func<object> GetTemplateContent { get; set; }

        /// <summary>
        /// When using the <see cref="AutoRegisterTemplate"/> feature, this allows you to overwrite the template in Elasticsearch if it already exists.
        /// Defaults to: false
        /// </summary>
        public bool OverwriteTemplate { get; set; }

        /// <summary>
        /// When using the <see cref="AutoRegisterTemplate"/> feature, this allows you to override the default number of shards.
        /// If not provided, this will default to the default number_of_shards configured in Elasticsearch.
        /// </summary>
        public int? NumberOfShards { get; set; }

        /// <summary>
        /// When using the <see cref="AutoRegisterTemplate"/> feature, this allows you to override the default number of replicas.
        /// If not provided, this will default to the default number_of_replicas configured in Elasticsearch.
        /// </summary>
        public int? NumberOfReplicas { get; set; }
        #endregion

        ///<summary>
        /// The index name formatter. A string.Format using the DateTimeOffset of the event is run over this string.
        /// defaults to "logstash-{0:yyyy.MM.dd}".
        /// </summary>
        public string IndexFormat { get; set; }

        /// <summary>
        /// Function to decide which index to write the log.
        /// </summary>
        public Func<LogMessage, string> IndexDecider { get; set; }

        /// <summary>
        /// Optionally set this value to the name of the index that should be used when the template cannot be written to ES.
        /// defaults to "deadletter-{0:yyyy.MM.dd}"
        /// </summary>
        public string DeadLetterIndexName { get; set; }

        /// <summary>
        /// Name the Pipeline where log are sent to es. 
        /// </summary>
        public string PipelineName { get; set; }

        /// <summary>
        /// Function to decide which Pipeline to use for the LogMessage.
        /// </summary>
        public Func<LogMessage, string> PipelineNameDecider { get; set; }

        ///<summary>
        /// The default elasticsearch type name to use for the log message. Defaults to: logmessage.
        /// </summary>
        public string TypeName { get; set; }

        ///<summary>
        /// Connection configuration to use for connecting to the cluster.
        /// </summary>
        public Func<ConnectionConfiguration, ConnectionConfiguration> ModifyConnectionSettings { get; set; }

        ///<summary>
        /// Allows you to override the connection used to communicate with elasticsearch.
        /// </summary>
        public IConnection Connection { get; set; }

        /// <summary>
        /// The connectionpool describing the cluster to write event to
        /// </summary>
        public IConnectionPool ConnectionPool { get; }

        /// <summary>
        /// The connection timeout (in milliseconds) when sending bulk operations to elasticsearch (defaults to 5000).
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; }

        ///<summary>
        /// When passing a serializer unknown object will be serialized to object instead of relying on their ToString representation
        /// </summary>
        public IElasticsearchSerializer Serializer { get; set; }

        /// <summary>
        /// A callback which can be used to handle logmessage which are not submitted to Elasticsearch like when it is unable to accept the events. 
        /// </summary>
        public Action<LogMessage> FailureCallback { get; set; }
    }
}

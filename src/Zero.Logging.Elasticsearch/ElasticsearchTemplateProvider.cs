using System;
using System.Collections.Generic;

namespace Zero.Logging.Elasticsearch
{
    internal class ElasticsearchTemplateProvider
    {
        public static object GetTemplate(Dictionary<string, string> settings, string templateMatchString)
        {
            return new
            {
                template = templateMatchString,
                settings,
                mappings = new
                {
                    _default_ = new
                    {
                        dynamic_templates = new List<Object>
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

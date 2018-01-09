﻿using Foundatio.Parsers.ElasticQueries.Extensions;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Microsoft.Extensions.Logging;
using Nest;

namespace Foundatio.Parsers.ElasticQueries {
    public static class ElasticQueryParserConfigurationExtensions {
        public static ElasticQueryParserConfiguration UseMappings<T>(this ElasticQueryParserConfiguration config, IndexTypeBase<T> indexType) where T : class {
            var logger = indexType.Configuration.LoggerFactory.CreateLogger(typeof(ElasticQueryParserConfiguration));
            var descriptor = indexType.ConfigureProperties(new TypeMappingDescriptor<object>());

            return config
                .UseAliases(indexType.AliasMap)
                .UseMappings<object>(d => descriptor, () => {
                    var response = indexType.Configuration.Client.GetMapping(new GetMappingRequest(indexType.Index.Name, "doc"));
                    logger.LogTrace(response.GetRequest());
                    if (!response.IsValid) 
                        logger.LogError(response.OriginalException, response.GetErrorMessage());

                    return (ITypeMapping) response.Indices[indexType.Index.Name]?["doc"] ?? descriptor;
                });
        }
    }
}
﻿using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Elasticsearch.Net;
using Nest;

namespace Foundatio.Repositories.Elasticsearch.Extensions {
    public static class ElasticExtensions {
        private static readonly Lazy<PropertyInfo> _connectionSettingsProperty = new Lazy<PropertyInfo>(() => typeof(HttpConnection).GetProperty("ConnectionSettings", BindingFlags.NonPublic | BindingFlags.Instance));
        
        public static string GetErrorMessage(this IResponse response) {
            var sb = new StringBuilder();

            if (response.OriginalException != null)
                sb.AppendLine($"Original: ({response.ApiCall.HttpStatusCode} - {response.OriginalException.GetType().Name}) {response.OriginalException.Message}");

            if (response.ServerError != null)
                sb.AppendLine($"Server: ({response.ServerError.Status}) {response.ServerError.Error}");

            var bulkResponse = response as IBulkResponse;
            if (bulkResponse != null)
                sb.AppendLine($"Bulk: {String.Join("\r\n", bulkResponse.ItemsWithErrors.Select(i => i.Error))}");

            if (sb.Length == 0)
                sb.AppendLine("Unknown error.");

            return sb.ToString();
        }

        public static string GetRequest(this IResponse response) {
            if (response == null)
                return String.Empty;

            return response.ApiCall.RequestBodyInBytes != null ?
                $"{response.ApiCall.HttpMethod} {response.ApiCall.Uri.PathAndQuery}\r\n{Encoding.UTF8.GetString(response.ApiCall.RequestBodyInBytes)}\r\n"
                : $"{response.ApiCall.HttpMethod} {response.ApiCall.Uri.PathAndQuery}\r\n";
        }
    }
}
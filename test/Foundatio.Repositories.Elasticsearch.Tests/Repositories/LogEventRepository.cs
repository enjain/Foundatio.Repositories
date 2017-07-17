﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Foundatio.Repositories.Elasticsearch.Tests.Repositories.Configuration;
using Foundatio.Repositories.Elasticsearch.Tests.Repositories.Models;
using Foundatio.Repositories.Models;
using Foundatio.Repositories.Advanced;

namespace Foundatio.Repositories.Elasticsearch.Tests {
    public class DailyLogEventRepository : ElasticRepositoryBase<LogEvent> {
        public DailyLogEventRepository(MyAppElasticConfiguration elasticConfiguration) : base(elasticConfiguration.DailyLogEvents.LogEvent) {
        }

        public DailyLogEventRepository(IIndexType<LogEvent> indexType) : base(indexType) {
        }

        public Task<FindResults<LogEvent>> GetByCompanyAsync(string company) {
            return this.FindAsync(q => q.Company(company));
        }

        public Task<FindResults<LogEvent>> GetPartialByCompanyAsync(string company) {
            return this.FindAsync(q => q.Company(company).Include(e => e.Id).Include(l => l.CreatedUtc));
        }

        public Task<FindResults<LogEvent>> GetAllByCompanyAsync(string company) {
            return this.FindAsync(q => q.Company(company));
        }

        public Task<CountResult> GetCountByCompanyAsync(string company) {
            return this.CountAsync(q => q.Company(company), o => o.CacheKey(company));
        }

        public async Task<long> IncrementValueAsync(string[] ids, int value = 1) {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            string script = $"ctx._source.value += {value};";
            if (ids.Length == 0)
                return await this.PatchAllAsync(null, new ScriptPatch(script), o => o.Notifications(false).ImmediateConsistency(true));

            await this.PatchAsync(ids, new ScriptPatch(script), o => o.Notifications(false).ImmediateConsistency(true));
            return ids.Length;
        }

        public async Task<long> IncrementValueAsync(RepositoryQueryDescriptor<LogEvent> query, int value = 1) {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            string script = $"ctx._source.value += {value};";
            return await this.PatchAllAsync(query, new ScriptPatch(script), o => o.ImmediateConsistency(true));
        }

        protected override async Task InvalidateCacheAsync(IReadOnlyCollection<ModifiedDocument<LogEvent>> documents, ICommandOptions options = null) {
            if (!IsCacheEnabled)
                return;

            if (documents != null && documents.Count > 0 && HasIdentity) {
                var keys = documents.Select(d => $"count:{d.Value.CompanyId}").Distinct().ToList();
                if (keys.Count > 0)
                    await Cache.RemoveAllAsync(keys);
            }

            await base.InvalidateCacheAsync(documents, options);
        }
    }

    public class DailyLogEventWithNoCachingRepository : DailyLogEventRepository {
        public DailyLogEventWithNoCachingRepository(MyAppElasticConfiguration configuration) : base(configuration) {
            DisableCache();
        }
    }

    public class MonthlyLogEventRepository : DailyLogEventRepository {
        public MonthlyLogEventRepository(MyAppElasticConfiguration elasticConfiguration) : base(elasticConfiguration.MonthlyLogEvents.LogEvent) {
        }
    }
}
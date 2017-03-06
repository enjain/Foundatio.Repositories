﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nest;
using System.Collections;
using System.Linq.Expressions;
using Foundatio.Repositories.Options;

namespace Foundatio.Repositories {
    public class FieldCondition {
        public Field Field { get; set; }
        public object Value { get; set; }
        public ComparisonOperator Operator { get; set; }
    }

    public enum ComparisonOperator {
        Equals,
        NotEquals,
        IsEmpty,
        HasValue
    }

    public static class FieldConditionQueryExtensions {
        internal const string FieldConditionsKey = "@FieldConditionsKey";

        public static T FieldCondition<T>(this T query, Field field, ComparisonOperator op, object value = null) where T : IRepositoryQuery {
            return query.AddCollectionOptionValue(FieldConditionsKey, new FieldCondition { Field = field, Value = value, Operator = op });
        }

        public static IRepositoryQuery<T> FieldCondition<T>(this IRepositoryQuery<T> query, Expression<Func<T, object>> objectPath, ComparisonOperator op, object value = null) where T : class {
            return query.AddCollectionOptionValue(FieldConditionsKey, new FieldCondition { Field = objectPath, Value = value, Operator = op });
        }

        public static T FieldEquals<T>(this T query, Field field, object value = null) where T : IRepositoryQuery {
            return query.AddCollectionOptionValue(FieldConditionsKey, new FieldCondition { Field = field, Value = value, Operator = ComparisonOperator.Equals });
        }

        public static IRepositoryQuery<T> FieldEquals<T>(this IRepositoryQuery<T> query, Expression<Func<T, object>> objectPath, object value = null) where T : class {
            return query.AddCollectionOptionValue(FieldConditionsKey, new FieldCondition { Field = objectPath, Value = value, Operator = ComparisonOperator.Equals });
        }

        public static T FieldNotEquals<T>(this T query, Field field, object value = null) where T : IRepositoryQuery {
            return query.AddCollectionOptionValue(FieldConditionsKey, new FieldCondition { Field = field, Value = value, Operator = ComparisonOperator.NotEquals });
        }

        public static IRepositoryQuery<T> FieldNotEquals<T>(this IRepositoryQuery<T> query, Expression<Func<T, object>> objectPath, object value = null) where T : class {
            return query.AddCollectionOptionValue(FieldConditionsKey, new FieldCondition { Field = objectPath, Value = value, Operator = ComparisonOperator.NotEquals });
        }

        public static T FieldHasValue<T>(this T query, Field field, object value = null) where T : IRepositoryQuery {
            return query.AddCollectionOptionValue(FieldConditionsKey, new FieldCondition { Field = field, Value = value, Operator = ComparisonOperator.HasValue });
        }

        public static IRepositoryQuery<T> FieldHasValue<T>(this IRepositoryQuery<T> query, Expression<Func<T, object>> objectPath, object value = null) where T : class {
            return query.AddCollectionOptionValue(FieldConditionsKey, new FieldCondition { Field = objectPath, Value = value, Operator = ComparisonOperator.HasValue });
        }

        public static T FieldEmpty<T>(this T query, Field field, object value = null) where T : IRepositoryQuery {
            return query.AddCollectionOptionValue(FieldConditionsKey, new FieldCondition { Field = field, Value = value, Operator = ComparisonOperator.IsEmpty });
        }

        public static IRepositoryQuery<T> FieldEmpty<T>(this IRepositoryQuery<T> query, Expression<Func<T, object>> objectPath, object value = null) where T : class {
            return query.AddCollectionOptionValue(FieldConditionsKey, new FieldCondition { Field = objectPath, Value = value, Operator = ComparisonOperator.IsEmpty });
        }
    }
}

namespace Foundatio.Repositories.Options {
    public static class ReadFieldConditionQueryExtensions {
        public static ICollection<FieldCondition> GetFieldConditions(this IRepositoryQuery query) {
            return query.SafeGetCollection<FieldCondition>(FieldConditionQueryExtensions.FieldConditionsKey);
        }
    }
}

namespace Foundatio.Repositories.Elasticsearch.Queries.Builders {
    public class FieldConditionsQueryBuilder : IElasticQueryBuilder {
        public Task BuildAsync<T>(QueryBuilderContext<T> ctx) where T : class, new() {
            var fieldConditions = ctx.Source.SafeGetCollection<FieldCondition>(FieldConditionQueryExtensions.FieldConditionsKey);
            if (fieldConditions == null || fieldConditions.Count <= 0)
                return Task.CompletedTask;

            foreach (var fieldValue in fieldConditions) {
                QueryBase query;
                switch (fieldValue.Operator) {
                    case ComparisonOperator.Equals:
                        if (fieldValue.Value is IEnumerable && !(fieldValue.Value is string))
                            query = new TermsQuery { Field = fieldValue.Field, Terms = (IEnumerable<object>)fieldValue.Value };
                        else
                            query = new TermQuery { Field = fieldValue.Field, Value = fieldValue.Value };
                        ctx.Filter &= query;

                        break;
                    case ComparisonOperator.NotEquals:
                        if (fieldValue.Value is IEnumerable && !(fieldValue.Value is string))
                            query = new TermsQuery { Field = fieldValue.Field, Terms = (IEnumerable<object>)fieldValue.Value };
                        else
                            query = new TermQuery { Field = fieldValue.Field, Value = fieldValue.Value };

                        ctx.Filter &= new BoolQuery { MustNot = new QueryContainer[] { query } };
                        break;
                    case ComparisonOperator.IsEmpty:
                        ctx.Filter &= new BoolQuery { MustNot = new QueryContainer[] { new ExistsQuery { Field = fieldValue.Field } } };
                        break;
                    case ComparisonOperator.HasValue:
                        ctx.Filter &= new ExistsQuery { Field = fieldValue.Field };
                        break;
                }
            }

            return Task.CompletedTask;
        }
    }
}
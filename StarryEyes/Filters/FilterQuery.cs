﻿using System;
using System.Linq;
using System.Reactive.Linq;
using Cadena.Data;
using StarryEyes.Casket;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
using StarryEyes.Filters.Sources;

namespace StarryEyes.Filters
{
    public sealed class FilterQuery : IEquatable<FilterQuery>, IMultiplexPredicate<TwitterStatus>
    {
        public event Action InvalidateRequired;

        private void RaiseInvalidateRequired()
        {
            InvalidateRequired?.Invoke();
        }

        public FilterSourceBase[] Sources { get; set; }

        public FilterExpressionRoot PredicateTreeRoot { get; set; }

        public bool IsPreparing
        {
            get { return Sources.Any(s => s.IsPreparing); }
        }

        public string ToQuery()
        {
            return "from " + Sources.GroupBy(s => s.FilterKey)
                                    .Select(g => g.Distinct(_ => _.FilterValue).ToArray()) // remove duplicated query
                                    .Select(fs =>
                                    {
                                        if (fs.Length == 1 && String.IsNullOrEmpty(fs[0].FilterValue))
                                        {
                                            // if filter value is not specified, return filter key only.
                                            return fs[0].FilterKey;
                                        }
                                        return fs[0].FilterKey + ": " +
                                               fs.Select(f => f.FilterValue.EscapeForQuery().Quote())
                                                 .JoinString(", ");
                                    })
                                    .JoinString(", ") +
                   " where " + PredicateTreeRoot.ToQuery();
        }

        public IObservable<TwitterStatus> ReceiveSources(long? maxId)
        {
            return Sources.Guard().ToObservable().SelectMany(s => s.Receive(maxId));
        }

        public Func<TwitterStatus, bool> GetEvaluator()
        {
            var sourcesEvals = Sources.Select(s => s.GetEvaluator());
            var predEvals = PredicateTreeRoot != null
                ? PredicateTreeRoot.GetEvaluator()
                : FilterExpressionBase.Contradiction;
            return _ => sourcesEvals.Any(f => f(_)) && predEvals(_);
        }

        public string GetSqlQuery()
        {
            var source =
                Sources.Guard()
                       .Select(s => s.GetSqlQuery())
                       .JoinString(" or ");
            var predicate = PredicateTreeRoot != null
                ? PredicateTreeRoot.GetSqlQuery()
                : String.Empty;
            if (!String.IsNullOrEmpty(source))
            {
                if (!String.IsNullOrEmpty(predicate))
                {
                    return "(" + source + ") and (" + predicate + ")";
                }
                return source;
            }
            return !String.IsNullOrEmpty(predicate)
                ? predicate
                : FilterExpressionBase.ContradictionSql;
        }

        public bool Equals(FilterQuery other)
        {
            if (other == null) return false;
            return ToQuery() == other.ToQuery();
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            return Equals(obj as FilterQuery);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return ToQuery().GetHashCode();
        }

        public override string ToString()
        {
            return ToQuery();
        }

        public void Activate()
        {
            Sources?.ForEach(s =>
            {
                s.Activate();
                s.InvalidateRequired += RaiseInvalidateRequired;
            });
            if (PredicateTreeRoot != null)
            {
                PredicateTreeRoot.BeginLifecycle();
                PredicateTreeRoot.InvalidateRequested += RaiseInvalidateRequired;
            }
        }

        public void Deactivate()
        {
            Sources?.ForEach(s =>
            {
                s.InvalidateRequired -= RaiseInvalidateRequired;
                s.Deactivate();
            });
            if (PredicateTreeRoot != null)
            {
                PredicateTreeRoot.InvalidateRequested -= RaiseInvalidateRequired;
                PredicateTreeRoot.EndLifecycle();
            }
        }
    }
}
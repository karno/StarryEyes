using System;
using System.Collections.ObjectModel;
using Livet;
using StarryEyes.Filters.Expressions;

namespace StarryEyes.ViewModels.Common.FilterEditor
{
    public abstract class FilterPredicateViewModelBase : ViewModel
    {
        public abstract FilterExpressionBase ToExpression();
    }

    public abstract class FilterNodeViewModelBase : FilterPredicateViewModelBase
    {
        private readonly ObservableCollection<FilterPredicateViewModelBase> _children =
            new ObservableCollection<FilterPredicateViewModelBase>();

        public ObservableCollection<FilterPredicateViewModelBase> Children
        {
            get { return this._children; }
        }
    }

    public abstract class FilterLeafViewModelBase : FilterPredicateViewModelBase
    {
    }

    public static class FilterPredicateViewModelFactory
    {
        public static FilterPredicateViewModelBase Create(FilterExpressionRoot root)
        {
            throw new NotImplementedException();
        }
    }
}

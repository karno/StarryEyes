using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Filters;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Expressions.Operators;

namespace StarryEyes.ViewModels.WindowParts.Common.FilterEditor
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
            get { return _children; }
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

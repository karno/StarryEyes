using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Sophia.Utilities
{
    public static class NotificationObjectHelper
    {
        public static IDisposable Subscribe(this INotifyPropertyChanged nobject, Action callback)
        {
            return Subscribe(nobject, null, callback);
        }

        public static IDisposable Subscribe<TNotifyPropertyChanged, TProperty>(
            this TNotifyPropertyChanged nobject,
            Expression<Func<TNotifyPropertyChanged, TProperty>> expr,
            Action callback)
            where TNotifyPropertyChanged : INotifyPropertyChanged
        {
            if (expr == null)
            {
                throw new ArgumentNullException(nameof(expr));
            }
            if (!(expr.Body is MemberExpression))
            {
                throw new ArgumentException("Argument should be a member expression.");
            }
            return Subscribe(nobject, ((MemberExpression)expr.Body).Member.Name, callback);
        }

        public static IDisposable Subscribe(this INotifyPropertyChanged nobject, string name, Action callback)
        {
            return new PropertyChangedEventListener(
                nobject, (obj, prop) =>
                {
                    if (name == null || prop.PropertyName == name)
                    {
                        callback();
                    }
                });
        }

        public static IDisposable Subscribe(this INotifyCollectionChanged collection, Action callback)
        {
            return collection.Subscribe(_ => callback());
        }

        public static IDisposable Subscribe(this INotifyCollectionChanged collection, Action<NotifyCollectionChangedEventArgs> callback)
        {
            return new CollectionChangedEventListener(collection, (o, e) => callback(e));
        }

        private sealed class PropertyChangedEventListener : IDisposable
        {
            private readonly INotifyPropertyChanged _target;
            private readonly PropertyChangedEventHandler _handler;

            public PropertyChangedEventListener(INotifyPropertyChanged target, PropertyChangedEventHandler handler)
            {
                _target = target;
                _handler = handler;
                _target.PropertyChanged += _handler;
            }

            public void Dispose()
            {
                _target.PropertyChanged -= _handler;
            }
        }

        private sealed class CollectionChangedEventListener : IDisposable
        {
            private readonly INotifyCollectionChanged _target;
            private readonly NotifyCollectionChangedEventHandler _handler;

            public CollectionChangedEventListener(INotifyCollectionChanged target,
                NotifyCollectionChangedEventHandler handler)
            {
                _target = target;
                _handler = handler;
                _target.CollectionChanged += _handler;
            }

            public void Dispose()
            {
                _target.CollectionChanged -= _handler;
            }
        }
    }
}
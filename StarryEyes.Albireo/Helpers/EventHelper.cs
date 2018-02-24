using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Disposables;

namespace StarryEyes.Albireo.Helpers
{
    public static class EventHelper
    {
        public static IDisposable ListenCollectionChanged(
            this INotifyCollectionChanged source,
            Action<NotifyCollectionChangedEventArgs> handler)
        {
            var h = new NotifyCollectionChangedEventHandler((_, e) => handler(e));
            source.CollectionChanged += h;
            return Disposable.Create(() => source.CollectionChanged -= h);
        }

        public static IDisposable ListenPropertyChanged(
            this INotifyPropertyChanged source,
            Action<PropertyChangedEventArgs> handler)
        {
            var h = new PropertyChangedEventHandler((_, e) => handler(e));
            source.PropertyChanged += h;
            return Disposable.Create(() => source.PropertyChanged -= h);
        }

        public static IDisposable ListenPropertyChanged<T>(
            this INotifyPropertyChanged source, Expression<Func<T>> propertyExpression,
            Action<PropertyChangedEventArgs> handler)
        {
            var propName = ExtractPropertyName(propertyExpression);
            if (propName == null)
            {
                throw new ArgumentException("Unknown property name.");
            }
            return source.ListenPropertyChanged(e =>
            {
                if (e.PropertyName == propName)
                {
                    handler(e);
                }
            });
        }

        public static IDisposable ListenPropertyChanging(
            this INotifyPropertyChanging source,
            Action<PropertyChangingEventArgs> handler)
        {
            var h = new PropertyChangingEventHandler((_, e) => handler(e));
            source.PropertyChanging += h;
            return Disposable.Create(() => source.PropertyChanging -= h);
        }

        public static IDisposable ListenPropertyChanging<T>(
            this INotifyPropertyChanging source, Expression<Func<T>> propertyExpression,
            Action<PropertyChangingEventArgs> handler)
        {
            var propName = ExtractPropertyName(propertyExpression);
            if (propName == null)
            {
                throw new ArgumentException("Unknown property name.");
            }
            return source.ListenPropertyChanging(e =>
            {
                if (e.PropertyName == propName)
                {
                    handler(e);
                }
            });

        }

        private static string ExtractPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            var memberExpression = propertyExpression.Body as MemberExpression;
            return memberExpression != null ? memberExpression.Member.Name : null;
        }

        public static void SafeInvoke(this Action action)
        {
            action?.Invoke();
        }

        public static void SafeInvoke<T>(this Action<T> action, T arg)
        {
            action?.Invoke(arg);
        }

        public static void SafeInvoke<T1, T2>(this Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            action?.Invoke(arg1, arg2);
        }
    }
}
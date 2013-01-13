using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace StarryEyes.Views.Behaviors
{
    public class KeyHandlingBehavior : Behavior<FrameworkElement>
    {
        public ICommand PreviewKeyDownCommand
        {
            get { return (ICommand)GetValue(PreviewKeyDownCommandProperty); }
            set { SetValue(PreviewKeyDownCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PreviewKeyDownCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreviewKeyDownCommandProperty =
            DependencyProperty.Register("PreviewKeyDownCommand", typeof(ICommand), typeof(KeyHandlingBehavior), new PropertyMetadata(null));

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected override void OnAttached()
        {
            base.OnAttached();
            _disposables.Add(Observable.FromEvent<KeyEventHandler, KeyEventArgs>(
                h => this.AssociatedObject.PreviewKeyDown += h, h => this.AssociatedObject.PreviewKeyDown -= h)
                                       .Subscribe(e =>
                                       {
                                           var commando = PreviewKeyDownCommand;
                                           if (commando != null && commando.CanExecute(e))
                                               commando.Execute(e);
                                       }));
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            _disposables.Dispose();
        }
    }
}

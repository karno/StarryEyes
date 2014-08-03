using System.Windows.Documents;
using System.Windows.Interactivity;
using System.Windows.Navigation;

namespace StarryEyes.Views.Behaviors
{
    public class NavigateHyperlinkBehavior : Behavior<Hyperlink>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.RequestNavigate += this.RequestNavigate;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.RequestNavigate -= this.RequestNavigate;
        }

        void RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            BrowserHelper.Open(e.Uri);
        }
    }
}

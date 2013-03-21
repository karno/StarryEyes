using System;
using System.Windows.Media;
using StarryEyes.Views;

namespace StarryEyes.Models.Backpanels.AppEvents
{
    public abstract class AppEventBase : BackpanelEventBase
    {
        public override string Title
        {
            get
            {
                switch (Kind)
                {
                    case AppEventKind.Notify:
                        return "INFO";
                    case AppEventKind.Warning:
                        return "WARN";
                    case AppEventKind.Error:
                        return "ERROR";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override Color Background
        {
            get
            {
                switch (Kind)
                {
                    case AppEventKind.Notify:
                        return MetroColors.Teal;
                    case AppEventKind.Warning:
                        return MetroColors.Orange;
                    case AppEventKind.Error:
                        return MetroColors.Red;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public abstract override string Detail { get; }

        public abstract AppEventKind Kind { get; }

        public sealed override string Id { get { return GetId(); } }

        protected abstract string GetId();

        public override EventRegistingKind RegisterKind
        {
            get { return EventRegistingKind.IdExclusive; }
        }
    }
}

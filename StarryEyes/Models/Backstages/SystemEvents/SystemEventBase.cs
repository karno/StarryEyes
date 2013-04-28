using System;
using System.Windows.Media;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.SystemEvents
{
    public abstract class SystemEventBase : BackstageEventBase
    {
        public override string Title
        {
            get
            {
                switch (Kind)
                {
                    case SystemEventKind.Notify:
                        return "INFO";
                    case SystemEventKind.Warning:
                        return "WARN";
                    case SystemEventKind.Error:
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
                    case SystemEventKind.Notify:
                        return MetroColors.Teal;
                    case SystemEventKind.Warning:
                        return MetroColors.Orange;
                    case SystemEventKind.Error:
                        return MetroColors.Red;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public abstract SystemEventKind Kind { get; }

        public virtual string Id { get { return null; } }

        public virtual SystemEventAction Action { get { return null; } }
    }

    public sealed class SystemEventAction
    {
        private readonly string _description;
        private readonly Action _callback;

        public SystemEventAction(string description, Action callback)
        {
            _description = description;
            _callback = callback;
        }

        public string Description
        {
            get { return _description; }
        }

        public Action Callback
        {
            get { return _callback; }
        }
    }
}

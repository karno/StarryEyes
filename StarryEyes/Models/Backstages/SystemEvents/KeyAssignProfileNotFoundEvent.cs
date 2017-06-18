using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;

namespace StarryEyes.Models.Backstages.SystemEvents
{
    public sealed class KeyAssignProfileNotFoundEvent : SystemEventBase
    {
        private readonly string _profile;

        public KeyAssignProfileNotFoundEvent(string profile)
        {
            _profile = profile;
        }

        public override SystemEventKind Kind
        {
            get { return SystemEventKind.Warning; }
        }

        public override string Detail
        {
            get { return SettingModelResources.KeyAssignProfileNotFoundFormat.SafeFormat(_profile); }
        }
    }
}

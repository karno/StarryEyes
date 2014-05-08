
using System;
using StarryEyes.Globalization;

namespace StarryEyes.Models.Backstages.SystemEvents
{
    public class ThemeProfileNotFoundEvent : SystemEventBase
    {
        private readonly string _profile;

        public ThemeProfileNotFoundEvent(string profile)
        {
            _profile = profile;
        }

        public override SystemEventKind Kind
        {
            get { return SystemEventKind.Warning; }
        }

        public override string Detail
        {
            get { return String.Format(BackstageResources.ProfileNotFoundFormat, _profile); }
        }
    }
}

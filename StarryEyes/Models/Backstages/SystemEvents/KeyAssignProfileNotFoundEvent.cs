
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
            get { return "キーアサインプロファイル " + _profile + "が見つかりませんでした。既定のプロファイルを使用します。"; }
        }
    }
}

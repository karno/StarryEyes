
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
            get { return "テーマプロファイル " + _profile + " は見つかりませんでした。既定のプロファイルを使用します。"; }
        }
    }
}

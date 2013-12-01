using System.Runtime.Serialization;
using System.Windows.Media;

namespace StarryEyes.Settings.Themes
{
    [DataContract]
    public class UserFlipTheme
    {
        #region Serialization properties

        [DataMember]
        private string KeyColor
        {
            get { return Key.ToColorString(); }
            set { Key = value.ToColor(); }
        }

        [DataMember]
        private string NotFollowedColor
        {
            get { return NotFollowed.ToColorString(); }
            set { NotFollowed = value.ToColor(); }
        }

        [DataMember]
        private string NotFollowbackedColor
        {
            get { return NotFollowbacked.ToColorString(); }
            set { NotFollowbacked = value.ToColor(); }
        }

        [DataMember]
        private string FollowingColor
        {
            get { return Following.ToColorString(); }
            set { Following = value.ToColor(); }
        }

        [DataMember]
        private string FollowbackedColor
        {
            get { return Followbacked.ToColorString(); }
            set { Followbacked = value.ToColor(); }
        }

        [DataMember]
        private string BlockingColor
        {
            get { return Blocking.ToColorString(); }
            set { Blocking = value.ToColor(); }
        }

        #endregion

        [DataMember]
        public ColorTheme Default { get; set; }

        [DataMember]
        public ColorTheme Highlight { get; set; }

        [IgnoreDataMember]
        public Color Key { get; set; }

        [IgnoreDataMember]
        public Color NotFollowed { get; set; }

        [IgnoreDataMember]
        public Color NotFollowbacked { get; set; }

        [IgnoreDataMember]
        public Color Following { get; set; }

        [IgnoreDataMember]
        public Color Followbacked { get; set; }

        [IgnoreDataMember]
        public Color Blocking { get; set; }
    }
}

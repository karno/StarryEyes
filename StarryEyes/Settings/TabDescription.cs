using System.Linq;
using System.Runtime.Serialization;
using StarryEyes.Models.Tab;

namespace StarryEyes.Settings
{
    [DataContract]
    public sealed class TabDescription
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public long[] BindingAccountIds { get; set; }

        [DataMember]
        public string[] BindingHashtags { get; set; }

        [DataMember]
        public bool IsShowUnreadCounts { get; set; }

        [DataMember]
        public bool IsNotifyNewArrivals { get; set; }

        [DataMember]
        public string Query { get; set; }

        public TabDescription()
        {
        }

        public TabDescription(TabModel model)
        {
            this.Name = model.Name;
            this.IsShowUnreadCounts = model.IsShowUnreadCounts;
            this.IsNotifyNewArrivals = model.IsNotifyNewArrivals;
            this.BindingAccountIds = model.BindingAccountIds.ToArray();
            this.BindingHashtags = model.BindingHashtags.ToArray();
            this.Query = model.FilterQueryString;
        }

        public TabModel ToTabModel()
        {
            var model = new TabModel(this.Name, this.Query)
             {
                 BindingHashtags = this.BindingHashtags,
                 IsNotifyNewArrivals = this.IsNotifyNewArrivals,
                 IsShowUnreadCounts = this.IsShowUnreadCounts
             };
            this.BindingAccountIds.ForEach(model.BindingAccountIds.Add);
            return model;
        }
    }

    [DataContract]
    public sealed class ColumnDescription
    {
        [DataMember]
        public TabDescription[] Tabs { get; set; }
    }
}

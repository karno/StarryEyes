using System.Linq;
using System.Runtime.Serialization;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
using StarryEyes.Models.Backstages.SystemEvents;
using StarryEyes.Models.Timelines.Tabs;

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
        public bool ShowUnreadCounts { get; set; }

        [DataMember]
        public bool NotifyNewArrivals { get; set; }

        [DataMember]
        public string NotifySoundSource { get; set; }

        [DataMember]
        public string Query { get; set; }

        public TabDescription()
        {
        }

        public TabDescription(TabModel model)
        {
            this.Name = model.Name;
            this.ShowUnreadCounts = model.ShowUnreadCounts;
            this.NotifyNewArrivals = model.NotifyNewArrivals;
            this.BindingAccountIds = model.BindingAccounts.ToArray();
            this.BindingHashtags = model.BindingHashtags.ToArray();
            this.NotifySoundSource = model.NotifySoundSource;
            this.Query = model.GetQueryString();
        }

        public TabModel ToTabModel()
        {
            FilterQuery filter;
            try
            {
                filter = QueryCompiler.Compile(Query);
            }
            catch (FilterQueryException ex)
            {
                BackstageModel.RegisterEvent(new QueryCorruptionEvent(Name, ex));
                filter = null;
            }
            var model = new TabModel
             {
                 Name = Name,
                 FilterQuery = filter,
                 RawQueryString = Query,
                 BindingHashtags = this.BindingHashtags,
                 NotifyNewArrivals = this.NotifyNewArrivals,
                 NotifySoundSource = this.NotifySoundSource,
                 ShowUnreadCounts = this.ShowUnreadCounts
             };
            this.BindingAccountIds.ForEach(model.BindingAccounts.Add);
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

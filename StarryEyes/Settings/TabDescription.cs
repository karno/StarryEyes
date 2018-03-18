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
            Name = model.Name;
            ShowUnreadCounts = model.ShowUnreadCounts;
            NotifyNewArrivals = model.NotifyNewArrivals;
            BindingAccountIds = model.BindingAccounts.ToArray();
            BindingHashtags = model.BindingHashtags.ToArray();
            NotifySoundSource = model.NotifySoundSource;
            Query = model.GetQueryString();
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
                BindingHashtags = BindingHashtags.Guard(),
                NotifyNewArrivals = NotifyNewArrivals,
                NotifySoundSource = NotifySoundSource,
                ShowUnreadCounts = ShowUnreadCounts
            };
            BindingAccountIds.ForEach(model.BindingAccounts.Add);
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
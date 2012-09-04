using System;
using System.Collections.Generic;
using System.ComponentModel;
using StarryEyes.Albireo.Data;
using StarryEyes.Mystique.Filters;
using StarryEyes.Mystique.Filters.Parsing;
using StarryEyes.Mystique.Models.Hub;

namespace StarryEyes.Mystique.Models.Tab
{
    /// <summary>
    /// Hold tab information for spawning tab.
    /// </summary>
    public class TabInfo
    {
        /// <summary>
        /// Name of this tab.
        /// </summary>
        private string Name { get; set; }

        private AVLTree<long> bindingAccountIds = new AVLTree<long>();
        /// <summary>
        /// Binding accounts ids
        /// </summary>
        public ICollection<long> BindingAccountIds
        {
            get { return bindingAccountIds; }
        }

        private List<string> bindingHashtags = new List<string>();
        /// <summary>
        /// Binding hashtags
        /// </summary>
        public List<string> BindingHashtags
        {
            get { return bindingHashtags; }
            set { bindingHashtags = value; }
        }

        private FilterQuery filterQuery = null;
        /// <summary>
        /// Filter query info
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FilterQuery FilterQuery
        {
            get { return filterQuery; }
            set { filterQuery = value; }
        }

        /// <summary>
        /// Filter querified string
        /// </summary>
        public string FilterQueryString
        {
            get { return filterQuery.ToQuery(); }
            set
            {
                try
                {
                    filterQuery = QueryCompiler.Compile(value);
                }
                catch (FilterQueryException fex)
                {
                    InformationHub.PublishInformation(new Information(InformationKind.Warning,
                        "TABINFO_QUERY_CORRUPTED_" + Name,
                        "クエリが壊れています。",
                        "タブ " + Name + " のクエリは破損しているため、フィルタが初期化されました。" + Environment.NewLine +
                        "送出された例外: " + fex.ToString()));
                }
            }
        }
    }
}

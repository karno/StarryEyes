using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarryEyes.Mystique.Models.Tab
{
    /// <summary>
    /// Hold tab information for spawning tab.
    /// </summary>
    public class TabInfo
    {
        private List<long> bindingAccountIds = new List<long>();
        /// <summary>
        /// Binding accounts ids
        /// </summary>
        public List<long> BindingAccountIds
        {
            get { return bindingAccountIds; }
            set { bindingAccountIds = value; }
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
    
        // TODO: QueryDescription, Filtering
        public void F()
        {
            var data = from src in "data"
                       where src > 0
                       select src;
        }
    }
}

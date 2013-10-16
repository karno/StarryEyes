using System.Collections.Generic;

namespace StarryEyes.Models.Timelines.Tabs
{
    /// <summary>
    /// Column information
    /// </summary>
    public class ColumnInfo
    {
        public ColumnInfo() { }

        public ColumnInfo(IEnumerable<TabModel> tabs)
        {
            this.Tabs = new List<TabModel>(tabs);
        }

        public List<TabModel> Tabs { get; set; }
    }
}

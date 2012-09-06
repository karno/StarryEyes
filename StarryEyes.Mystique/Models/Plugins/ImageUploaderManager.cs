using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.SweetLady.Imaging;

namespace StarryEyes.Mystique.Models.Plugins
{
    public static class ImageUploaderManager
    {
        private static object uploaderLocker = new object();
        private static SortedDictionary<string, ImageUploaderBase> uploaders
            = new SortedDictionary<string,ImageUploaderBase>();

        public static IEnumerable<string> Uploaders
        {
            get
            {
                lock (uploaderLocker)
                {
                    return uploaders.Keys.ToArray();
                }
            }
        }
    }
}

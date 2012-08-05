using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.SweetLady.DataModel;
using Livet;
using StarryEyes.Mystique.Models.Store;

namespace StarryEyes.Mystique.Models.Tab
{
    /// <summary>
    /// Manage resources for a tab.
    /// </summary>
    public class TabBackEnd 
    {
        ObservableSynchronizedCollection<TwitterStatus> backendCollection = new ObservableSynchronizedCollection<TwitterStatus>();

        public TabBackEnd()
        {
        }

        /// <summary>
        /// Initialize collection and start receiving and filtering
        /// </summary>
        public void Initialize()
        {
            StatusStore.StatusPublisher.Subscribe(notify =>
            {
                // add or remove backend collection and visible collection
            });
        }

        /// <summary>
        /// Notify this tab is closed.
        /// </summary>
        public void Close()
        {
        }

        /// <summary>
        /// Notify this tab is revived.
        /// </summary>
        public void Revive()
        {
        }
    }
}

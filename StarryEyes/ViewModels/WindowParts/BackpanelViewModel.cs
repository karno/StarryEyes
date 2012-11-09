using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using StarryEyes.Models;
using StarryEyes.Models.Backpanel;
using System.Windows.Media;

namespace StarryEyes.ViewModels.WindowParts
{
    /// <summary>
    /// バックパネル ViewModel
    /// </summary>
    public class BackpanelViewModel : ViewModel
    {
        public void Initialize()
        {
        }
    }

    public class BackpanelEventViewModel : ViewModel
    {
        private readonly BackpanelEventBase _sourceEvent;
        public BackpanelEventBase SourceEvent
        {
            get { return _sourceEvent; }
        } 

        public BackpanelEventViewModel(BackpanelEventBase ev)
        {
            this._sourceEvent = ev;
        }

        public Color Background
        {
            get { return SourceEvent.Background; }
        }

        public Color Foreground
        {
            get { return SourceEvent.Foreground; }
        }

        public string Title
        {
            get { return SourceEvent.Title; }
        }

        public string Detail
        {
            get { return SourceEvent.Detail; }
        }
    }
}

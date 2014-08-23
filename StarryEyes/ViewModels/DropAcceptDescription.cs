using System;
using System.Windows;
using StarryEyes.Albireo;
using StarryEyes.Albireo.Helpers;

namespace StarryEyes.ViewModels
{
    public sealed class DropAcceptDescription
    {
        public event Action<DragEventArgs> DragOver;

        public void OnDragOver(DragEventArgs dragEventArgs)
        {
            DragOver.SafeInvoke(dragEventArgs);
        }

        public event Action<DragEventArgs> DragDrop;

        public void OnDrop(DragEventArgs dragEventArgs)
        {
            DragDrop.SafeInvoke(dragEventArgs);
        }
    }
}

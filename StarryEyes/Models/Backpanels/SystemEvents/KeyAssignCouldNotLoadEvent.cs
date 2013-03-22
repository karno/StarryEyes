using System;
using System.IO;

namespace StarryEyes.Models.Backpanels.SystemEvents
{
    public sealed class KeyAssignCouldNotLoadEvent : SystemEventBase
    {
        private readonly string _filepath;
        private readonly Exception _thrown;
        private readonly Action _callback;

        public KeyAssignCouldNotLoadEvent(string filepath, Exception thrown, Action callback)
        {
            _filepath = filepath;
            _thrown = thrown;
            _callback = callback;
        }

        public override SystemEventKind Kind
        {
            get { return SystemEventKind.Error; }
        }

        public override string Detail
        {
            get { return "キーアサインファイル" + Path.GetFileNameWithoutExtension(_filepath) + "を読み込めません: " + _thrown.Message; }
        }

        public override string Id
        {
            get { return "KEYASSIGN_COULD_NOT_LOAD_" + _filepath; }
        }

        public override SystemEventAction Action
        {
            get
            {
                return new SystemEventAction("再読み込み", _callback);
            }
        }
    }
}

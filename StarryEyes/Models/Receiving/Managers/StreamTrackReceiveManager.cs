using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Models.Receiving.Receivers;

namespace StarryEyes.Models.Receiving.Managers
{
    internal class StreamTrackReceiveManager
    {
        private readonly UserReceiveManager _receiveManager;

        private readonly object _trackLocker = new object();

        private readonly IDictionary<string, long> _trackResolver = new Dictionary<string, long>();

        private readonly IDictionary<string, int> _trackReferenceCount = new Dictionary<string, int>();

        private readonly object _danglingLocker = new object();

        private readonly List<string> _danglingKeywords = new List<string>();

        private bool _isDanglingNotified;

        private readonly object _addTrackLocker = new object();

        private List<string> _addTrackWaits;

        public StreamTrackReceiveManager(UserReceiveManager receiveManager)
        {
            this._receiveManager = receiveManager;
            this._receiveManager.TrackRearranged += this.UpdateTrackInfo;
        }

        void UpdateTrackInfo()
        {
            string[] dang;
            lock (this._trackLocker)
            {
                var allTracks = this._trackResolver.Keys.ToArray();
                this._trackResolver.Clear();
                var trackers = this._receiveManager.GetTrackers();
                foreach (var track in trackers)
                {
                    var id = track.UserId;
                    track.TrackKeywords.ForEach(k => this._trackResolver[k] = id);
                }
                dang = allTracks.Except(this._trackResolver.Keys).ToArray();
            }
            if (dang.Length > 0)
            {
                lock (this._danglingLocker)
                {
                    this._danglingKeywords.AddRange(dang);
                    this.NotifyDangling();
                }
            }
        }

        public void AddTrackKeyword(string track)
        {
            lock (this._addTrackLocker)
            {
                if (this._addTrackWaits == null)
                {
                    this._addTrackWaits = new List<string> { track };
                    Observable.Timer(TimeSpan.FromSeconds(3))
                              .Subscribe(_ =>
                              {
                                  List<string> tracks;
                                  lock (this._addTrackLocker)
                                  {
                                      tracks = this._addTrackWaits;
                                      this._addTrackWaits = null;
                                  }
                                  this.AddTrackKeywordCore(tracks.ToArray());
                              });
                }
                else
                {
                    this._addTrackWaits.Add(track);
                }
            }
        }

        public void RemoveTrackKeyword(string track)
        {
            Observable.Timer(TimeSpan.FromSeconds(10))
                      .Subscribe(_ => this.RemoveTrackKeywordCore(track));
        }

        private void AddTrackKeywordCore(string[] tracks)
        {
            lock (this._trackLocker)
            {
                foreach (var track in tracks)
                {
                    if (this._trackReferenceCount.ContainsKey(track))
                    {
                        this._trackReferenceCount[track]++;
                        return;
                    }
                    BehaviorLogger.Log("TRACK", "add query: " + track);
                    System.Diagnostics.Debug.WriteLine("◎ track add: " + track);
                    this._trackReferenceCount[track] = 1;
                }

                var trackList = new List<string>(tracks);

                while (trackList.Count > 0)
                {
                    var tracker = this._receiveManager.GetSuitableKeywordTracker();
                    if (tracker == null)
                    {
                        lock (this._danglingLocker)
                        {
                            this._danglingKeywords.AddRange(trackList);
                        }
                        this.NotifyDangling();
                        return;
                    }
                    var acceptableCount = UserStreamsReceiver.MaxTrackingKeywordCounts - tracker.TrackKeywords.Count();
                    var acceptables = trackList.Take(acceptableCount).ToArray();
                    acceptables.ForEach(track => this._trackResolver[track] = tracker.UserId);
                    tracker.TrackKeywords = tracker.TrackKeywords.Concat(acceptables).ToArray();
                    trackList = trackList.Skip(acceptableCount).ToList();
                }
            }
        }

        private void RemoveTrackKeywordCore(string track)
        {
            lock (this._trackLocker)
            {
                if (!this._trackReferenceCount.ContainsKey(track) || --this._trackReferenceCount[track] > 0)
                {
                    return;
                }
                this._trackReferenceCount.Remove(track);
                if (this._trackResolver.ContainsKey(track))
                {
                    BehaviorLogger.Log("TRACK", "remove query: " + track);
                    var id = this._trackResolver[track];
                    this._trackResolver.Remove(track);
                    var tracker = this._receiveManager.GetKeywordTrackerFromId(id);
                    tracker.TrackKeywords = tracker.TrackKeywords.Except(new[] { track });
                    return;
                }
            }
            lock (this._danglingLocker)
            {
                this._danglingKeywords.Remove(track);
            }
        }

        private void NotifyDangling()
        {
            if (this._isDanglingNotified) return;
            this._isDanglingNotified = true;
        }

        private void ResolveDanglings()
        {
        }

    }
}

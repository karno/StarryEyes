using StarryEyes.Models.Timeline;

namespace StarryEyes.ViewModels.Timeline
{
    public class TimelineViewModelBase
    {
        private readonly TimelineModelBase _model;

        public TimelineViewModelBase(TimelineModelBase model)
        {
            _model = model;
        }
    }
}

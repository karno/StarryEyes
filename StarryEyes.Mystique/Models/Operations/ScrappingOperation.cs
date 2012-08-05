using System;
using System.Reactive;
using System.Reactive.Linq;
using StarryEyes.Mystique.Scrapping;

namespace StarryEyes.Mystique.Models.Operations
{
    public class ScrappingOperation : OperationBase<bool>
    {
        public ScrappingOperation() { }

        public ScrappingOperation(string url, long? sourceId = null)
        {
            this.Url = url;
            this.SourceId = sourceId;
        }

        public string Url { get; set; }

        public long? SourceId { get; set; }

        protected override IObservable<bool> RunCore()
        {
            ScrappingService scrapService = null;
            /*
            switch (Setting.ScrappingService.ServiceType)
            {
                case ScrappingServiceType.None:
                    NotifyHelper.Notify("please configure login information in setting page.",
                        "service unconfigured");
                    return Observable.Return(new Unit());
                case ScrappingServiceType.Instapaper:
                    scrapService = new InstapaperApi(
                        Setting.ScrappingService.UserId,
                        Setting.ScrappingService.Password);
                    break;
                case ScrappingServiceType.Pocket:
                    scrapService = new PocketApi(
                        Define.PocketApiKey,
                        Setting.ScrappingService.UserId,
                        Setting.ScrappingService.Password);
                    break;
                case ScrappingServiceType.Readability:
                    scrapService = new ReadabilityApi(
                        Define.ReadabilityApiKey,
                        Define.ReadabilityApiSecret,
                        Setting.ScrappingService.UserId,
                        Setting.ScrappingService.Password);
                    break;
            }
            */
            return scrapService.Scrap(url: Url, sourceTweetId: SourceId);
        }
    }
}
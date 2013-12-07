using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Subsystems
{
    public static class TwitterConfigurationService
    {
        private const int _textMaxLength = 140;
        private static int _httpUrlLength = 22;
        private static int _httpsUrlLength = 23;
        private static int _mediaUrlLength = 23;

        public static int TextMaxLength
        {
            get { return _textMaxLength; }
        }

        public static int HttpUrlLength
        {
            get { return _httpUrlLength; }
            private set { _httpUrlLength = value; }
        }

        public static int HttpsUrlLength
        {
            get { return _httpsUrlLength; }
            private set { _httpsUrlLength = value; }
        }

        public static int MediaUrlLength
        {
            get { return _mediaUrlLength; }
            private set { _mediaUrlLength = value; }
        }

        internal static void Initialize()
        {
            Observable.Timer(TimeSpan.FromSeconds(5), TimeSpan.FromDays(0.5))
                      .ObserveOn(TaskPoolScheduler.Default)
                      .Subscribe(_ => UpdateConfiguration());
        }

        private static async void UpdateConfiguration()
        {
            var account = Setting.Accounts.GetRandomOne();
            if (account == null)
            {
                // execute later
                Observable.Timer(TimeSpan.FromMinutes(1))
                          .ObserveOn(TaskPoolScheduler.Default)
                          .Subscribe(_ => UpdateConfiguration());
                return;
            }
            try
            {
                var config = await account.GetConfigurationAsync();
                HttpUrlLength = config.ShortUrlLength;
                HttpsUrlLength = config.ShortUrlLengthHttps;
                MediaUrlLength = config.CharactersReservedPerMedia;
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent("TwitterAPI構成情報の受信に失敗しました", ex));
                // execute later
                Observable.Timer(TimeSpan.FromMinutes(5))
                          .ObserveOn(TaskPoolScheduler.Default)
                          .Subscribe(_ => UpdateConfiguration());
            }
        }
    }
}
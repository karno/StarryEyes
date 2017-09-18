using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Globalization.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Subsystems
{
    public static class TwitterConfigurationService
    {
        public const int TextMaxLength = 140;

        // default values
        private static int _httpUrlLength = 22;
        private static int _httpsUrlLength = 23;
        private static int _mediaUrlLength = 0; // media url -> 0 (extended_tweet)

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
                var config = await account.GetConfigurationAsync().ConfigureAwait(false);
                HttpUrlLength = config.ShortUrlLength;
                HttpsUrlLength = config.ShortUrlLengthHttps;
                MediaUrlLength = config.CharactersReservedPerMedia;
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent(SubsystemResources.TwitterConfigurationReceiveError, ex));
                // execute later
                Observable.Timer(TimeSpan.FromMinutes(5))
                          .ObserveOn(TaskPoolScheduler.Default)
                          .Subscribe(_ => UpdateConfiguration());
            }
        }
    }
}
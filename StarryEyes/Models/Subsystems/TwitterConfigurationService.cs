using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Cadena.Api.Rest;
using StarryEyes.Globalization.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Subsystems
{
    public static class TwitterConfigurationService
    {
        public const int TextMaxLength = 140;
        public const int NewTextMaxLength = 280;

        // default values

        public static int HttpUrlLength { get; private set; } = 22;

        public static int HttpsUrlLength { get; private set; } = 23;

        public static int MediaUrlLength { get; private set; } = 23;

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
                var config = (await account.CreateAccessor().GetConfigurationAsync(CancellationToken.None)
                                           .ConfigureAwait(false)).Result;
                HttpUrlLength = config.ShortUrlLength;
                HttpsUrlLength = config.ShortUrlLengthHttps;
                MediaUrlLength = config.CharactersReservedPerMedia;
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(
                    new OperationFailedEvent(SubsystemResources.TwitterConfigurationReceiveError, ex));
                // execute later
                Observable.Timer(TimeSpan.FromMinutes(5))
                          .ObserveOn(TaskPoolScheduler.Default)
                          .Subscribe(_ => UpdateConfiguration());
            }
        }
    }
}
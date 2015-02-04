using System;
using JetBrains.Annotations;
using StarryEyes.Anomaly;
using StarryEyes.Anomaly.TwitterApi;

namespace StarryEyes.Illumine.Settings
{
    public interface ITwitterAccount : IOAuthCredential
    {
        /// <summary>
        /// <para>Fallback target ID</para>
        /// <para>If specified ID is not found in setting store, ignore it.</para>
        /// </summary>
        long? FallbackAccountId { get; set; }

        /// <summary>
        /// <para>Fallback favorites just like do with tweets.</para>
        /// </summary>
        bool IsFallbackFavorite { get; set; }

        /// <summary>
        /// Caching objects
        /// </summary>
        [NotNull]
        ITwitterAccountCache Cache { get; }

        /// <summary>
        /// Properties for UserStreams
        /// </summary>
        [NotNull]
        IUserStreamsProperties UserStreamsProperties { get; }

        /// <summary>
        /// Properties for accessing twitter
        /// </summary>
        [NotNull]
        IApiAccessProperties ApiAccessProperties { get; }
    }

    public interface ITwitterPostingProperties
    {
        /// <summary>
        /// <para>Mark uploaded medias as sensitive contents.</para>
        /// <para>(if true, set flag possibly_sensitive=true)</para>
        /// </summary>
        bool MarkMediaAsPossiblySensitive { get; set; }
    }

    public interface ITwitterAccountCache
    {
        /// <summary>
        /// Cached screen name
        /// </summary>
        string ScreenName { get; set; }

        /// <summary>
        /// Cached user name
        /// </summary>
        string UserName { get; set; }

        /// <summary>
        /// Cached profile image URI
        /// </summary>
        Uri ProfileImageUri { get; set; }
    }

    public interface IUserStreamsProperties
    {
        /// <summary>
        /// Use user streams for receiving tweets.
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// <para>Receive replies to following users.</para>
        /// <para>(if true, set flag replies=all)</para>
        /// </summary>
        bool ReceiveRepliesToFollowings { get; set; }

        /// <summary>
        /// <para>Receive activities caused by following users.</para>
        /// <para>(if true, set flag include_followings_activity=true)</para>
        /// </summary>
        bool ReceiveActivitiesFromFollowings { get; set; }
    }


}

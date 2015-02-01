namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public interface ICursorResult<out T>
    {
        T Result { get; }
        long PreviousCursor { get; }
        long NextCursor { get; }
        bool CanReadNext { get; }
        bool CanReadPrevious { get; }
    }
}
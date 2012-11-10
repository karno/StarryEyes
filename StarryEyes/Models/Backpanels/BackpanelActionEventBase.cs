
namespace StarryEyes.Models.Backpanels
{
    /// <summary>
    /// A Event that contains related action.
    /// </summary>
    public abstract class BackpanelActionEventBase: BackpanelEventBase
    {
        /// <summary>
        /// Execute related action.
        /// </summary>
        public abstract void Execute();
    }
}

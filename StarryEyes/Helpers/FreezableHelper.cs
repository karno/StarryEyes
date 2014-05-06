
// ReSharper disable once CheckNamespace
namespace System.Windows
{
    public static class FreezableHelper
    {
        public static T ToFrozen<T>(this T freezable) where T : Freezable
        {
            if (!freezable.IsFrozen)
            {
                if (freezable.CanFreeze)
                    freezable.Freeze();
                else
                    throw new InvalidOperationException("Object could not frozen.");
            }
            return freezable;
        }
    }
}

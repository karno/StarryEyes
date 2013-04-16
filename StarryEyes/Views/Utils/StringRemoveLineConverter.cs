
namespace StarryEyes.Views.Utils
{
    public class StringRemoveLineConverter : OneWayConverter<string, string>
    {
        protected override string ToTarget(string input, object parameter)
        {
            return input.Replace("\r", "").Replace("\n", " ");
        }
    }
}

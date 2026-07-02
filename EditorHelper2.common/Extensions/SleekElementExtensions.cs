using SDG.Unturned;

namespace EditorHelper2.common.Extensions
{
    public static class SleekElementExtensions
    {
        /// <summary>
        /// <see cref="ISleekElement.GetChildAtIndex(int)"/> currently has an issue with getting <see cref="SleekWrapper"/> children.
        /// This is a temporary fix until Nelson fixes it
        /// </summary>
        public static ISleekElement GetChildAtIndexEx(this ISleekElement element, int index)
        {
            var child = element.GetChildAtIndex(index);

            if (child is ISleekProxyImplementation proxy) child = proxy.GetWrapper();

            return child;
        }
    }
}

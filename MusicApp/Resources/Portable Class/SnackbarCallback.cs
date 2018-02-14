using Android.Support.Design.Widget;
using Java.Lang;
using MusicApp.Resources.values;

namespace MusicApp.Resources.Portable_Class
{
    public class SnackbarCallback : Snackbar.Callback
    {
        public override void OnShown(Snackbar sb)
        {
            base.OnShown(sb);
            MainActivity.instance.PaddingHasChanged(new PaddingChange(MainActivity.paddingBot, sb.View.Height));
        }

        public override void OnDismissed(Snackbar sb, int @event)
        {
            base.OnDismissed(sb, @event);
            MainActivity.instance.PaddingHasChanged(new PaddingChange(MainActivity.paddingBot));
        }
    }
}
using Android.Content;
using Android.Gms.Cast;
using Android.Gms.Cast.Framework;
using Android.Gms.Cast.Framework.Media;
using Android.Runtime;
using System.Collections.Generic;

namespace Opus
{
    [Register("Opus/CastProvider")]
    public class CastProvider : Java.Lang.Object, IOptionsProvider
    {
        public CastOptions GetCastOptions(Context appContext)
        {
            NotificationOptions notification = new NotificationOptions.Builder()
                .SetActions(new List<string> { MediaIntentReceiver.ActionSkipPrev, MediaIntentReceiver.ActionTogglePlayback, MediaIntentReceiver.ActionSkipNext }, new int[] { 1 })
                .SetTargetActivityClassName(Java.Lang.Class.FromType(typeof(MainActivity)).Name)
                .SetSmallIconDrawableResId(Resource.Drawable.NotificationIcon)
                .SetSkipPrevDrawableResId(Resource.Drawable.SkipPrevious)
                .SetPauseDrawableResId(Resource.Drawable.Pause)
                .SetPlayDrawableResId(Resource.Drawable.Play)
                .SetSkipNextDrawableResId(Resource.Drawable.SkipNext)
                .Build();

            CastMediaOptions media = new CastMediaOptions.Builder()
                .SetNotificationOptions(notification)
                .Build();

            CastOptions options = new CastOptions.Builder()
                .SetReceiverApplicationId(CastMediaControlIntent.DefaultMediaReceiverApplicationId)
                .SetCastMediaOptions(media)
                .SetStopReceiverApplicationWhenEndingSession(true)
                .Build();
            return options;
        }

        public IList<SessionProvider> GetAdditionalSessionProviders(Context appContext)
        {
            return null;
        }
    }
}
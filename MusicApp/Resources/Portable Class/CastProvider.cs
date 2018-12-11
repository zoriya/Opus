using Android.Content;
using Android.Gms.Cast;
using Android.Gms.Cast.Framework;
using Android.Gms.Cast.Framework.Media;
using Android.Runtime;
using System.Collections.Generic;

namespace MusicApp
{
    [Register("MusicApp/CastProvider")]
    public class CastProvider : Java.Lang.Object, IOptionsProvider
    {
        public CastOptions GetCastOptions(Context appContext)
        {
            //NotificationOptions notification = new NotificationOptions.Builder()
            //    .
            //Customise (or disable) build in notification here.

            CastOptions options = new CastOptions.Builder()
                .SetReceiverApplicationId(CastMediaControlIntent.DefaultMediaReceiverApplicationId)
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
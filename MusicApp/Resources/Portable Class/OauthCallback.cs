using System;
using Android.App;
using Android.Content;
using Android.OS;

namespace MusicApp.Resources.Portable_Class
{
    [Activity(Label = "OauthCallback")]
    [
    IntentFilter
    (
        actions: new[] { Intent.ActionView },
        Categories = new[]
        {
                Intent.CategoryDefault,
                Intent.CategoryBrowsable
        },
        DataSchemes = new[]
        {
            "com.musicapp.android"
        },
        DataPaths = new[]
        {
            "/oauth2redirect"
        }
    )
]
    public class OauthCallback : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Android.Net.Uri IntentUri = Intent.Data;
            Uri uri = new Uri(IntentUri.ToString());

            MainActivity.auth?.OnPageLoading(uri);
            Finish();
        }
    }
}
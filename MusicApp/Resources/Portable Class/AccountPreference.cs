using Android.Content;
using Android.Gms.Auth.Api;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V7.Preferences;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using MusicApp;
using MusicApp.Resources.Portable_Class;
using Square.Picasso;
using System;

public class AccountPreference : Preference, IResultCallback
{
    private View view;
    private EventHandler logIn;
    private EventHandler logOut;

    public AccountPreference(Context context) : base(context) { }

    public AccountPreference(Context context, IAttributeSet attrs) : base(context, attrs) { }

    public AccountPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { }

    public AccountPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) { }

    protected AccountPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }


    public override void OnBindViewHolder(PreferenceViewHolder holder)
    {
        base.OnBindViewHolder(holder);
        view = holder.ItemView;
        Button log = (Button)view.FindViewById(Resource.Id.logButton);

        logIn = (s, e) => { LogIn(); };
        logOut = (s, e) => { LogOut(); };

        if (MainActivity.account == null)
        {
            log.SetTextColor(Color.DarkBlue);
            log.Text = Preferences.instance.Resources.GetString(Resource.String.log_in);
            log.Click += logIn;

            if (MainActivity.Theme == 1)
                view.FindViewById<ImageView>(Android.Resource.Id.Icon).SetColorFilter(Color.White);
            else
                view.FindViewById<ImageView>(Android.Resource.Id.Icon).SetColorFilter(Color.Gray);
        }
        else
        {
            log.Text = Preferences.instance.Resources.GetString(Resource.String.log_out);
            Picasso.With(Android.App.Application.Context).Load(MainActivity.account.PhotoUrl).Transform(new CircleTransformation()).Into(view.FindViewById<ImageView>(Android.Resource.Id.Icon));
            log.SetTextColor(Color.Red);
            log.Click += logOut;
        }
    }

    public void OnSignedIn()
    {
        Button log = (Button)view.FindViewById(Resource.Id.logButton);
        log.Text = Preferences.instance.Resources.GetString(Resource.String.log_out);
        Picasso.With(Android.App.Application.Context).Load(MainActivity.account.PhotoUrl).Transform(new CircleTransformation()).Into(view.FindViewById<ImageView>(Android.Resource.Id.Icon));
        view.FindViewById<ImageView>(Android.Resource.Id.Icon).ClearColorFilter();
        log.SetTextColor(Color.Red);
        log.Click -= logIn;
    }

    public void OnResult(Java.Lang.Object result)
    {
        Button log = (Button)view.FindViewById(Resource.Id.logButton);
        log.SetTextColor(Color.DarkBlue);
        log.Text = Preferences.instance.Resources.GetString(Resource.String.log_in);
        Summary = "";
        Picasso.With(Android.App.Application.Context).Load(Resource.Drawable.account).Into(view.FindViewById<ImageView>(Android.Resource.Id.Icon));
        if(MainActivity.Theme == 1)
            view.FindViewById<ImageView>(Android.Resource.Id.Icon).SetColorFilter(Color.White);
        else
            view.FindViewById<ImageView>(Android.Resource.Id.Icon).SetColorFilter(Color.Gray);
        log.Click -= logOut;
        MainActivity.instance.InvalidateOptionsMenu();
    }

    void LogIn()
    {
        if (MainActivity.instance.googleClient == null)
        {
            GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                    .RequestIdToken("112086459272-59scolco82ho7d6hcieq8kmdjai2i2qd.apps.googleusercontent.com")
                    .RequestServerAuthCode("112086459272-59scolco82ho7d6hcieq8kmdjai2i2qd.apps.googleusercontent.com")
                    .RequestEmail()
                    .RequestScopes(new Scope(YouTubeService.Scope.Youtube))
                    .Build();

            MainActivity.instance.googleClient = new GoogleApiClient.Builder(Preferences.instance)
                    .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
                    .Build();

            MainActivity.instance.googleClient.Connect();
        }

        Preferences.instance.StartActivityForResult(Auth.GoogleSignInApi.GetSignInIntent(MainActivity.instance.googleClient), 5981);
    }

    void LogOut()
    {
        MainActivity.account = null;
        YoutubeEngine.youtubeService = null;
        Auth.GoogleSignInApi.SignOut(MainActivity.instance.googleClient).SetResultCallback(this);
    }
}
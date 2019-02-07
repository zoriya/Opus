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
using System.Threading.Tasks;

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

    private async void LogIn()
    {
        GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            .RequestIdToken(Preferences.instance.GetString(Resource.String.clientID))
            .RequestServerAuthCode(Preferences.instance.GetString(Resource.String.clientID))
            .RequestEmail()
            .RequestScopes(new Scope(YouTubeService.Scope.Youtube))
            .Build();

        GoogleApiClient googleClient = new GoogleApiClient.Builder(Preferences.instance)
            .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
            .Build();

        googleClient.Connect();

        while (!googleClient.IsConnected)
            await Task.Delay(10);

        Preferences.instance.StartActivityForResult(Auth.GoogleSignInApi.GetSignInIntent(googleClient), 5981);
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


    private async void LogOut()
    {
        MainActivity.account = null;
        YoutubeEngine.youtubeService = null;

        GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestIdToken(Preferences.instance.GetString(Resource.String.clientID))
                .RequestServerAuthCode(Preferences.instance.GetString(Resource.String.clientID))
                .RequestEmail()
                .RequestScopes(new Scope(YouTubeService.Scope.Youtube))
                .Build();

        GoogleApiClient googleClient = new GoogleApiClient.Builder(Preferences.instance)
                .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
                .Build();

        googleClient.Connect();

        while (!googleClient.IsConnected)
            await Task.Delay(10);

        Auth.GoogleSignInApi.RevokeAccess(googleClient).SetResultCallback(this);

        ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Preferences.instance);
        ISharedPreferencesEditor editor = prefManager.Edit();
        editor.Remove("refresh-token");
        editor.Apply();
    }

    //Log out result
    public void OnResult(Java.Lang.Object result)
    {
        Button log = (Button)view.FindViewById(Resource.Id.logButton);
        log.SetTextColor(Color.DarkBlue);
        log.Text = Preferences.instance.Resources.GetString(Resource.String.log_in);
        Summary = "";
        Picasso.With(Android.App.Application.Context).Load(Resource.Drawable.account).Into(view.FindViewById<ImageView>(Android.Resource.Id.Icon));
        if (MainActivity.Theme == 1)
            view.FindViewById<ImageView>(Android.Resource.Id.Icon).SetColorFilter(Color.White);
        else
            view.FindViewById<ImageView>(Android.Resource.Id.Icon).SetColorFilter(Color.Gray);
        log.Click -= logOut;
        MainActivity.instance.InvalidateOptionsMenu();
    }
}
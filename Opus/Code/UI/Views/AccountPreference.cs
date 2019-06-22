using Android.Content;
//using Android.Gms.Auth.Api;
//using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Android.Graphics;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.Preferences;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
//using Google.Apis.YouTube.v3;
using Opus;
//using Opus.Api;
using Opus.Fragments;
using Opus.Others;
using Square.Picasso;
using System;
//using System.Threading.Tasks;

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

            Color color;
            TypedValue value = new TypedValue();
            if (Context.Theme.ResolveAttribute(Resource.Attribute.accountIconColor, value, true))
                color = Color.ParseColor("#" + Integer.ToHexString(value.Data));
            else
                color = Color.Black;

            view.FindViewById<ImageView>(Android.Resource.Id.Icon).SetColorFilter(color);

        }
        else
        {
            log.Text = Preferences.instance.Resources.GetString(Resource.String.log_out);
            Picasso.With(Android.App.Application.Context).Load(MainActivity.account.PhotoUrl).Transform(new CircleTransformation()).Into(view.FindViewById<ImageView>(Android.Resource.Id.Icon));
            log.SetTextColor(Color.Red);
            log.Click += logOut;
        }
    }

    private /*async*/ void LogIn()
    {
        Snackbar snackBar = Snackbar.Make(Preferences.instance.FindViewById(Android.Resource.Id.Content), Resource.String.login_disabled, Snackbar.LengthLong);
        snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
        snackBar.Show();

        //GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
        //    .RequestIdToken(Preferences.instance.GetString(Resource.String.clientID))
        //    .RequestServerAuthCode(Preferences.instance.GetString(Resource.String.clientID))
        //    .RequestEmail()
        //    .RequestScopes(new Scope(YouTubeService.Scope.Youtube))
        //    .Build();

        //GoogleApiClient googleClient = new GoogleApiClient.Builder(Preferences.instance)
        //    .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
        //    .Build();

        //googleClient.Connect();

        //while (!googleClient.IsConnected)
        //    await Task.Delay(10);

        //Preferences.instance.StartActivityForResult(Auth.GoogleSignInApi.GetSignInIntent(googleClient), 5981);
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


    private /*async*/ void LogOut()
    {
        //MainActivity.account = null;
        //YoutubeManager.YoutubeService = null;

        //GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
        //        .RequestIdToken(Preferences.instance.GetString(Resource.String.clientID))
        //        .RequestServerAuthCode(Preferences.instance.GetString(Resource.String.clientID))
        //        .RequestEmail()
        //        .RequestScopes(new Scope(YouTubeService.Scope.Youtube))
        //        .Build();

        //GoogleApiClient googleClient = new GoogleApiClient.Builder(Preferences.instance)
        //        .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
        //        .Build();

        //googleClient.Connect();

        //while (!googleClient.IsConnected)
        //    await Task.Delay(10);

        //Auth.GoogleSignInApi.RevokeAccess(googleClient).SetResultCallback(this);

        //ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Preferences.instance);
        //ISharedPreferencesEditor editor = prefManager.Edit();
        //editor.Remove("refresh-token");
        //editor.Apply();
    }

    //Log out result
    public void OnResult(Java.Lang.Object result)
    {
        Button log = (Button)view.FindViewById(Resource.Id.logButton);
        log.SetTextColor(Color.DarkBlue);
        log.Text = Preferences.instance.Resources.GetString(Resource.String.log_in);
        Summary = "";
        Picasso.With(Android.App.Application.Context).Load(Resource.Drawable.account).Into(view.FindViewById<ImageView>(Android.Resource.Id.Icon));
        log.Click -= logOut;
        MainActivity.instance.InvalidateOptionsMenu();
    }
}
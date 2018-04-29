using Android.Content;
using Android.Gms.Auth.Api;
using Android.Gms.Common.Apis;
using Android.Preferences;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using MusicApp;
using Square.Picasso;
using System;

public class AccountPreference : Preference, IResultCallback
{
    private View view;

    public AccountPreference(Context context) : base(context) { }

    public AccountPreference(Context context, IAttributeSet attrs) : base(context, attrs) { }

    public AccountPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { }

    public AccountPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) { }

    protected AccountPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

    protected override void OnBindView(View view)
    {
        base.OnBindView(view);
        this.view = view;
        Button log = (Button)view.FindViewById(Resource.Id.logButton);

        if (MainActivity.account == null)
        {
            log.SetTextColor(Android.Graphics.Color.DarkBlue);
            log.Text = "Log In";
            log.Click += (s, e) => { LogIn(); };
        }
        else
        {
            log.Text = "Log Out";
            Picasso.With(Android.App.Application.Context).Load(MainActivity.account.PhotoUrl).Into(view.FindViewById<ImageView>(Android.Resource.Id.Icon));
            log.SetTextColor(Android.Graphics.Color.Red);
            log.Click += (s, e) => { LogOut(); };
        }
    }

    public void OnSignedIn()
    {
        Button log = (Button)view.FindViewById(Resource.Id.logButton);
        log.Text = "Log Out";
        Picasso.With(Android.App.Application.Context).Load(MainActivity.account.PhotoUrl).Into(view.FindViewById<ImageView>(Android.Resource.Id.Icon));
        log.SetTextColor(Android.Graphics.Color.Red);
        log.Click -= (s, e) => { LogIn(); };
        log.Click += (s, e) => { LogOut(); };
    }

    public void OnResult(Java.Lang.Object result)
    {
        Button log = (Button)view.FindViewById(Resource.Id.logButton);
        log.SetTextColor(Android.Graphics.Color.DarkBlue);
        log.Text = "Log In";
        Picasso.With(Android.App.Application.Context).Load(Resource.Drawable.ic_account_circle_black_24dp).Into(view.FindViewById<ImageView>(Android.Resource.Id.Icon));
        log.Click -= (s, e) => { LogOut(); };
        log.Click += (s, e) => { LogIn(); };
    }

    void LogIn()
    {
        MainActivity.instance.Login(true);
    }

    void LogOut()
    {
        MainActivity.account = null;
        Auth.GoogleSignInApi.SignOut(MainActivity.instance.googleClient).SetResultCallback(this);
    }
}
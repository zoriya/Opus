using Android.Content;
using Android.Runtime;
using Android.Preferences;
using Android.Util;
using Android.Widget;
using MusicApp;
using MusicApp.Resources.Portable_Class;
using System;
using System.Linq;
using Xamarin.Auth;
using Android.Views;

public class AccountPreference : Preference
{
    public AccountPreference(Context context) : base(context) { }

    public AccountPreference(Context context, IAttributeSet attrs) : base(context, attrs) { }

    public AccountPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { }

    public AccountPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) { }

    protected AccountPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

    protected override void OnBindView(View view)
    {
        base.OnBindView(view);
        Button log = (Button)view.FindViewById(MusicApp.Resource.Id.logButton);

        if (MainActivity.refreshToken == null || MainActivity.refreshToken == "")
        {
            log.SetTextColor(Android.Graphics.Color.DarkBlue);
            log.Text = "Log In";
            log.Click += (s, e) => { MainActivity.instance.Login(); };
        }
        else
        {
            log.Text = "Log Out";
            log.SetTextColor(Android.Graphics.Color.Red);
            log.Click += (s, e) =>
            {
                MainActivity.refreshToken = null;
                AccountStore accountStore = AccountStore.Create();
                Account account = accountStore.FindAccountsForService("Google").FirstOrDefault();
                accountStore.Delete(account, "Google");

                ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Preferences.instance);
                ISharedPreferencesEditor editor = pref.Edit();
                editor.Remove("expireDate");
            };
        }
    }
}
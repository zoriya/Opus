using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Gms.Auth.Api;
using Android.Gms.Auth.Api.SignIn;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Preferences;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Preference = Android.Support.V7.Preferences.Preference;
using PreferenceManager = Android.Support.V7.Preferences.PreferenceManager;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Opus.Resources.Portable_Class
{
    [Activity(Label = "Settings", Theme = "@style/Theme")]
    public class Preferences : AppCompatActivity
    {
        public static Preferences instance;
        public Toolbar toolbar;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (MainActivity.Theme == 1)
                SetTheme(Resource.Style.DarkPreferences);

            SetContentView(Resource.Layout.PreferenceRoot);
            toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            instance = this;
            toolbar.NavigationClick += (sender, e) =>
            {
                if (DownloadFragment.instance == null)
                    Finish();
                else if (DownloadFragment.instance != null)
                {
                    DownloadFolderBack();
                }
            };

            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.PreferenceFragment, new PreferencesFragment()).Commit();
        }

        public override void OnBackPressed()
        {
            if (DownloadFragment.instance != null)
                DownloadFolderBack();
            else
                base.OnBackPressed();
        }

        private void DownloadFolderBack()
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(this);
            ISharedPreferencesEditor editor = prefManager.Edit();
            editor.PutString("downloadPath", DownloadFragment.instance.path);
            editor.Apply();
            Preference downloadPref = PreferencesFragment.instance.PreferenceScreen.FindPreference("downloadPath");
            downloadPref.Summary = DownloadFragment.instance.path ?? Environment.GetExternalStoragePublicDirectory(Environment.DirectoryMusic).ToString();
            PreferencesFragment.instance.path = DownloadFragment.instance.path;

            DownloadFragment.instance = null;
            SupportFragmentManager.PopBackStack();
        }

        protected override void OnStop()
        {
            base.OnStop();
            instance = null;
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == 5981)
            {
                GoogleSignInResult result = Auth.GoogleSignInApi.GetSignInResultFromIntent(data);
                if (result.IsSuccess)
                {
                    MainActivity.account = result.SignInAccount;
                    PreferencesFragment.instance?.SignedIn();
                    MainActivity.instance.CreateYoutube();
                }
                else
                {
                    MainActivity.instance.waitingForYoutube = false;
                }
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
        }
    }

    public class PreferencesFragment : PreferenceFragmentCompat
    {
        public static PreferencesFragment instance;
        public string path;
        private View view;

        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey)
        {
            instance = this;
            SetPreferencesFromResource(Resource.Layout.Preferences, rootKey);
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);

            //Download Path
            Preference downloadPref = PreferenceScreen.FindPreference("downloadPath");
            downloadPref.IconSpaceReserved = false;
            downloadPref.PreferenceClick += DownloadClick;
            downloadPref.Summary = prefManager.GetString("downloadPath", Environment.GetExternalStoragePublicDirectory(Environment.DirectoryMusic).ToString());
            path = prefManager.GetString("downloadPath", Environment.GetExternalStoragePublicDirectory(Environment.DirectoryMusic).ToString());

            //Maximum Download
            Preference maxDlPref = PreferenceScreen.FindPreference("maxDownload");
            maxDlPref.IconSpaceReserved = false;
            maxDlPref.PreferenceClick += MaxDownloadClick;
            maxDlPref.Summary = prefManager.GetInt("maxDownload", 4).ToString();

            //Keep Deleted
            Preference keepDeletedPref = PreferenceScreen.FindPreference("keepDeleted");
            keepDeletedPref.IconSpaceReserved = false;
            keepDeletedPref.PreferenceClick += KeepDeletedClick;
            keepDeletedPref.Summary = (!prefManager.GetBoolean("keepDeleted", true)).ToString();

            //Theme
            Preference themePreference = PreferenceScreen.FindPreference("theme");
            themePreference.IconSpaceReserved = false;
            themePreference.PreferenceClick += ChangeTheme;
            themePreference.Summary = prefManager.GetInt("theme", 0) == 0 ? Resources.GetString(Resource.String.white_theme) : Resources.GetString(Resource.String.dark_theme);

            //Check For Update
            Preference updatePreference = PreferenceScreen.FindPreference("update");
            updatePreference.IconSpaceReserved = false;
            updatePreference.PreferenceClick += UpdatePreference_PreferenceClick;

            //Version Number
            Preference versionPreference = PreferenceScreen.FindPreference("version");
            string VersionAsset;
            string Beta;
            AssetManager assets = Application.Context.Assets;
            using (StreamReader sr = new StreamReader(assets.Open("Version.txt")))
            {
                VersionAsset = sr.ReadLine();
                Beta = sr.ReadLine();
            }

            string version = VersionAsset.Substring(9, 5);
            if (version.EndsWith(".0"))
                version = version.Substring(0, 3);
            bool beta = false;
            if (Beta.Substring(6, 1) == "T")
                beta = true;

            versionPreference.Summary = "v" + version + (beta ? "-Beta" : "");
            versionPreference.IconSpaceReserved = false;

            //Account
            Preference accountPreference = PreferenceScreen.FindPreference("account");
            accountPreference.IconSpaceReserved = false;

            if (MainActivity.account != null)
            {
                accountPreference.Title = Resources.GetString(Resource.String.logged_in);
                accountPreference.Summary = MainActivity.account.DisplayName;
            }
        }


        public override void OnDestroy()
        {
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            view = base.OnCreateView(inflater, container, savedInstanceState);
            if (MainActivity.Theme == 1)
                view.SetBackgroundColor(Android.Graphics.Color.Argb(225, 33, 33, 33));
            return view;
        }

        public void SignedIn()
        {
            AccountPreference accountPreference = (AccountPreference)PreferenceScreen.FindPreference("account");
            accountPreference.Title = "Logged in as:";
            accountPreference.Summary = MainActivity.account.DisplayName;
            accountPreference.OnSignedIn();
            MainActivity.instance.InvalidateOptionsMenu();
        }

        #region Download location
        private void DownloadClick(object sender, Preference.PreferenceClickEventArgs e)
        {
            Preferences.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.PreferenceFragment, DownloadFragment.NewInstance(path)).AddToBackStack(null).Commit();
            Preferences.instance.toolbar.Title = "Download Location";
        }
        #endregion

        #region Maximum Download
        private void MaxDownloadClick(object sender, Preference.PreferenceClickEventArgs e)
        {
            View pickerView = LayoutInflater.Inflate(Resource.Layout.NumberPicker, null);
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle(Resources.GetString(Resource.String.max_download_dialog));
            builder.SetView(pickerView);
            NumberPicker picker = (NumberPicker)pickerView;
            picker.MinValue = 1;
            picker.MaxValue = 10;
            picker.Value = int.Parse(FindPreference("maxDownload").Summary);

            builder.SetPositiveButton(Resources.GetString(Resource.String.apply), (s, eventArg) => 
            {
                ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                ISharedPreferencesEditor editor = pref.Edit();
                editor.PutInt("maxDownload", picker.Value);
                editor.Apply();

                Preference prefButton = FindPreference("maxDownload");
                prefButton.Summary = pref.GetInt("maxDownload", 2).ToString();

                if(Downloader.instance != null && Downloader.queue.Count > 0)
                {
                    Downloader.instance.maxDownload = pref.GetInt("maxDownload", 4);
                    Downloader.instance.StartDownload();
                } 
            });
            builder.SetNegativeButton(Resources.GetString(Resource.String.cancel), (s, eventArg) => { });
            builder.Show();
        }
        #endregion

        #region Keep Deleted
        private void KeepDeletedClick(object sender, Preference.PreferenceClickEventArgs e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Delete song when removing them from a synced playlist:");
            builder.SetItems(new string[] { "True", "False" }, (s, args) => 
            {
                ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                ISharedPreferencesEditor editor = pref.Edit();
                editor.PutBoolean("keepDeleted", args.Which == 1);
                editor.Apply();

                Preference prefButton = FindPreference("keepDeleted");
                prefButton.Summary = args.Which == 0 ? "True" : "False";
            });
            builder.Show();
        }
        #endregion

        #region Theme
        private void ChangeTheme(object sender, Preference.PreferenceClickEventArgs e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle(Resources.GetString(Resource.String.theme_dialog));
            builder.SetItems(new[] { Resources.GetString(Resource.String.white_theme), Resources.GetString(Resource.String.dark_theme) }, (s, args) =>
            {
                ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                ISharedPreferencesEditor editor = pref.Edit();
                editor.PutInt("theme", args.Which);
                editor.Apply();

                Preference prefButton = FindPreference("theme");
                prefButton.Summary = args.Which == 0 ? Resources.GetString(Resource.String.white_theme) : Resources.GetString(Resource.String.dark_theme);

                MainActivity.instance.SwitchTheme(args.Which);
                MainActivity.instance.Recreate();
                Activity.Recreate();
            });
            builder.Show();
        }
        #endregion

        #region Updater
        private void UpdatePreference_PreferenceClick(object sender, Preference.PreferenceClickEventArgs e)
        {
            MainActivity.CheckForUpdate(Preferences.instance, true);
        }
        #endregion
    }
}
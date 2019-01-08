using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Database;
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
using System.Threading.Tasks;
using static Android.Provider.MediaStore.Audio;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using CursorLoader = Android.Support.V4.Content.CursorLoader;
using Preference = Android.Support.V7.Preferences.Preference;
using PreferenceManager = Android.Support.V7.Preferences.PreferenceManager;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace MusicApp.Resources.Portable_Class
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
                //{
                SetTheme(Resource.Style.DarkPreferences);
            //    Window.SetNavigationBarColor(Android.Graphics.Color.Argb(255, 33, 33, 33));
            //}
            SetContentView(Resource.Layout.PreferenceRoot);
            toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            instance = this;
            Window.SetStatusBarColor(Android.Graphics.Color.Argb(255, 33, 33, 33));
            toolbar.Title = "Settings";
            toolbar.NavigationClick += (sender, e) =>
            {
                if (DownloadFragment.instance == null && TopicSelector.instance == null)
                    Finish();
                else
                {
                    if (DownloadFragment.instance != null)
                    {
                        ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(this);
                        ISharedPreferencesEditor editor = prefManager.Edit();
                        editor.PutString("downloadPath", DownloadFragment.instance.path);
                        editor.Apply();
                        Preference downloadPref = PreferencesFragment.instance.PreferenceScreen.FindPreference("downloadPath");
                        downloadPref.Summary = DownloadFragment.instance.path ?? "not set";
                        PreferencesFragment.instance.path = DownloadFragment.instance.path;

                        DownloadFragment.instance = null;
                        SupportFragmentManager.PopBackStack();
                    }
                    else if (TopicSelector.instance != null)
                    {
                        ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(this);
                        ISharedPreferencesEditor editor = prefManager.Edit();
                        List<string> topics = new List<string>();
                        for (int i = 0; i < TopicSelector.instance.selectedTopics.Count; i++)
                        {
                            topics.Add(TopicSelector.instance.selectedTopics[i] + "/#-#/" + TopicSelector.instance.selectedTopicsID[i]);
                        }
                        editor.PutStringSet("selectedTopics", topics);
                        editor.Apply();
                        TopicSelector.instance = null;
                        SupportFragmentManager.PopBackStack();

                        Preference topicPreference = PreferencesFragment.instance.PreferenceScreen.FindPreference("topics");
                        if (topics.Count == 0)
                            topicPreference.Summary = "Actually nothing";
                        else if (topics.Count == 1)
                            topicPreference.Summary = topics[0].Substring(0, topics[0].IndexOf("/#-#/"));
                        else if (topics.Count == 2)
                            topicPreference.Summary = topics[0].Substring(0, topics[0].IndexOf("/#-#/")) + " and " + topics[1].Substring(0, topics[1].IndexOf("/#-#/"));
                        else if (topics.Count == 3)
                            topicPreference.Summary = topics[0].Substring(0, topics[0].IndexOf("/#-#/")) + ", " + topics[1].Substring(0, topics[1].IndexOf("/#-#/")) + " and " + topics[2].Substring(0, topics[2].IndexOf("/#-#/"));
                        else if (topics.Count > 3)
                            topicPreference.Summary = topics[0].Substring(0, topics[0].IndexOf("/#-#/")) + ", " + topics[1].Substring(0, topics[1].IndexOf("/#-#/")) + ", " + topics[2].Substring(0, topics[2].IndexOf("/#-#/")) + " and more.";
                    }
                }
            };

            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.PreferenceFragment, new PreferencesFragment()).Commit();
        }

        protected override void OnStop()
        {
            base.OnStop();
            instance = null;
            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
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
            Window.SetStatusBarColor(Android.Graphics.Color.Argb(255, 33, 33, 33));
        }
    }

    public class PreferencesFragment : PreferenceFragmentCompat
    {
        public static PreferencesFragment instance;
        public string path;
        private View view;

        //Local Shortcut
        private int LSposition;

        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey)
        {
            AskForPermission();

            instance = this;
            SetPreferencesFromResource(Resource.Layout.Preferences, rootKey);
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);

            //Music Genres
            Preference topicPreference = PreferenceScreen.FindPreference("topics");
            topicPreference.PreferenceClick += TopicPreference;
            string[] topics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToArray();

            if (topics.Length == 0)
                topicPreference.Summary = "Actually nothing";
            else if (topics.Length == 1)
                topicPreference.Summary = topics[0].Substring(0, topics[0].IndexOf("/#-#/"));
            else if (topics.Length == 2)
                topicPreference.Summary = topics[0].Substring(0, topics[0].IndexOf("/#-#/")) + " and " + topics[1].Substring(0, topics[1].IndexOf("/#-#/"));
            else if(topics.Length == 3)
                topicPreference.Summary = topics[0].Substring(0, topics[0].IndexOf("/#-#/")) + ", " + topics[1].Substring(0, topics[1].IndexOf("/#-#/")) + " and " + topics[2].Substring(0, topics[2].IndexOf("/#-#/"));
            else if(topics.Length > 3)
                topicPreference.Summary = topics[0].Substring(0, topics[0].IndexOf("/#-#/")) + ", " + topics[1].Substring(0, topics[1].IndexOf("/#-#/")) + ", " + topics[2].Substring(0, topics[2].IndexOf("/#-#/")) + " and more.";


            //Skip Exist Verification
            Preference skipExistVerification = PreferenceScreen.FindPreference("skipExistVerification");
            skipExistVerification.PreferenceClick += SkipClick;
            skipExistVerification.Summary = prefManager.GetBoolean("skipExistVerification", false) ? "True" : "False";

            //Local play shortcut
            Preference localShortcutPreference = PreferenceScreen.FindPreference("localPlay");
            localShortcutPreference.PreferenceClick += LocalShortcut;
            localShortcutPreference.Summary = prefManager.GetString("localPlay", "Shuffle All Audio Files");

            //Download Path
            Preference downloadPref = PreferenceScreen.FindPreference("downloadPath");
            downloadPref.PreferenceClick += DownloadClick;
            downloadPref.Summary = prefManager.GetString("downloadPath", Environment.GetExternalStoragePublicDirectory(Environment.DirectoryMusic).ToString());
            path = prefManager.GetString("downloadPath", Environment.GetExternalStoragePublicDirectory(Environment.DirectoryMusic).ToString());

            //Maximum Download
            Preference maxDlPref = PreferenceScreen.FindPreference("maxDownload");
            maxDlPref.PreferenceClick += MaxDownloadClick;
            maxDlPref.Summary = prefManager.GetInt("maxDownload", 4).ToString();

            //Keep Deleted
            Preference keepDeletedPref = PreferenceScreen.FindPreference("keepDeleted");
            keepDeletedPref.PreferenceClick += KeepDeletedClick;
            keepDeletedPref.Summary = (!prefManager.GetBoolean("keepDeleted", true)).ToString();

            //Theme
            Preference themePreference = PreferenceScreen.FindPreference("theme");
            themePreference.PreferenceClick += ChangeTheme;
            themePreference.Summary = prefManager.GetInt("theme", 0) == 0 ? "White Theme" : "Dark Theme";

            //Check For Update
            Preference updatePreference = PreferenceScreen.FindPreference("update");
            updatePreference.PreferenceClick += UpdatePreference_PreferenceClick;

            //Version Number
            Preference versionPreference = PreferenceScreen.FindPreference("version");
            string VersionAsset;
            AssetManager assets = Application.Context.Assets;
            using (StreamReader sr = new StreamReader(assets.Open("Version.txt")))
            {
                VersionAsset = sr.ReadToEnd();
            }

            string version = VersionAsset.Substring(9, 5);
            if (version.EndsWith(".0"))
                version = version.Substring(0, 3);
            versionPreference.Summary = "V. " + version;

            //Account
            Preference accountPreference = PreferenceScreen.FindPreference("account");

            if (MainActivity.account != null)
            {
                accountPreference.Title = "Logged in as:";
                accountPreference.Summary = MainActivity.account.DisplayName;
            }
        }

        private async void AskForPermission()
        {
            await Task.Delay(100);
            MainActivity.instance.GetStoragePermission();
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

        #region Topic Preference
        private void TopicPreference(object sender, Preference.PreferenceClickEventArgs e)
        {
            Preferences.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.PreferenceFragment, TopicSelector.NewInstance()).AddToBackStack(null).Commit();
            Preferences.instance.toolbar.Title = "Music Genres";
        }
        #endregion

        #region Skip Verification
        private void SkipClick(object sender, Preference.PreferenceClickEventArgs e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Always play youtube file even if you have already downloaded the track:");
            builder.SetItems(new[] { "True", "False" }, (s, args) =>
            {
                ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                ISharedPreferencesEditor editor = pref.Edit();
                editor.PutBoolean("skipExistVerification", args.Which == 0);
                editor.Apply();

                Preference prefButton = FindPreference("skipExistVerification");
                prefButton.Summary = args.Which == 0 ? "True" : "False";
            });
            builder.Show();
        }
        #endregion

        #region LocalShortcut
        private void LocalShortcut(object sender, Preference.PreferenceClickEventArgs e)
        {
            string[] items = new string[] { "Shuffle All Audio Files", "Shuffle a playlist" };

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Set the local storage shortcut:");
            builder.SetItems(items, (s, args) => { if (args.Which == 0) LCShuffleAll(); else LCSufflePlaylist(); });
            builder.Show();
        }

        void LCShuffleAll()
        {
            ISharedPreferences pref = Android.Support.V7.Preferences.PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            ISharedPreferencesEditor editor = pref.Edit();
            editor.PutString("localPlay", "Shuffle All Audio Files");
            editor.Apply();

            Preference prefButton = FindPreference("localPlay");
            prefButton.Summary = "Shuffle All Audio Files";
        }

        void LCSufflePlaylist()
        {
            List<string> playList = new List<string>();
            List<long> playlistId = new List<long>();

            Android.Net.Uri uri = Playlists.ExternalContentUri;
            CursorLoader loader = new CursorLoader(Application.Context, uri, null, null, null, null);
            ICursor cursor = (ICursor)loader.LoadInBackground();

            if (cursor != null && cursor.MoveToFirst())
            {
                int nameID = cursor.GetColumnIndex(Playlists.InterfaceConsts.Name);
                int listID = cursor.GetColumnIndex(Playlists.InterfaceConsts.Id);
                do
                {
                    string name = cursor.GetString(nameID);
                    long id = cursor.GetLong(listID);
                    playList.Add(name);
                    playlistId.Add(id);

                }
                while (cursor.MoveToNext());
                cursor.Close();
            }

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Set the local storage shortcut:");
            builder.SetSingleChoiceItems(playList.ToArray(), -1, (s, args) => { LSposition = args.Which; });
            builder.SetPositiveButton("Ok", (s, args) => { LCSufflePlaylist(playList[LSposition], playlistId[LSposition]); });
            builder.SetNegativeButton("Cancel", (s, args) => { return; });
            builder.Show();
        }

        void LCSufflePlaylist(string playlist, long playlistID)
        {
            ISharedPreferences pref = Android.Support.V7.Preferences.PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            ISharedPreferencesEditor editor = pref.Edit();
            editor.PutString("localPlay", "Shuffle " + playlist);
            editor.PutLong("localPlaylistID", playlistID);
            editor.Apply();

            Preference prefButton = FindPreference("localPlay");
            prefButton.Summary = "Shuffle " + playlist;
        }
        #endregion

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
            builder.SetTitle("Choose the maximum number of current download:");
            builder.SetView(pickerView);
            NumberPicker picker = (NumberPicker)pickerView;
            picker.MinValue = 1;
            picker.MaxValue = 10;
            picker.Value = int.Parse(FindPreference("maxDownload").Summary);

            builder.SetPositiveButton("Apply", (s, eventArg) => 
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
            builder.SetNegativeButton("Cancel", (s, eventArg) => { });
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
            builder.SetTitle("Choose a theme :");
            builder.SetItems(new[] { "White Theme", "Dark Theme" }, (s, args) =>
            {
                ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                ISharedPreferencesEditor editor = pref.Edit();
                editor.PutInt("theme", args.Which);
                editor.Apply();

                Preference prefButton = FindPreference("theme");
                prefButton.Summary = args.Which == 0 ? "White Theme" : "Dark Theme";

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
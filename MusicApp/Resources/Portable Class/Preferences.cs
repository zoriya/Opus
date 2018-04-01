using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Preferences;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Android.Provider.MediaStore.Audio;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace MusicApp.Resources.Portable_Class
{
    [Activity(Label = "Settings", Theme = "@style/Theme")]
    public class Preferences : PreferenceActivity
    {
        public static Preferences instance;
        public Toolbar toolbar;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if(MainActivity.Theme == 1)
                SetTheme(Resource.Style.DarkPreferences);

            instance = this;

            FragmentManager.BeginTransaction().Replace(Android.Resource.Id.Content, new PreferencesFragment()).Commit();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            instance = null;
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
            LinearLayout root = (LinearLayout)FindViewById(Android.Resource.Id.List).Parent.Parent.Parent;
            toolbar = (Toolbar)LayoutInflater.From(this).Inflate(Resource.Layout.PreferenceToolbar, root, false);
            root.AddView(toolbar, 0);
            toolbar.Title = "Settings";
            toolbar.NavigationClick += (sender, e) => 
            {
                if(DownloadFragment.instance == null)
                    Finish();
                else
                {
                    ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(this);
                    ISharedPreferencesEditor editor = prefManager.Edit();
                    editor.PutString("downloadPath", DownloadFragment.instance.path);
                    editor.Apply();
                    FragmentManager.BeginTransaction().Replace(Android.Resource.Id.ListContainer, new PreferencesFragment()).AddToBackStack(null).Commit();
                    DownloadFragment.instance = null;
                }
            };
        }
    }

    public class PreferencesFragment : PreferenceFragment
    {
        public static PreferencesFragment instance;
        private View view;

        //Local Shortcut
        private int LSposition;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AskForPermission();

            instance = this;
            AddPreferencesFromResource(Resource.Layout.Preferences);
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);

            //Music Genres
            Preference topicPreference = PreferenceScreen.FindPreference("topics");
            topicPreference.PreferenceClick += TopicPreference;
            //topicPreference.Summary;//prefManager.GetString("downloadPath", "not set");

            //Download Path
            Preference downloadPref = PreferenceScreen.FindPreference("downloadPath");
            downloadPref.PreferenceClick += DownloadClick;
            downloadPref.Summary = prefManager.GetString("downloadPath", "not set");

            //Local play shortcut
            Preference localShortcutPreference = PreferenceScreen.FindPreference("localPlay");
            localShortcutPreference.PreferenceClick += LocalShortcut;
            localShortcutPreference.Summary = prefManager.GetString("localPlay", "Shuffle All Audio Files");

            //Theme
            Preference themePreference = PreferenceScreen.FindPreference("theme");
            themePreference.PreferenceClick += ChangeTheme;
            themePreference.Summary = prefManager.GetInt("theme", 0) == 0 ? "White Theme" : "Dark Theme";

            //SmallOnTop
            Preference smallOnTopPreference = PreferenceScreen.FindPreference("smallOnTop");
            smallOnTopPreference.PreferenceClick += SmallOnTop;
            smallOnTopPreference.Summary = prefManager.GetBoolean("smallOnTop", false) ? "Comming soon" : "Comming Soon"/*"True" : "False"*/;
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
            view.SetPadding(0, MainActivity.instance.SupportActionBar.Height, 0, 0);
            return view;
        }

        #region Topic Preference
        private void TopicPreference(object sender, Preference.PreferenceClickEventArgs e)
        {
            FragmentManager.BeginTransaction().Replace(Android.Resource.Id.ListContainer, TopicSelector.NewInstance()).AddToBackStack(null).Commit();
            instance = null;
            Preferences.instance.toolbar.Title = "Music Genres";
        }
        #endregion

        #region Download location
        private void DownloadClick(object sender, Preference.PreferenceClickEventArgs e)
        {
            FragmentManager.BeginTransaction().Replace(Android.Resource.Id.ListContainer, DownloadFragment.NewInstance()).AddToBackStack(null).Commit();
            instance = null;
            Preferences.instance.toolbar.Title = "Download Location";
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
            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
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
            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            ISharedPreferencesEditor editor = pref.Edit();
            editor.PutString("localPlay", "Shuffle " + playlist);
            editor.PutLong("localPlaylistID", playlistID);
            editor.Apply();

            Preference prefButton = FindPreference("localPlay");
            prefButton.Summary = "Shuffle " + playlist;
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
                //MainActivity.Theme = args.Which;
                //MainActivity.dialogTheme = args.Which == 0 ? Resource.Style.AppCompatAlertDialogStyle : Resource.Style.AppCompatDarkAlertDialogStyle;
                Activity.Recreate();
            });
            builder.Show();
        }
        #endregion

        #region SmallOnTop
        private void SmallOnTop(object sender, Preference.PreferenceClickEventArgs e)
        {
            new AlertDialog.Builder(Activity, MainActivity.dialogTheme)
                .SetTitle("Display the small player on top of the bottom navigation :")
                .SetItems(new[] { "True", "False" }, (s, args) =>
                {
                    ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                    ISharedPreferencesEditor editor = pref.Edit();
                    editor.PutBoolean("smallOnTop", args.Which == 0 ? true : false);
                    editor.Apply();

                    Preference prefButton = FindPreference("smallOnTop");
                    prefButton.Summary = args.Which == 0 ? "True" : "False";
                })
                .Show();
        }
        #endregion
    }
}
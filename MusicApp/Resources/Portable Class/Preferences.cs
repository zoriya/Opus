using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Preferences;
using Android.Views;
using Android.Widget;
using Java.IO;
using MusicApp.Resources.values;
using System;
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
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if(MainActivity.Theme == 1)
                SetTheme(Resource.Style.DarkPreferences);

            FragmentManager.BeginTransaction().Replace(Android.Resource.Id.Content, new PreferencesFragment()).Commit();
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
            LinearLayout root = (LinearLayout) FindViewById(Android.Resource.Id.List).Parent.Parent.Parent;
            Toolbar toolbar = (Toolbar) LayoutInflater.From(this).Inflate(Resource.Layout.PreferenceToolbar, root, false);
            root.AddView(toolbar, 0);
            toolbar.Title = "Settings";
            toolbar.NavigationClick += (sender, e) => { Finish(); };
        }
    }

    public class PreferencesFragment : PreferenceFragment
    {
        public static PreferencesFragment instance;

        //DownloadPath
        private List<Folder> folders;
        private FolderAdapter adapter;
        private ListView folderList;
        private string path;
        private AlertDialog dialog;

        //Local Shortcut
        private int LSposition;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AskForPermission();

            instance = this;
            AddPreferencesFromResource(Resource.Layout.Preferences);
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);

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
            smallOnTopPreference.Summary = prefManager.GetBoolean("smallOnTop", false) ? "True" : "False";
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
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            view.SetPadding(0, MainActivity.instance.SupportActionBar.Height, 0, 0);
            return view;
        }

        #region Download location
        private void DownloadClick(object sender, Preference.PreferenceClickEventArgs e)
        {
            folders = ListFolders();
            adapter = new FolderAdapter(Application.Context, Resource.Layout.folderList, folders);

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Choose download location:");
            builder.SetAdapter(adapter, (senderAlert, args) => {  });
            builder.SetPositiveButton("Ok", (senders, args) => { SetDownloadFolder(); });
            builder.SetNegativeButton("Cancel", (s, args) => { return; });
            dialog = builder.Create();

            folderList = dialog.ListView;

            dialog.ListView.FastScrollEnabled = true;
            dialog.ListView.SmoothScrollbarEnabled = true;
            dialog.ListView.ItemClick += ListView_ItemClick;
            dialog.Show();
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Folder folder = folders[e.Position];
            if (folder.asChild)
            {
                if (!folder.isExtended)
                    ExpandFolder(folder);
                else
                    UnexpandFolder(folder);
            }
            else
                Select(folder);
        }

        void ExpandFolder(Folder folder)
        {
            int index = folders.IndexOf(folder) + 1;
            List<Folder> childs = ListChilds(folder.uri);
            for (int i = 0; i < childs.Count; i++)
            {
                childs[i].Padding = folders[index - 1].Padding;
                childs[i].Padding += 30;
                folders.Insert(index + i, childs[i]);
                adapter.Insert(childs[i], index + i);
            }

            if (index - 1 < adapter.selectedPosition)
                adapter.selectedPosition += childs.Count;
            folders[index - 1].isExtended = true;
            folders[index - 1].childCount = childs.Count;

            adapter.NotifyDataSetChanged();
        }

        void UnexpandFolder(Folder folder)
        {
            int index = folders.IndexOf(folder);
            int count = folders[index].childCount;
            folders[index].isExtended = false;
            for (int i = index + 1; i < index + count; i++)
            {
                adapter.Remove(folders[i]);
            }

            if (adapter.selectedPosition != index && adapter.selectedPosition > index - count && adapter.selectedPosition < index + count)
                adapter.selectedPosition = -1;
            else if (adapter.selectedPosition != index)
                adapter.selectedPosition -= count;

            folders.RemoveRange(index + 1, count);
            adapter.NotifyDataSetChanged();

            dialog.ListView.ScrollBarSize = 10;
        }

        void Select(Folder folder)
        {
            path = folder.uri;
        }

        public void Used_Click(object sender, EventArgs e)
        {
            RadioButton radio = (RadioButton) sender;
            adapter.selectedPosition = (int)radio.GetTag(Resource.Id.folderName);
            adapter.NotifyDataSetChanged();

            path = (string)radio.GetTag(Resource.Id.folderUsed);
        }

        void SetDownloadFolder()
        {
            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            ISharedPreferencesEditor editor = pref.Edit();
            editor.PutString("downloadPath", path);
            editor.Apply();

            Preference prefButton = FindPreference("downloadPath");
            prefButton.Summary = path;
        }

        List<Folder> ListFolders()
        {
            File folderPath = Android.OS.Environment.ExternalStorageDirectory;

            File[] file = folderPath.ListFiles();

            if (file == null)
            {
                System.Console.WriteLine("&file is null");
                return new List<Folder>();
            }

            List<Folder> folders = new List<Folder>();
            for (int i = 0; i < file.Length; i++)
            {
                if (file[i].IsDirectory)
                {
                    bool asChild = false;

                    File[] childs = file[i].ListFiles();

                    for (int j = 0; i < childs.Length; i++)
                    {
                        if (childs[j].IsDirectory)
                        {
                            asChild = true;
                            break;
                        }
                    }

                    Folder folder = new Folder(file[i].Name, file[i].Path, asChild);

                    folders.Add(folder);
                }
            }
            return folders;
        }

        List<Folder> ListChilds(string path)
        {
            File folderPath = new File(path);

            File[] files = folderPath.ListFiles();

            List<Folder> folders = new List<Folder>();

            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].IsDirectory)
                {
                    bool asChild = false;

                    File[] childs = files[i].ListFiles();

                    for (int j = 0; j < childs.Length; j++)
                    {
                        if (childs[j].IsDirectory)
                        {
                            asChild = true;
                            continue;
                        }
                    }

                    Folder folder = new Folder(files[i].Name, files[i].Path, asChild);

                    folders.Add(folder);
                }
            }
            return folders;
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

                MainActivity.Theme = args.Which;
                MainActivity.dialogTheme = args.Which == 0 ? Resource.Style.AppCompatAlertDialogStyle : Resource.Style.AppCompatDarkAlertDialogStyle;
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
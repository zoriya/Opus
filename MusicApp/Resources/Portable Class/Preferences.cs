using Android.Support.V4.App;
using Android.Support.V7.Preferences;
using Android.Support.V7.App;
using Android.Views;
using Android.OS;
using System;
using System.Collections.Generic;
using MusicApp.Resources.values;
using Java.IO;
using Android.Widget;
using Android.Content;

namespace MusicApp.Resources.Portable_Class
{
    public class Preferences : PreferenceFragmentCompat
    {
        public static Preferences instance;
        private List<Folder> folders;
        private FolderAdapter adapter;
        private ListView folderList;
        private string path;
        private AlertDialog dialog;

        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey)
        {
            AddPreferencesFromResource(Resource.Layout.Preferences);
            Preference pref = PreferenceScreen.FindPreference("downloadPath");
            pref.PreferenceClick += Pref_PreferenceClick;

            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            pref.Summary = prefManager.GetString("downloadPath", "not set");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            //view.SetPadding(0, 150, 0, 0);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new Preferences { Arguments = new Bundle() };
            return instance;
        }

        private void Pref_PreferenceClick(object sender, Preference.PreferenceClickEventArgs e)
        {
            folders = ListFolders();
            adapter = new FolderAdapter(Android.App.Application.Context, Resource.Layout.folderList, folders);

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
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

            if (index -1 < adapter.selectedPosition)
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

            Preference prefButton = PreferenceScreen.FindPreference("downloadPath");
            prefButton.Summary = path;
        }

        List<Folder> ListFolders()
        {
            File folderPath = Android.OS.Environment.ExternalStorageDirectory;

            File[] file = folderPath.ListFiles();

            if(file == null)
            {
                System.Console.WriteLine("file is null");
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
    }
}
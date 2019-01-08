using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Java.IO;
using MusicApp.Resources.values;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicApp.Resources.Portable_Class
{
    [Register("MusicApp/DownloadFragment")]
    public class DownloadFragment : ListFragment
    {
        public static DownloadFragment instance;
        public string path;

        private List<Folder> folders;
        private FolderAdapter adapter;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            folders = ListFolders();
            adapter = new FolderAdapter(Android.App.Application.Context, Resource.Layout.folderList, folders);
            if (path != null)
                adapter.selectedPosition = folders.FindIndex(x => x.uri == path);
            else
                adapter.selectedPosition = -1;
            ListView.Divider = null;
            ListView.ItemClick += ListView_ItemClick;
            ListView.TextFilterEnabled = true;
            ListView.DividerHeight = 0;
            ListAdapter = adapter;
        }

        public static Fragment NewInstance(string path)
        {
            instance = new DownloadFragment { Arguments = new Bundle() };
            instance.path = path;
            return instance;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            instance = null;
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
                    for (int j = 0; j < childs.Length; j++)
                    {
                        if (!childs[j].IsDirectory)
                            continue;

                        asChild = true;
                        break;
                    }

                    Folder folder = new Folder(file[i].Name, file[i].Path, asChild);
                    folders.Add(folder);
                }
            }

            List<Folder> folderList = folders.OrderBy(x => x.name).ToList();
            folders = folderList;
            return folders;
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
                Select(folder, e.View);
        }

        public void Used_Click(object sender, EventArgs e)
        {
            RadioButton radio = (RadioButton)sender;
            adapter.selectedPosition = (int)radio.GetTag(Resource.Id.folderName);
            adapter.NotifyDataSetChanged();

            path = (string)radio.GetTag(Resource.Id.folderUsed);
        }

        private void Select(Folder folder, View view)
        {
            path = folder.uri;
            view.FindViewById<RadioButton>(Resource.Id.folderUsed).CallOnClick();
        }

        private void ExpandFolder(Folder folder)
        {
            int index = folders.IndexOf(folder);
            List<Folder> childs = ListChilds(folder.uri);

            for (int i = 0; i < childs.Count; i++)
            {
                childs[i].Padding = folder.Padding + 30;
                adapter.Insert(childs[i], index + i + 1);
            }

            folders.InsertRange(index + 1, childs);
            folders[index].isExtended = true;
            folders[index].childCount = childs.Count;
            adapter.NotifyDataSetChanged();

            if (index < adapter.selectedPosition)
                adapter.selectedPosition += childs.Count;
        }

        private void UnexpandFolder(Folder folder)
        {
            int index = folders.IndexOf(folder);
            int count = folder.childCount;

            folders[index].isExtended = false;

            for (int i = 0; i < count + 1; i++)
            {
                if(folders[index + i].isExtended)
                    count += folders[index + i].childCount;


                adapter.Remove(folders[index + i]);
            }

            if(index < adapter.selectedPosition && adapter.selectedPosition < index + count)
            {
                adapter.selectedPosition = -1;
                path = null;
            }
            if (index < adapter.selectedPosition)
                adapter.selectedPosition -= count;

            folders.RemoveRange(index + 1, count);
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
                        if (!childs[j].IsDirectory)
                            continue;

                        asChild = true;
                        break;
                    }

                    Folder folder = new Folder(files[i].Name, files[i].Path, asChild);
                    folders.Add(folder);
                }
            }

            List<Folder> folderList = folders.OrderBy(x => x.name).ToList();
            folders = folderList;
            return folders;
        }

        public override void OnStop()
        {
            base.OnStop();
            Preferences.instance.toolbar.Title = "Settings";
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            if (MainActivity.Theme == 1)
                view.SetBackgroundColor(Color.Argb(225, 33, 33, 33));
            base.OnViewCreated(view, savedInstanceState);
        }
    }
}
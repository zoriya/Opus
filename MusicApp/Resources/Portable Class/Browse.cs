using Android;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TagLib;

namespace MusicApp.Resources.Portable_Class
{
    public class Browse : ListFragment
    {
        public static Browse instance;
        public static Context act;
        public static LayoutInflater inflater;
        public List<Song> musicList = new List<Song>();
        public List<Song> result;
        public Adapter adapter;
        public View emptyView;
        public bool focused = true;

        private View view;
        private readonly string[] actions = new string[] { "Play", "Play Next", "Play Last", "Add To Playlist", "Edit Metadata" };
        private bool isEmpty = false;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            act = Activity;
            inflater = LayoutInflater;
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoSong, null);
            ListView.EmptyView = emptyView;
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            ListView.Scroll += MainActivity.instance.Scroll;
            ListView.NestedScrollingEnabled = true;

            if (ListView.Adapter == null)
                MainActivity.instance.GetStoragePermission();
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
            base.OnDestroy();
            instance = null;
            act = null;
            inflater = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            this.view = view;
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new Browse { Arguments = new Bundle() };
            return instance;
        }

        public void PopulateList()
        {
            musicList = new List<Song>();

            Android.Net.Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int titleID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Title);
                int artistID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Artist);
                int albumID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Album);
                int thisID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    string Artist = musicCursor.GetString(artistID);
                    string Title = musicCursor.GetString(titleID);
                    string Album = musicCursor.GetString(albumID);
                    long AlbumArt = musicCursor.GetLong(musicCursor.GetColumnIndex(MediaStore.Audio.Albums.InterfaceConsts.AlbumId));
                    long id = musicCursor.GetLong(thisID);
                    string path = musicCursor.GetString(pathID);

                    if (Title == null)
                        Title = "Unknown Title";
                    if (Artist == null)
                        Artist = "Unknow Artist";
                    if (Album == null)
                        Album = "Unknow Album";

                    musicList.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            List<Song> songList = musicList.OrderBy(x => x.Title).ToList();
            musicList = songList;
            int listPadding = 0;
            if (adapter != null)
                listPadding = adapter.listPadding;
            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, musicList)
            {
                listPadding = listPadding
            };
            ListAdapter = adapter;
            ListView.TextFilterEnabled = true;
            ListView.ItemClick += ListView_ItemClick;
            ListView.ItemLongClick += ListView_ItemLongClick;

            if (adapter == null || adapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }

            //if (MainActivity.paddingBot > MainActivity.defaultPaddingBot && adapter.listPadding == 0)
            //    adapter.listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot;

            if(result != null)
            {
                if (adapter != null)
                    listPadding = adapter.listPadding;
                adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, result)
                {
                    listPadding = listPadding
                };
                ListAdapter = adapter;
            }
        }

        private void OnRefresh(object sender, System.EventArgs e)
        {
            if (!focused)
                return;
            Refresh();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public void Refresh()
        {
            PopulateList();
        }

        public void Search(string search)
        {
            result = new List<Song>();
            foreach(Song item in musicList)
            {
                if(item.Title.ToLower().Contains(search.ToLower()) || item.Artist.ToLower().Contains(search.ToLower()))
                {
                    result.Add(item);
                }
            }
            int listPadding = 0;
            if (adapter != null)
                listPadding = adapter.listPadding;
            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, result)
            {
                listPadding = listPadding
            };
            ListAdapter = adapter;
        }

        public void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Song item = musicList[e.Position];
            if (result != null)
                item = result[e.Position];

            item = CompleteItem(item);

            Play(item, ListView.GetChildAt(e.Position - ListView.FirstVisiblePosition).FindViewById<ImageView>(Resource.Id.albumArt));
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            Song item = musicList[e.Position];
            if (result != null)
                item = result[e.Position];

            More(item, e.Position);
        } 

        public void More(Song item, int position)
        {
            item = CompleteItem(item);

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        Play(item, ListView.GetChildAt(position - ListView.FirstVisiblePosition).FindViewById<ImageView>(Resource.Id.albumArt));
                        break;
                    case 1:
                        PlayNext(item);
                        break;
                    case 2:
                        PlayLast(item);
                        break;
                    case 3:
                        GetPlaylist(item);
                        break;
                    case 4:
                        EditMetadata(item, "Browse", ListView.OnSaveInstanceState());
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        public Song CompleteItem(Song item)
        {
            Stream stream = new FileStream(item.Path, FileMode.Open, FileAccess.Read);

            var meta = TagLib.File.Create(new StreamFileAbstraction(item.Path, stream, stream));
            string ytID = meta.Tag.Comment;
            stream.Dispose();

            return new Song(item.Title, item.Artist, item.Album, ytID, item.AlbumArt, item.Id, item.Path, item.IsYt, item.isParsed, item.queueSlot);
        }

        public static void Play(Song item, View albumArt)
        {
            MusicPlayer.queue?.Clear();
            MusicPlayer.UpdateQueueDataBase();
            MusicPlayer.currentID = -1;

            Context context = Android.App.Application.Context;
            Intent intent = new Intent(context, typeof(MusicPlayer));
            intent.PutExtra("file", item.Path);
            context.StartService(intent);

            MainActivity.instance.ShowSmallPlayer();
            MainActivity.instance.ShowPlayer();
        }

        public static void PlayNext(Song item)
        {
            Context context = Android.App.Application.Context;
            Intent intent = new Intent(context, typeof(MusicPlayer));
            intent.PutExtra("file", item.Path);
            intent.SetAction("PlayNext");
            context.StartService(intent);
        }

        public static void PlayLast(Song item)
        {
            Context context = Android.App.Application.Context;
            Intent intent = new Intent(context, typeof(MusicPlayer));
            intent.PutExtra("file", item.Path);
            intent.SetAction("PlayLast");
            context.StartService(intent);
        }

        public static void GetPlaylist(Song item)
        {
            List<string> playList = new List<string>();
            List<long> playListId = new List<long>();
            playList.Add("Create a playlist");
            playListId.Add(0);

            Android.Net.Uri uri = MediaStore.Audio.Playlists.ExternalContentUri;
            CursorLoader loader = new CursorLoader(Android.App.Application.Context, uri, null, null, null, null);
            ICursor cursor = (ICursor)loader.LoadInBackground();

            if (cursor != null && cursor.MoveToFirst())
            {
                int nameID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Name);
                int playlistID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Id);
                do
                {
                    string name = cursor.GetString(nameID);
                    long id = cursor.GetLong(playlistID);
                    playList.Add(name);
                    playListId.Add(id);
                }
                while (cursor.MoveToNext());
                cursor.Close();
            }

            AlertDialog.Builder builder = new AlertDialog.Builder(act, MainActivity.dialogTheme);
            builder.SetTitle("Add to a playlist");
            builder.SetItems(playList.ToArray(), (senderAlert, args) =>
            {
                AddToPlaylist(item, playList[args.Which], playListId[args.Which]);
            });
            builder.Show();
        }

        public static long GetPlaylistID(string playlistName)
        {
            Android.Net.Uri uri = MediaStore.Audio.Playlists.ExternalContentUri;
            Looper.Prepare();
            CursorLoader loader = new CursorLoader(Android.App.Application.Context, uri, null, null, null, null);
            ICursor cursor = (ICursor)loader.LoadInBackground();

            if (cursor != null && cursor.MoveToFirst())
            {
                int nameID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Name);
                int plID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Id);
                do
                {
                    string name = cursor.GetString(nameID);

                    if (name != playlistName)
                        continue;

                    return cursor.GetLong(plID);
                }
                while (cursor.MoveToNext());
                cursor.Close();
            }

            //Playlist do not exist, create it
            ContentResolver resolver = act.ContentResolver;
            ContentValues value = new ContentValues();
            value.Put(MediaStore.Audio.Playlists.InterfaceConsts.Name, playlistName);
            resolver.Insert(uri, value);

            CursorLoader loaderBis = new CursorLoader(Android.App.Application.Context, uri, null, null, null, null);
            ICursor cursorBis = (ICursor)loader.LoadInBackground();

            if (cursorBis != null && cursorBis.MoveToFirst())
            {
                int nameID = cursorBis.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Name);
                int getplaylistID = cursorBis.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Id);
                do
                {
                    string name = cursorBis.GetString(nameID);
                    long id = cursorBis.GetLong(getplaylistID);

                    if (playlistName == name)
                        return id;
                }
                while (cursorBis.MoveToNext());
                cursorBis.Close();
            }
            return -1;
        }

        public async static Task CheckWritePermission()
        {
            const string permission = Manifest.Permission.WriteExternalStorage;
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(act, permission) != (int)Permission.Granted)
            {
                string[] permissions = new string[] { permission };
                MainActivity.instance.RequestPermissions(permissions, 2659);

                await Task.Delay(1000);
                while (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(act, permission) != (int)Permission.Granted)
                    await Task.Delay(500);
            }
            return;
        }

        public async static void AddToPlaylist(Song item, string playList, long playlistID)
        {
            if (playList == "Create a playlist")
                CreatePlalistDialog(item);

            else
            {
                await CheckWritePermission();

                ContentResolver resolver = act.ContentResolver;
                ContentValues value = new ContentValues();
                value.Put(MediaStore.Audio.Playlists.Members.AudioId, item.Id);
                value.Put(MediaStore.Audio.Playlists.Members.PlayOrder, 0);
                resolver.Insert(MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistID), value);
            }
        }

        public static void CreatePlalistDialog(Song item)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(act, MainActivity.dialogTheme);
            builder.SetTitle("Playlist name");
            View view = inflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            builder.SetNegativeButton("Cancel", (senderAlert, args) => { });
            builder.SetPositiveButton("Create", (senderAlert, args) => 
            {
                CreatePlaylist(view.FindViewById<EditText>(Resource.Id.playlistName).Text, item);
            });
            builder.Show();
        }

        public async static void CreatePlaylist(string name, Song item)
        {
            await CheckWritePermission();

            ContentResolver resolver = act.ContentResolver;
            Android.Net.Uri uri = MediaStore.Audio.Playlists.ExternalContentUri;
            ContentValues value = new ContentValues();
            value.Put(MediaStore.Audio.Playlists.InterfaceConsts.Name, name);
            resolver.Insert(uri, value);

            string playList = "foo";
            long playlistID = 0;

            CursorLoader loader = new CursorLoader(Android.App.Application.Context, uri, null, null, null, null);
            ICursor cursor = (ICursor)loader.LoadInBackground();

            if (cursor != null && cursor.MoveToFirst())
            {
                int nameID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Name);
                int getplaylistID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Id);
                do
                {
                    string playlistName = cursor.GetString(nameID);
                    long id = cursor.GetLong(getplaylistID);

                    if (playlistName == name)
                        playlistID = id;
                }
                while (cursor.MoveToNext());
                cursor.Close();
            }

            AddToPlaylist(item, playList, playlistID);
        }

        public static void EditMetadata(Song item, string sender, IParcelable parcelable)
        {
            MainActivity.instance.HideTabs();
            MainActivity.parcelableSender = sender;
            MainActivity.parcelable = parcelable;
            Intent intent = new Intent(Android.App.Application.Context, typeof(EditMetaData));
            intent.PutExtra("Song", item.ToString());
            MainActivity.instance.StartActivity(intent);
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
            if(MainActivity.parcelable != null && MainActivity.parcelableSender == "Browse")
            {
                ListView.OnRestoreInstanceState(MainActivity.parcelable);
                MainActivity.parcelable = null;
                MainActivity.parcelableSender = null;
            }
        }
    }
}
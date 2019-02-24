using Android;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Opus.Resources.values;
using System.Collections.Generic;
using System.Threading.Tasks;
using CursorLoader = Android.Support.V4.Content.CursorLoader;

namespace Opus.Resources.Portable_Class
{
    public class FolderBrowse : Fragment
    {
        public static FolderBrowse instance;
        public List<string> pathDisplay = new List<string>();
        public List<string> paths = new List<string>();
        public List<int> pathUse = new List<int>();
        public RecyclerView ListView;
        public TwoLineAdapter adapter;
        public TextView EmptyView;
        public bool IsFocused = false;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.CompleteRecycler, container, false);
            EmptyView = view.FindViewById<TextView>(Resource.Id.empty);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));
            ListView.SetItemAnimator(new DefaultItemAnimator());
            //ListView.ScrollChange += (s, e) => { MainActivity.instance.contentRefresh.Enabled = e. == 0; };
            ListView.NestedScrollingEnabled = true;

            PopulateList();
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new FolderBrowse { Arguments = new Bundle() };
            return instance;
        }

        public void PopulateList()
        {
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Android.App.Application.Context, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
                return;

            pathDisplay.Clear();
            paths.Clear();
            pathUse.Clear();

            Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;
            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    string path = musicCursor.GetString(pathID);
                    path = path.Substring(0, path.LastIndexOf("/"));
                    string displayPath = path.Substring(path.LastIndexOf("/") + 1, path.Length - (path.LastIndexOf("/") + 1));

                    if (!paths.Contains(path))
                    {
                        pathDisplay.Add(displayPath);
                        paths.Add(path);
                        pathUse.Add(1);
                    }
                    else
                        pathUse[paths.IndexOf(path)] += 1;
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            adapter = new TwoLineAdapter(pathDisplay, pathUse);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongClick += ListView_ItemLongClick;
            ListView.SetAdapter(adapter);

            if (adapter.ItemCount == 0)
            {
                EmptyView.Visibility = ViewStates.Visible;
                EmptyView.Text = MainActivity.instance.Resources.GetString(Resource.String.no_song);
            }
        }

        private void OnRefresh(object sender, System.EventArgs e)
        {
            if (!IsFocused)
                return;
            Refresh();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public void Refresh()
        {
            PopulateList();
        }

        public void ListView_ItemClick(object sender, int position)
        {
            ListSongs(pathDisplay[position], paths[position]);
        }

        private void ListView_ItemLongClick(object sender, int position)
        {
            More(position);
        }

        public void More(int position)
        {
            string path = paths[position];
            string displayPath = pathDisplay[position];

            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = displayPath;
            bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = path;
            bottomView.FindViewById<ImageView>(Resource.Id.bsArt).Visibility = ViewStates.Gone;
            bottomSheet.SetContentView(bottomView);

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Folder, Resources.GetString(Resource.String.list_songs), (sender, eventArg) =>
                {
                    ListSongs(displayPath, path);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Play, Resources.GetString(Resource.String.play_in_order), (sender, eventArg) =>
                {
                    PlayInOrder(path);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Shuffle, Resources.GetString(Resource.String.random_play), (sender, eventArg) =>
                {
                    RandomPlay(path);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlaylistAdd, Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) =>
                {
                    GetPlaylist(path);
                    bottomSheet.Dismiss();
                })
            });
            bottomSheet.Show();
        }

        void ListSongs(string displayPath, string path)
        {
            AppCompatActivity act = (AppCompatActivity)Activity;
            act.SupportActionBar.SetHomeButtonEnabled(true);
            act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            act.SupportActionBar.Title = displayPath;

            MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, FolderTracks.NewInstance(path, displayPath)).AddToBackStack(null).Commit();
        }

        async void PlayInOrder(string folderPath)
        {
            List<Song> songs = new List<Song>();
            Uri musicUri = MediaStore.Audio.Media.GetContentUriForPath(folderPath);
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
                    string path = musicCursor.GetString(pathID);

                    if (!path.Contains(folderPath))
                        continue;

                    string Artist = musicCursor.GetString(artistID);
                    string Title = musicCursor.GetString(titleID);
                    string Album = musicCursor.GetString(albumID);
                    long AlbumArt = musicCursor.GetLong(musicCursor.GetColumnIndex(MediaStore.Audio.Albums.InterfaceConsts.AlbumId));
                    long id = musicCursor.GetLong(thisID);

                    if (Title == null)
                        Title = "Unknown Title";
                    if (Artist == null)
                        Artist = "Unknow Artist";
                    if (Album == null)
                        Album = "Unknow Album";

                    songs.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();

                songs.Reverse();
                Browse.Play(songs[0]);

                while (MusicPlayer.instance == null)
                    await Task.Delay(10);

                foreach (Song song in songs)
                {
                    MusicPlayer.instance.AddToQueue(song);
                }
            }
        }

        public void GetPlaylist(string path)
        {
            List<string> playList = new List<string>();
            List<long> playListId = new List<long>();
            playList.Add(GetString(Resource.String.create_playlist));
            playListId.Add(0);

            Uri uri = MediaStore.Audio.Playlists.ExternalContentUri;
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

            AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme);
            builder.SetTitle(Resource.String.save_folder_playlist);
            builder.SetItems(playList.ToArray(), (senderAlert, args) =>
            {
                AddToPlaylist(path, playList[args.Which], playListId[args.Which]);
            });
            builder.Show();
        }

        public async void AddToPlaylist(string path, string playList, long playlistID)
        {
            if (playList == GetString(Resource.String.create_playlist))
                CreatePlalistDialog(path);

            else
            {
                await Browse.CheckWritePermission();

                List<ContentValues> values = new List<ContentValues>();
                ContentResolver resolver = MainActivity.instance.ContentResolver;

                Uri musicUri = MediaStore.Audio.Media.GetContentUriForPath(path);
                CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
                ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

                if (musicCursor != null && musicCursor.MoveToFirst())
                {
                    int thisID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
                    int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                    do
                    {
                        string songPath = musicCursor.GetString(pathID);

                        if (!songPath.Contains(path))
                            continue;

                        long id = musicCursor.GetLong(thisID);

                        ContentValues value = new ContentValues();
                        value.Put(MediaStore.Audio.Playlists.Members.AudioId, id);
                        value.Put(MediaStore.Audio.Playlists.Members.PlayOrder, 0);
                        values.Add(value);
                    }
                    while (musicCursor.MoveToNext());
                    musicCursor.Close();
                }
                resolver.BulkInsert(MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistID), values.ToArray());
            }
        }

        public void CreatePlalistDialog(string path)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme);
            builder.SetTitle(Resource.String.new_playlist);
            View view = LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            PlaylistLocationAdapter adapter = new PlaylistLocationAdapter(MainActivity.instance, Android.Resource.Layout.SimpleSpinnerItem, new string[] { MainActivity.instance.GetString(Resource.String.create_local), MainActivity.instance.GetString(Resource.String.create_youtube), MainActivity.instance.GetString(Resource.String.create_synced) })
            {
                YoutubeWorkflow = false
            };
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            view.FindViewById<Spinner>(Resource.Id.playlistLocation).Adapter = adapter;
            builder.SetNegativeButton(Resource.String.cancel, (senderAlert, args) => { });
            builder.SetPositiveButton(Resource.String.ok, (senderAlert, args) =>
            {
                CreatePlaylist(view.FindViewById<EditText>(Resource.Id.playlistName).Text, path);
            });
            builder.Show();
        }

        public async void CreatePlaylist(string name, string path)
        {
            await Browse.CheckWritePermission();

            ContentResolver resolver = MainActivity.instance.ContentResolver;
            Uri uri = MediaStore.Audio.Playlists.ExternalContentUri;
            ContentValues value = new ContentValues();
            value.Put(MediaStore.Audio.Playlists.InterfaceConsts.Name, name);
            resolver.Insert(uri, value);

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

            AddToPlaylist(path, "foo", playlistID);
        }

        void RandomPlay(string folderPath)
        {
            List<string> trackPaths = new List<string>();
            Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();


            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    string path = musicCursor.GetString(pathID);

                    if (path.Contains(folderPath))
                        trackPaths.Add(path);
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }


            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.PutStringArrayListExtra("files", trackPaths.ToArray());
            intent.SetAction("RandomPlay");
            Activity.StartService(intent);
            MainActivity.instance.ShowSmallPlayer();
            MainActivity.instance.ShowPlayer();
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
        }
    }
}
using Android.Content;
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System.Collections.Generic;
using System.Threading.Tasks;
using CursorLoader = Android.Support.V4.Content.CursorLoader;

namespace MusicApp.Resources.Portable_Class
{
    public class FolderTracks : Fragment
    {
        public static FolderTracks instance;
        public string folderName;
        private RecyclerView ListView;
        private View EmptyView;
        public BrowseAdapter adapter;
        public List<Song> result;
        public string path;

        private List<Song> tracks = new List<Song>();


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;

            MainActivity.instance.DisplaySearch(1);
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            instance = null;
            base.OnDestroy();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.YoutubeSearch, container, false);
            EmptyView = view.FindViewById<TextView>(Resource.Id.empty);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));
            ListView.SetItemAnimator(new DefaultItemAnimator());

            PopulateList();
            return view;
        }

        public static Fragment NewInstance(string path, string folderName)
        {
            instance = new FolderTracks { Arguments = new Bundle() };
            instance.path = path;
            instance.folderName = folderName;
            return instance;
        }

        void PopulateList()
        {
            Uri musicUri = MediaStore.Audio.Media.GetContentUriForPath(path);

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            tracks = new List<Song>();

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

                    if (!path.Contains(this.path))
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

                    tracks.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            adapter = new BrowseAdapter(tracks, false);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongClick;
            ListView.SetAdapter(adapter);
        }

        private void OnRefresh(object sender, System.EventArgs e)
        {
            tracks.Clear();

            Uri musicUri = MediaStore.Audio.Media.GetContentUriForPath(path);

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

                    if (!path.Contains(this.path))
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

                    tracks.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            adapter = new BrowseAdapter(tracks, false);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongClick;
            ListView.SetAdapter(adapter);
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public void Search(string search)
        {
            result = new List<Song>();
            foreach (Song item in tracks)
            {
                if (item.Title.ToLower().Contains(search.ToLower()) || item.Artist.ToLower().Contains(search.ToLower()))
                {
                    result.Add(item);
                }
            }
            adapter = new BrowseAdapter(result, false);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongClick;
            ListView.SetAdapter(adapter);
        }

        private async void ListView_ItemClick(object sender, int position)
        {
            Song item = tracks[position];
            List<Song> queue = tracks.GetRange(position + 1, tracks.Count - position - 1);
            if (result != null)
            {
                item = result[position];
                queue = result.GetRange(position + 1, result.Count - position - 1);
            }
            queue.Reverse();

            Browse.Play(item);

            while(MusicPlayer.instance == null)
                await Task.Delay(10);

            foreach(Song song in queue)
            {
                MusicPlayer.instance.AddToQueue(song);
            }
            Player.instance.UpdateNext();
        }

        private void ListView_ItemLongClick(object sender, int position)
        {
            Song item = tracks[position];
            if (result != null)
                item = result[position];

            More(item, position);
        }

        public void More(Song item, int position)
        {
            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Title;
            bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Artist;
            if (item.Album == null)
            {
                var songCover = Uri.Parse("content://media/external/audio/albumart");
                var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, item.AlbumArt);

                Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            else
            {
                Picasso.With(MainActivity.instance).Load(item.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            bottomSheet.SetContentView(bottomView);

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Play, Resources.GetString(Resource.String.play), async (sender, eventArg) => 
                {
                    int Position = tracks.IndexOf(item);

                    List<Song> queue = tracks.GetRange(Position + 1, tracks.Count - Position - 1);
                    if (result != null)
                    {
                        queue = result.GetRange(Position + 1, result.Count - Position - 1);
                    }
                    queue.Reverse();

                    Browse.Play(item);

                    while (MusicPlayer.instance == null)
                        await Task.Delay(10);

                    foreach (Song song in queue)
                    {
                        MusicPlayer.instance.AddToQueue(song);
                    }
                    Player.instance.UpdateNext();
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlaylistPlay, Resources.GetString(Resource.String.play_next), (sender, eventArg) => { Browse.PlayNext(item); bottomSheet.Dismiss(); }),
                new BottomSheetAction(Resource.Drawable.Queue, Resources.GetString(Resource.String.play_last), (sender, eventArg) => { Browse.PlayLast(item); bottomSheet.Dismiss(); }),
                new BottomSheetAction(Resource.Drawable.PlaylistAdd, Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) => { Browse.GetPlaylist(item); bottomSheet.Dismiss(); }),
                new BottomSheetAction(Resource.Drawable.Edit, Resources.GetString(Resource.String.edit_metadata), (sender, eventArg) => { Browse.EditMetadata(item); bottomSheet.Dismiss(); })
            });
            bottomSheet.Show();
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
        }
    }
}
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V7.App;
using Android.Support.V4.App;
using System.Collections.Generic;
using Android.Provider;
using Android.Database;
using Android.Content.PM;
using Android.Support.Design.Widget;
using Android;
using Android.Net;
using System.Threading.Tasks;
using Java.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MusicApp.Resources.values;
using Java.IO;
using Google.Apis.Auth.OAuth2;
using System.IO;
using Google.Apis.Auth.OAuth2.Flows;
using System.Collections;
using System.Threading;
using Google.Apis.Util.Store;

namespace MusicApp.Resources.Portable_Class
{
    public class YtPlaylist : ListFragment
    {
        public static YtPlaylist instance;
        public Adapter adapter;
        public View emptyView;
        public static Credentials credentials;

        private List<Song> playlists = new List<Song>();
        private string[] actions = new string[] { "Random play", "Rename", "Delete" };
        private bool isEmpty = false;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoPlaylist, null);
            ListView.EmptyView = emptyView;

            if (YoutubeEngine.youtubeService == null)
                MainActivity.instance.Login();

            GetYoutubePlaylists();
        }

        public override void OnDestroy()
        {
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            view.SetPadding(0, 100, 0, MainActivity.paddingBot);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new YtPlaylist { Arguments = new Bundle() };
            return instance;
        }

        async void GetYoutubePlaylists()
        {
            while (YoutubeEngine.youtubeService == null)
                await Task.Delay(100);

            System.Console.WriteLine("getting playlists");

            HashMap parameters = new HashMap();
            parameters.Put("part", "snippet,contentDetails");
            parameters.Put("mine", "true");
            parameters.Put("maxResults", "25");
            parameters.Put("onBehalfOfContentOwner", "");
            parameters.Put("onBehalfOfContentOwnerChannel", "");

            YouTubeService youtube = YoutubeEngine.youtubeService;

            PlaylistsResource.ListRequest ytPlaylists = youtube.Playlists.List(parameters.Get("part").ToString());

            if (parameters.ContainsKey("mine") && parameters.Get("mine").ToString() != "")
            {
                bool mine = (parameters.Get("mine").ToString() == "true") ? true : false;
                ytPlaylists.Mine = mine;
            }

            if (parameters.ContainsKey("maxResults"))
            {
                ytPlaylists.MaxResults = long.Parse(parameters.Get("maxResults").ToString());
            }

            if (parameters.ContainsKey("onBehalfOfContentOwner") && parameters.Get("onBehalfOfContentOwner").ToString() != "")
            {
                ytPlaylists.OnBehalfOfContentOwner = parameters.Get("onBehalfOfContentOwner").ToString();
            }

            if (parameters.ContainsKey("onBehalfOfContentOwnerChannel") && parameters.Get("onBehalfOfContentOwnerChannel").ToString() != "")
            {
                ytPlaylists.OnBehalfOfContentOwnerChannel = parameters.Get("onBehalfOfContentOwnerChannel").ToString();
            }

            PlaylistListResponse response = await ytPlaylists.ExecuteAsync();
            playlists = new List<Song>();

            for (int i = 0; i < response.Items.Count; i++)
            {
                Google.Apis.YouTube.v3.Data.Playlist playlist = response.Items[i];
                Song song = new Song(playlist.Snippet.Title, playlist.Snippet.ChannelTitle, playlist.Snippet.Thumbnails.Default__.Url, -1, -1, playlist.Id, true);
                playlists.Add(song);
            }

            Adapter ytAdapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, playlists);
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            AppCompatActivity act = (AppCompatActivity)Activity;
            act.SupportActionBar.SetHomeButtonEnabled(true);
            act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            act.SupportActionBar.Title = playlists[e.Position].GetName();
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            transaction.Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlists[e.Position].GetPath()));
            transaction.AddToBackStack(null);
            transaction.Commit();
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            //AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
            //builder.SetTitle("Pick an action");
            //builder.SetItems(actions, (senderAlert, args) =>
            //{
            //    switch (args.Which)
            //    {
            //        case 0:
            //            RandomPlay(playlists[e.Position].GetPath());
            //            break;
            //        case 1:
            //            Rename(e.Position, playlistId[e.Position]);
            //            break;
            //        case 2:
            //            RemovePlaylist(e.Position, playlistId[e.Position]);
            //            break;
            //        default:
            //            break;
            //    }
            //});
            //builder.Show();
        }

        void RandomPlay(long playlistID)
        {
            List<string> tracksPath = new List<string>();
            Uri musicUri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistID);

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();


            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    string path = musicCursor.GetString(pathID);

                    tracksPath.Add(path);
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.PutStringArrayListExtra("files", tracksPath);
            intent.SetAction("RandomPlay");
            Activity.StartService(intent);
        }

        void Rename(int position, long playlistID)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
            builder.SetTitle("Playlist name");
            View view = LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            builder.SetNegativeButton("Cancel", (senderAlert, args) => { });
            builder.SetPositiveButton("Rename", (senderAlert, args) =>
            {
                RenamePlaylist(position, view.FindViewById<EditText>(Resource.Id.playlistName).Text, playlistID);
            });
            builder.Show();
        }

        void RenamePlaylist(int position, string name, long playlistID)
        {
            //ContentResolver resolver = Activity.ContentResolver;
            //Uri uri = MediaStore.Audio.Playlists.ExternalContentUri;
            //ContentValues value = new ContentValues();
            //value.Put(MediaStore.Audio.Playlists.InterfaceConsts.Name, name);
            //resolver.Update(MediaStore.Audio.Playlists.ExternalContentUri, value, MediaStore.Audio.Playlists.InterfaceConsts.Id + "=?", new string[] { playlistID.ToString() });
            //playList[position] = name;
            //ListAdapter = new ArrayAdapter(Android.App.Application.Context, Resource.Layout.PlaylistList, playList);
            //if (ListAdapter.Count == 0)
            //{
            //    isEmpty = true;
            //    Activity.AddContentView(emptyView, View.LayoutParameters);
            //}
        }

        void RemovePlaylist(int position, long playlistID)
        {
            //ContentResolver resolver = Activity.ContentResolver;
            //Uri uri = MediaStore.Audio.Playlists.ExternalContentUri;
            //resolver.Delete(MediaStore.Audio.Playlists.ExternalContentUri, MediaStore.Audio.Playlists.InterfaceConsts.Id + "=?", new string[] { playlistID.ToString() });
            //playList.RemoveAt(position);
            //playlistId.RemoveAt(position);
            //ListAdapter = new ArrayAdapter(Android.App.Application.Context, Resource.Layout.PlaylistList, playList);
            //if (ListAdapter.Count == 0)
            //{
            //    isEmpty = true;
            //    Activity.AddContentView(emptyView, View.LayoutParameters);
            //}
        }
    }
}
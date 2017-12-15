using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Java.Util;
using MusicApp.Resources.values;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class YtPlaylist : ListFragment
    {
        public static YtPlaylist instance;
        public Adapter adapter;
        public View emptyView;
        public bool isEmpty = false;

        private List<Song> playlists = new List<Song>();
        private List<Google.Apis.YouTube.v3.Data.Playlist> YtPlaylists = new List<Google.Apis.YouTube.v3.Data.Playlist>();
        private string[] actions = new string[] { "Random play", "Rename", "Delete", "Download" };


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoYtPlaylist, null);
            ListView.EmptyView = emptyView;

            if (YoutubeEngine.youtubeService == null)
                MainActivity.instance.Login();

            GetYoutubePlaylists();
        }

        public void AddEmptyView()
        {
            Activity.AddContentView(emptyView, View.LayoutParameters);
        }

        public void RemoveEmptyView()
        {
            ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
            rootView.RemoveView(emptyView);
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
            view.SetPadding(0, 0, 0, MainActivity.paddingBot);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new YtPlaylist { Arguments = new Bundle() };
            return instance;
        }

        async void GetYoutubePlaylists()
        {
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

            if (instance == null)
                return;

            playlists = new List<Song>();

            for (int i = 0; i < response.Items.Count; i++)
            {
                Google.Apis.YouTube.v3.Data.Playlist playlist = response.Items[i];
                YtPlaylists.Add(playlist);
                Song song = new Song(playlist.Snippet.Title, playlist.Snippet.ChannelTitle, playlist.Snippet.Thumbnails.Default__.Url, -1, -1, playlist.Id, true);
                playlists.Add(song);
            }

            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, playlists);
            ListAdapter = adapter;
            ListView.Adapter = adapter;
            ListView.ItemClick += ListView_ItemClick;
            ListView.ItemLongClick += ListView_ItemLongClick;

            if (adapter == null || adapter.Count == 0)
            {
                if (isEmpty)
                    return;
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            AppCompatActivity act = (AppCompatActivity)Activity;
            act.SupportActionBar.SetHomeButtonEnabled(true);
            act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            act.SupportActionBar.Title = playlists[e.Position].GetName();

            MainActivity.instance.HideTabs();
            MainActivity.instance.Transition(Resource.Id.contentView, PlaylistTracks.NewInstance(playlists[e.Position].GetPath()), true);
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            More(e.Position);
        }

        public void More(int position)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        RandomPlay(playlists[position].GetPath());
                        break;
                    case 1:
                        Rename(position, playlists[position].GetPath());
                        break;
                    case 2:
                        RemovePlaylist(position, playlists[position].GetPath());
                        break;
                    case 3:
                        DownloadPlaylist(playlists[position].GetPath());
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        async void RandomPlay(string playlistID)
        {
            List<Song> tracks = new List<Song>();
            string nextPageToken = "";
            while (nextPageToken != null)
            {
                var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = playlistID;
                ytPlaylistRequest.MaxResults = 50;
                ytPlaylistRequest.PageToken = nextPageToken;

                var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                foreach (var item in ytPlaylist.Items)
                {
                    Song song = new Song(item.Snippet.Title, "", item.Snippet.Thumbnails.Default__.Url, -1, -1, item.ContentDetails.VideoId, true, false);
                    tracks.Add(song);
                }

                nextPageToken = ytPlaylist.NextPageToken;
            }
            YoutubeEngine.PlayFiles(tracks.ToArray());
        }

        void Rename(int position, string playlistID)
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

        void RenamePlaylist(int position, string name, string playlistID)
        {
            Google.Apis.YouTube.v3.Data.Playlist playlist = new Google.Apis.YouTube.v3.Data.Playlist();
            playlist.Snippet = YtPlaylists[position].Snippet;
            playlist.Snippet.Title = name;
            playlist.Id = playlistID;
        
            YtPlaylists[position].Snippet.Title = name;
            YoutubeEngine.youtubeService.Playlists.Update(playlist, "snippet").Execute();

            playlists[position].SetName(name);
            adapter.NotifyDataSetChanged();
            if (ListAdapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        void RemovePlaylist(int position, string playlistID)
        {
            HashMap parameters = new HashMap();
            parameters.Put("id", playlistID);
            parameters.Put("onBehalfOfContentOwner", "");

            PlaylistsResource.DeleteRequest deleteRequest = YoutubeEngine.youtubeService.Playlists.Delete(playlistID);
            if (parameters.ContainsKey("onBehalfOfContentOwner") && parameters.Get("onBehalfOfContentOwner").ToString() != "")
            {
                deleteRequest.OnBehalfOfContentOwner = parameters.Get("onBehalfOfContentOwner").ToString();
            }

            deleteRequest.Execute();

            playlists.RemoveAt(position);
            YtPlaylists.RemoveAt(position);
            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, playlists);
            ListAdapter = adapter;
            ListView.Adapter = adapter;
            if (ListAdapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        private async void DownloadPlaylist(string playlistID)
        {
            List<string> names = new List<string>();
            List<string> videoIDs = new List<string>();
            string nextPageToken = "";
            while (nextPageToken != null)
            {
                var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = playlistID;
                ytPlaylistRequest.MaxResults = 50;
                ytPlaylistRequest.PageToken = nextPageToken;

                var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                foreach (var item in ytPlaylist.Items)
                {
                    names.Add(item.Snippet.Title);
                    videoIDs.Add(item.ContentDetails.VideoId);
                }

                nextPageToken = ytPlaylist.NextPageToken;
            }
            YoutubeEngine.DownloadFiles(names.ToArray(), videoIDs.ToArray());
        }

        public void OnPageScrollStateChanged(int state)
        {
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
        {
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
        }

        public void OnPageSelected(int position)
        {
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
        }
    }
}
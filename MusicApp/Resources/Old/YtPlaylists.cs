using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    public class YtPlaylist : ListFragment
    {
        public static YtPlaylist instance;
        public Adapter adapter;
        public View emptyView;
        public bool isEmpty = false;
        public bool focused = false;

        private bool useMixes = false;
        private List<Song> playlists = new List<Song>();
        private List<Google.Apis.YouTube.v3.Data.Playlist> YtPlaylists = new List<Google.Apis.YouTube.v3.Data.Playlist>();
        private string[] actions = new string[] { "Random play", "Rename", "Delete", "Download" };
        private string[] mixActions = new string[] { "Random play", "Download" };


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoYtPlaylist, null);
            ListView.EmptyView = emptyView;
            ListView.Scroll += MainActivity.instance.Scroll;
            MainActivity.instance.pagerRefresh.Refresh += OnRefresh;

            if (YoutubeEngine.youtubeService == null)
                MainActivity.instance.Login();

            if(playlists.Count == 0)
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
            MainActivity.instance.pagerRefresh.Refresh -= OnRefresh;
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
            view.SetPadding(0, 0, 0, MainActivity.defaultPaddingBot);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new YtPlaylist { Arguments = new Bundle() };
            return instance;
        }

        public static Fragment NewInstance(List<Song> playlists)
        {
            instance = new YtPlaylist { Arguments = new Bundle() };
            instance.playlists = playlists;
            instance.useMixes = true;
            return instance;
        }

        async void GetYoutubePlaylists()
        {
            if (MainActivity.instance.TokenHasExpire())
            {
                YoutubeEngine.youtubeService = null;
                MainActivity.instance.Login();

                while (YoutubeEngine.youtubeService == null)
                    await Task.Delay(500);
            }

            YouTubeService youtube = YoutubeEngine.youtubeService;

            PlaylistsResource.ListRequest ytPlaylists = youtube.Playlists.List("snippet,contentDetails");
            ytPlaylists.Mine = true;
            ytPlaylists.MaxResults = 25;
            PlaylistListResponse response = await ytPlaylists.ExecuteAsync();

            if (instance == null)
                return;

            playlists = new List<Song>();

            for (int i = 0; i < response.Items.Count; i++)
            {
                Google.Apis.YouTube.v3.Data.Playlist playlist = response.Items[i];
                YtPlaylists.Add(playlist);
                Song song = new Song(playlist.Snippet.Title, playlist.Snippet.ChannelTitle, playlist.Snippet.Thumbnails.Default__.Url, null, -1, -1, playlist.Id, true);
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

        private async void OnRefresh(object sender, System.EventArgs e)
        {
            await Refresh();
            MainActivity.instance.pagerRefresh.Refreshing = false;
        }

        private async Task Refresh()
        {
            if (useMixes)
                return;

            if (MainActivity.instance.TokenHasExpire())
            {
                YoutubeEngine.youtubeService = null;
                MainActivity.instance.Login();

                while (YoutubeEngine.youtubeService == null)
                    await Task.Delay(500);
            }

            YouTubeService youtube = YoutubeEngine.youtubeService;
            PlaylistsResource.ListRequest ytPlaylists = youtube.Playlists.List("snippet,contentDetails");
            ytPlaylists.Mine = true;
            ytPlaylists.MaxResults = 25;
            PlaylistListResponse response = await ytPlaylists.ExecuteAsync();

            if (instance == null)
                return;

            playlists = new List<Song>();
            playlists.Clear();

            for (int i = 0; i < response.Items.Count; i++)
            {
                Google.Apis.YouTube.v3.Data.Playlist playlist = response.Items[i];
                YtPlaylists.Add(playlist);
                Song song = new Song(playlist.Snippet.Title, playlist.Snippet.ChannelTitle, playlist.Snippet.Thumbnails.Default__.Url, null, -1, -1, playlist.Id, true);
                playlists.Add(song);
            }

            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, playlists);
            ListAdapter = adapter;

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
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            if (useMixes)
            {
                builder.SetItems(mixActions, (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            RandomPlay(playlists[position].GetPath());
                            break;
                        case 3:
                            DownloadPlaylist(playlists[position].GetPath());
                            break;
                        default:
                            break;
                    }
                });
            }
            else
            {
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
            }
            builder.Show();
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
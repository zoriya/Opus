using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    public class Home : Fragment
    {
        public static Home instance;
        public RecyclerView ListView;
        public RecyclerAdapter adapter;
        public ItemTouchHelper itemTouchHelper;
        public List<Song> songs = new List<Song>();
        public View view;

        private string[] actions = new string[] { "Play", "Play Next", "Play Last", "Add To Playlist", "Edit Metadata" };

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            MainActivity.instance.OnPaddingChanged += PaddingChanged;
        }

        private void PaddingChanged(object sender, PaddingChange e)
        {
            view.SetPadding(0, 0, 0, MainActivity.paddingBot);
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            MainActivity.instance.OnPaddingChanged -= PaddingChanged;
            ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            view = inflater.Inflate(Resource.Layout.RecyclerFragment, container, false);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            view.SetPadding(0, 0, 0, MainActivity.paddingBot);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));

            PopulateSongs();
            return view;
        }

        private async void PopulateSongs()
        {
            if (MainActivity.instance.TokenHasExpire())
            {
                YoutubeEngine.youtubeService = null;
                MainActivity.instance.Login();

                while (YoutubeEngine.youtubeService == null)
                    await Task.Delay(500);
            }

            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Activity);
            //if(pref.)


            //YouTubeService youtube = YoutubeEngine.youtubeService;
            //ActivitiesResource.ListRequest request = youtube.Activities.List("snippet, contentDetails");
            //request.MaxResults = 25;
            //request.Home = true;
            //request.OauthToken = YoutubeEngine.youtubeService.ApiKey;

            //ChannelListResponse foo;

            //ActivityListResponse response = await request.ExecuteAsync();

            //foreach (var video in response.Items)
            //{
            //    Song videoInfo = new Song(video.Snippet.Title, video.Snippet.ChannelTitle, video.Snippet.Thumbnails.High.Url, video.Id, -1, -1, video.Id, true);
            //    songs.Add(videoInfo);
            //}

            adapter = new RecyclerAdapter(songs);

            ListView.SetAdapter(adapter);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongCLick;
            ListView.SetItemAnimator(new DefaultItemAnimator());
            ListView.ScrollChange += MainActivity.instance.Scroll;

            ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter);
            itemTouchHelper = new ItemTouchHelper(callback);
            itemTouchHelper.AttachToRecyclerView(ListView);
        }

        public static Fragment NewInstance()
        {
            instance = new Home { Arguments = new Bundle() };
            return instance;
        }

        private void OnRefresh(object sender, System.EventArgs e)
        {
            Refresh();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public void Refresh()
        {
            //Refresh
        }

        public void LoadMore()
        {
            //List<Song> songList = MusicPlayer.queue.Except(adapter.songList).ToList(); //Load more
        }

        private void ListView_ItemClick(object sender, int Position)
        {
            Song item = songs[Position];

            if (item.IsYt)
                YoutubeEngine.Play(item.youtubeID, item.GetName(), item.GetArtist(), item.GetAlbum());
            else
                Browse.Play(item);
        }

        private void ListView_ItemLongCLick(object sender, int e)
        {
            Song item = songs[e];
            More(item);
        }

        public void More(Song item)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    default:
                        break;
                }
            });
            builder.Show();
        }

        public override void OnResume()
        {
            base.OnResume();
            if (MainActivity.parcelable != null)
            {
                ListView.GetLayoutManager().OnRestoreInstanceState(MainActivity.parcelable);
                MainActivity.parcelable = null;
                MainActivity.parcelableSender = null;
            }
        }
    }
}
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    public class Home : Fragment
    {
        public static Home instance;
        public RecyclerView ListView;
        public HomeAdapter adapter;
        public ItemTouchHelper itemTouchHelper;
        public List<HomeItem> Items = new List<HomeItem>();
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
            if (YoutubeEngine.youtubeService == null)
                MainActivity.instance.Login();

            if (MainActivity.instance.TokenHasExpire())
            {
                YoutubeEngine.youtubeService = null;
                MainActivity.instance.Login();

                while (YoutubeEngine.youtubeService == null)
                    await Task.Delay(500);
            }

            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Activity);
            string[] selectedTopicsID = prefManager.GetStringSet("selectedTopicsID", new string[] { }).ToArray();


            foreach(string topic in selectedTopicsID)
            {
                YouTubeService youtube = YoutubeEngine.youtubeService;

                ChannelSectionsResource.ListRequest request = youtube.ChannelSections.List("snippet, contentDetails");
                request.ChannelId = topic;

                ChannelSectionListResponse response = await request.ExecuteAsync();

                foreach (var section in response.Items)
                {
                    if (section.Snippet.Type == "channelsectionTypeUndefined")
                        continue;

                    List<string> contentValue = null;
                    switch (section.Snippet.Title)
                    {
                        case "multipleChannels":
                            contentValue = section.ContentDetails.Channels.ToList();
                            break;
                        case "multiplePlaylists":
                        case "singlePlaylist":
                            contentValue = section.ContentDetails.Playlists.ToList();
                            break;
                        default:
                            contentValue = new List<string>();
                            break;
                    }

                    HomeItem item = new HomeItem(section.Snippet.Title, section.Snippet.Type, contentValue);
                    Items.Add(item);
                }
            }

            List<string> sections = new List<string>();
            List<HomeItem> homeSections = new List<HomeItem>();
            foreach(HomeItem item in Items)
            {
                if (!sections.Contains(item.SectionTitle))
                {
                    sections.Add(item.SectionTitle);
                    homeSections.Add(item);
                    System.Console.WriteLine("&" + item.SectionTitle);
                }
                else
                {
                    for(int i = 0; i < homeSections.Count; i++)
                    {
                        if (homeSections[i].SectionTitle != item.SectionTitle)
                            continue;

                        homeSections[i].AddContent(item);
                        break;
                    }
                }
            }

            //Get youtube data from all section but with random value inside, refresh playlist every item found
            //Some Content Type may be unsuported
            foreach(HomeItem item in homeSections)
            {
                switch (item.contentType)
                {
                    case "multipleChannels":
                        break;
                    case "multiplePlaylists":
                    case "singlePlaylist":
                        break;
                    default:
                        break;
                }
                //HomeSection section = new HomeSection(item.SectionTitle, SectionType.)
            }

            adapter = new HomeAdapter(homeSections);

            ListView.SetAdapter(adapter);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongCLick;
            ListView.SetItemAnimator(new DefaultItemAnimator());
            ListView.ScrollChange += MainActivity.instance.Scroll;
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
            HomeItem item = Items[Position];
        }

        private void ListView_ItemLongCLick(object sender, int e)
        {
            HomeItem item = Items[e];
            More(item);
        }

        public void More(HomeItem item)
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
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MusicApp.Resources.values;
using System;
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
        public static List<HomeSection> adapterItems = new List<HomeSection>();
        public View view;

        private readonly string[] actions = new string[] { "Play", "Play Next", "Play Last", "Add To Playlist", "Edit Metadata" };

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            view = inflater.Inflate(Resource.Layout.RecyclerFragment, container, false);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            view.SetPadding(0, 0, 0, MainActivity.defaultPaddingBot);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));

            if(adapterItems.Count > 0)
            {
                adapter = new HomeAdapter(adapterItems);
                ListView.SetAdapter(adapter);
                adapter.ItemClick += ListView_ItemClick;
                ListView.SetItemAnimator(new DefaultItemAnimator());
                ListView.ScrollChange += MainActivity.instance.Scroll;
            }
            else
                PopulateSongs();
            return view;
        }

        private async void PopulateSongs()
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Activity);
            string[] selectedTopicsID = prefManager.GetStringSet("selectedTopicsID", new string[] { }).ToArray();

            if (selectedTopicsID.Length < 1)
                return;

            if (YoutubeEngine.youtubeService == null)
                MainActivity.instance.Login();

            await MainActivity.instance.WaitForYoutube();

            List<HomeItem> Items = new List<HomeItem>();

            foreach (string topic in selectedTopicsID)
            {
                YouTubeService youtube = YoutubeEngine.youtubeService;

                ChannelSectionsResource.ListRequest request = youtube.ChannelSections.List("snippet, contentDetails");
                request.ChannelId = topic;

                ChannelSectionListResponse response = await request.ExecuteAsync();

                foreach (var section in response.Items)
                {
                    if (section.Snippet.Type == "channelsectionTypeUndefined")
                        continue;

                    SectionType type = SectionType.None;
                    List<string> contentValue = null;
                    switch (section.Snippet.Type)
                    {
                        case "multipleChannels":
                            type = SectionType.ChannelList;
                            contentValue = section.ContentDetails.Channels.ToList();
                            break;
                        case "multiplePlaylists":
                            contentValue = section.ContentDetails.Playlists.ToList();
                            type = SectionType.PlaylistList;
                            break;
                        case "singlePlaylist":
                            contentValue = section.ContentDetails.Playlists.ToList();
                            type = SectionType.SinglePlaylist;
                            break;
                        default:
                            contentValue = new List<string>();
                            break;
                    }

                    HomeItem item = new HomeItem(section.Snippet.Title, type, contentValue);
                    Items.Add(item);
                }
            }

            List<HomeItem> itemsSave = Items;
            foreach(HomeItem item in Items)
            {
                List<Song> contentValue = new List<Song>
                {
                    new Song("HeaderSlot", null, null, null, -1, -1, null)
                };

                switch (item.contentType)
                {
                    case SectionType.ChannelList:
                        if(adapterItems.Where(x => x.SectionTitle == item.SectionTitle).Count() == 0)
                        {
                            foreach (string channelID in item.contentValue)
                            {
                                YouTubeService youtube = YoutubeEngine.youtubeService;

                                ChannelsResource.ListRequest request = youtube.Channels.List("snippet");
                                request.Id = channelID;

                                ChannelListResponse response = await request.ExecuteAsync();

                                foreach (var ytItem in response.Items)
                                {
                                    Song channel = new Song(ytItem.Snippet.Title, "", ytItem.Snippet.Thumbnails.Default__.Url, ytItem.Id, -1, -1, null, true);
                                    contentValue.Add(channel);

                                    if (instance == null)
                                        return;
                                }

                            }

                            HomeSection section = new HomeSection(item.SectionTitle, item.contentType, contentValue);

                            if (adapter == null)
                            {
                                adapterItems.Add(section);
                                adapter = new HomeAdapter(adapterItems);
                                ListView.SetAdapter(adapter);
                                adapter.ItemClick += ListView_ItemClick;
                                adapter.ItemLongClick += ListView_ItemLongCLick;
                                ListView.SetItemAnimator(new DefaultItemAnimator());
                                ListView.ScrollChange += MainActivity.instance.Scroll;
                            }
                            else
                            {
                                adapterItems.Add(section);
                                adapter.AddToList(new List<HomeSection>() { section });
                            }
                        }
                        break;
                    case SectionType.SinglePlaylist:
                        if (adapterItems.Where(x => x.SectionTitle == item.SectionTitle).Count() == 0)
                        {
                            YouTubeService youtube = YoutubeEngine.youtubeService;

                            PlaylistItemsResource.ListRequest request = youtube.PlaylistItems.List("snippet, contentDetails");
                            request.PlaylistId = item.contentValue[0];
                            request.MaxResults = 10;
                            request.PageToken = "";

                            PlaylistItemListResponse response = await request.ExecuteAsync();

                            foreach (var ytItem in response.Items)
                            {
                                Song song = new Song(ytItem.Snippet.Title, ytItem.Snippet.ChannelTitle, ytItem.Snippet.Thumbnails.High.Url, ytItem.ContentDetails.VideoId, -1, -1, ytItem.ContentDetails.VideoId, true);
                                contentValue.Add(song);

                                if (instance == null)
                                    return;
                            }

                            HomeSection section = new HomeSection(item.SectionTitle, item.contentType, contentValue)
                            {
                                data = item.contentValue[0]
                            };

                            if (adapter == null)
                            {
                                adapterItems.Add(section);
                                adapter = new HomeAdapter(adapterItems);
                                ListView.SetAdapter(adapter);
                                adapter.ItemClick += ListView_ItemClick;
                                adapter.ItemLongClick += ListView_ItemLongCLick;
                                ListView.SetItemAnimator(new DefaultItemAnimator());
                                ListView.ScrollChange += MainActivity.instance.Scroll;
                            }
                            else
                            {
                                adapterItems.Add(section);
                                adapter.AddToList(new List<HomeSection>() { section });
                            }
                        }
                        break;
                    case SectionType.PlaylistList:
                        if (adapterItems.Where(x => x.SectionTitle == item.SectionTitle).Count() == 0)
                        {
                            foreach (string playlistID in item.contentValue)
                            {
                                YouTubeService youtube = YoutubeEngine.youtubeService;

                                PlaylistsResource.ListRequest request = youtube.Playlists.List("snippet, contentDetails");
                                request.Id = playlistID;

                                PlaylistListResponse response = await request.ExecuteAsync();


                                foreach (var playlist in response.Items)
                                {
                                    Console.WriteLine("&" + playlist.Snippet.Title);
                                    Song song = new Song(playlist.Snippet.Title, playlist.Snippet.ChannelTitle, playlist.Snippet.Thumbnails.Default__.Url, playlist.Id, -1, -1, playlist.Id, true);
                                    contentValue.Add(song);

                                    if (instance == null)
                                        return;
                                }
                            }

                            HomeSection section = new HomeSection(item.SectionTitle, item.contentType, contentValue);
                            if (adapter == null)
                            {
                                adapterItems.Add(section);
                                adapter = new HomeAdapter(adapterItems);
                                ListView.SetAdapter(adapter);
                                adapter.ItemClick += ListView_ItemClick;
                                adapter.ItemLongClick += ListView_ItemLongCLick;
                                ListView.SetItemAnimator(new DefaultItemAnimator());
                                ListView.ScrollChange += MainActivity.instance.Scroll;
                            }
                            else
                            {
                                adapterItems.Add(section);
                                adapter.AddToList(new List<HomeSection>() { section });
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            if (instance == null)
                return;
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

        private void ListView_ItemClick(object sender, int position)
        {
            int pos = adapter.GetItemPosition(position, out int ContainerID);
            HomeSection section = adapterItems[ContainerID];
            Song item = section.contentValue[pos];

            if(pos == 0)
            {
                if (section.contentType == SectionType.SinglePlaylist)
                {
                    MainActivity.parcelableSender = "Home";
                    MainActivity.parcelable = ListView.GetLayoutManager().OnSaveInstanceState();

                    AppCompatActivity act = (AppCompatActivity)Activity;
                    act.SupportActionBar.SetHomeButtonEnabled(true);
                    act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                    act.SupportActionBar.Title = section.SectionTitle;

                    MainActivity.instance.HideTabs();
                    MainActivity.instance.HomeDetails = true;
                    MainActivity.instance.Transition(Resource.Id.contentView, PlaylistTracks.NewInstance(section.data, item.GetName(), false), true);
                }
            }
            else
            {
                if (section.contentType == SectionType.SinglePlaylist)
                {
                    YoutubeEngine.Play(item.youtubeID, item.GetName(), item.GetArtist(), item.GetAlbum());
                }
                else if (section.contentType == SectionType.PlaylistList)
                {
                    MainActivity.parcelableSender = "Home";
                    MainActivity.parcelable = ListView.GetLayoutManager().OnSaveInstanceState();

                    AppCompatActivity act = (AppCompatActivity)Activity;
                    act.SupportActionBar.SetHomeButtonEnabled(true);
                    act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                    act.SupportActionBar.Title = section.SectionTitle;

                    MainActivity.instance.HideTabs();
                    MainActivity.instance.HomeDetails = true;
                    MainActivity.instance.Transition(Resource.Id.contentView, PlaylistTracks.NewInstance(item.youtubeID, item.GetName(), false), true);
                }
            }
        }

        private void ListView_ItemLongCLick(object sender, int e)
        {
            HomeSection item = adapterItems[e];
            //More(item);
        }

        public void More(HomeItem item)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
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
            if (MainActivity.parcelableSender == "Home" && !MainActivity.instance.ResumeKiller)
            {
                ListView.GetLayoutManager().OnRestoreInstanceState(MainActivity.parcelable);
                MainActivity.parcelable = null;
                MainActivity.parcelableSender = null;
            }
        }
    }
}
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
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
        public static IParcelable savedState;
        public RecyclerView ListView;
        public HomeAdapter adapter;
        public ItemTouchHelper itemTouchHelper;
        public static List<HomeSection> adapterItems = new List<HomeSection>();
        public View view;

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

            if (savedState == null)
                PopulateSongs();
            else
            {
                //ListView.GetLayoutManager().OnRestoreInstanceState(savedState);
                savedState = null;
            }
            return view;
        }

        private async void PopulateSongs()
        {
            adapterItems = new List<HomeSection>();

            HomeSection queue = new HomeSection("Queue", SectionType.SinglePlaylist, MusicPlayer.queue);
            if(queue.contentValue.Count > 0)
                adapterItems.Add(queue);

            Android.Net.Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

            List<Song> allSongs = new List<Song>();
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

                    allSongs.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }
            Random r = new Random();
            List<Song> songList = allSongs.OrderBy(x => r.Next()).ToList();

            HomeSection featured = new HomeSection("Featured", SectionType.SinglePlaylist, songList.GetRange(0, 50));
            adapterItems.Add(featured);

            adapter = new HomeAdapter(adapterItems);
            ListView.SetAdapter(adapter);
            adapter.ItemClick += ListView_ItemClick;
            ListView.SetItemAnimator(new DefaultItemAnimator());
            ListView.ScrollChange += MainActivity.instance.Scroll;

            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Activity);
            string[] selectedTopics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToArray();
            string[] selectedTopicsID = prefManager.GetStringSet("selectedTopicsID", new string[] { }).ToArray();

            if (selectedTopicsID.Length > 0)
            {
                await MainActivity.instance.WaitForYoutube();

                List<HomeItem> Items = new List<HomeItem>();
                foreach (string topic in selectedTopicsID)
                {
                    YouTubeService youtube = YoutubeEngine.youtubeService;
                    int maxSection = 3;
                    switch (selectedTopics.Length)
                    {
                        case 1:
                            maxSection = 5000;
                            break;
                        case 2:
                            maxSection = 5;
                            break;
                        default:
                            maxSection = 3;
                            break;
                    }

                    ChannelSectionsResource.ListRequest request = youtube.ChannelSections.List("snippet, contentDetails");
                    request.ChannelId = topic;

                    ChannelSectionListResponse response = await request.ExecuteAsync();
                    List<ChannelSection> topicItems = response.Items.ToList();
                    topicItems.RemoveAt(0);
                    r = new Random();
                    topicItems = topicItems.OrderBy(x => r.Next()).ToList();
                    topicItems.Insert(0, response.Items[0]);

                    foreach (var section in topicItems)
                    {
                        if (section.Snippet.Type == "channelsectionTypeUndefined")
                            continue;

                        string title = section.Snippet.Title;
                        if (title == null || title == "")
                            title = selectedTopics[Array.IndexOf(selectedTopicsID, topic)];

                        if (Items.Exists(x => x.SectionTitle == title))
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

                        HomeItem item = new HomeItem(title, type, contentValue);
                        Items.Add(item);
                    }
                }

                foreach (HomeItem item in Items)
                {
                    List<Song> contentValue = new List<Song>();
                    switch (item.contentType)
                    {
                        case SectionType.SinglePlaylist:
                            YouTubeService youtube = YoutubeEngine.youtubeService;

                            PlaylistItemsResource.ListRequest request = youtube.PlaylistItems.List("snippet, contentDetails");
                            request.PlaylistId = item.contentValue[0];
                            request.MaxResults = 25;
                            request.PageToken = "";

                            PlaylistItemListResponse response = await request.ExecuteAsync();

                            if (response.Items.Count < 10)
                                break;

                            foreach (var ytItem in response.Items)
                            {
                                Song song = new Song(ytItem.Snippet.Title, ytItem.Snippet.ChannelTitle, ytItem.Snippet.Thumbnails.High.Url, ytItem.ContentDetails.VideoId, -1, -1, ytItem.ContentDetails.VideoId, true);
                                contentValue.Add(song);

                                if (instance == null)
                                    return;
                            }

                            HomeSection section = new HomeSection(item.SectionTitle, item.contentType, contentValue);
                            adapter.AddToList(new List<HomeSection>() { section });
                            break;
                        //case SectionType.ChannelList:
                        //    if (adapterItems.Where(x => x.SectionTitle == item.SectionTitle).Count() == 0)
                        //    {
                        //        foreach (string channelID in item.contentValue)
                        //        {
                        //            YouTubeService youtube = YoutubeEngine.youtubeService;

                        //            ChannelsResource.ListRequest request = youtube.Channels.List("snippet");
                        //            request.Id = channelID;

                        //            ChannelListResponse response = await request.ExecuteAsync();

                        //            foreach (var ytItem in response.Items)
                        //            {
                        //                Song channel = new Song(ytItem.Snippet.Title, "", ytItem.Snippet.Thumbnails.Default__.Url, ytItem.Id, -1, -1, null, true);
                        //                contentValue.Add(channel);

                        //                if (instance == null)
                        //                    return;
                        //            }

                        //        }

                        //        HomeSection section = new HomeSection(item.SectionTitle, item.contentType, contentValue);

                        //        if (adapter == null)
                        //        {
                        //            adapterItems.Add(section);
                        //            adapter = new HomeAdapter(adapterItems);
                        //            ListView.SetAdapter(adapter);
                        //            adapter.ItemClick += ListView_ItemClick;
                        //            adapter.ItemLongClick += ListView_ItemLongCLick;
                        //            ListView.SetItemAnimator(new DefaultItemAnimator());
                        //            ListView.ScrollChange += MainActivity.instance.Scroll;
                        //        }
                        //        else
                        //        {
                        //            adapterItems.Add(section);
                        //            adapter.AddToList(new List<HomeSection>() { section });
                        //        }
                        //    }
                        //    break;
                        //case SectionType.PlaylistList:
                        //    if (adapterItems.Where(x => x.SectionTitle == item.SectionTitle).Count() == 0)
                        //    {
                        //        foreach (string playlistID in item.contentValue)
                        //        {
                        //            YouTubeService youtube = YoutubeEngine.youtubeService;

                        //            PlaylistsResource.ListRequest request = youtube.Playlists.List("snippet, contentDetails");
                        //            request.Id = playlistID;

                        //            PlaylistListResponse response = await request.ExecuteAsync();


                        //            foreach (var playlist in response.Items)
                        //            {
                        //                Console.WriteLine("&" + playlist.Snippet.Title);
                        //                Song song = new Song(playlist.Snippet.Title, playlist.Snippet.ChannelTitle, playlist.Snippet.Thumbnails.Default__.Url, playlist.Id, -1, -1, playlist.Id, true);
                        //                contentValue.Add(song);

                        //                if (instance == null)
                        //                    return;
                        //            }
                        //        }

                        //        HomeSection section = new HomeSection(item.SectionTitle, item.contentType, contentValue);
                        //        if (adapter == null)
                        //        {
                        //            adapterItems.Add(section);
                        //            adapter = new HomeAdapter(adapterItems);
                        //            ListView.SetAdapter(adapter);
                        //            adapter.ItemClick += ListView_ItemClick;
                        //            adapter.ItemLongClick += ListView_ItemLongCLick;
                        //            ListView.SetItemAnimator(new DefaultItemAnimator());
                        //            ListView.ScrollChange += MainActivity.instance.Scroll;
                        //        }
                        //        else
                        //        {
                        //            adapterItems.Add(section);
                        //            adapter.AddToList(new List<HomeSection>() { section });
                        //        }
                        //    }
                        //    break;
                        default:
                            break;
                    }
                }
            }
        }

        public static Fragment NewInstance()
        {
            instance = new Home { Arguments = new Bundle() };
            return instance;
        }

        public void OnRefresh(object sender, EventArgs e)
        {
            Refresh();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public void Refresh()
        {
            PopulateSongs();
        }

        public void LoadMore()
        {
            //List<Song> songList = MusicPlayer.queue.Except(adapter.songList).ToList(); //Load more
        }

        private void ListView_ItemClick(object sender, int position)
        {
            //int pos = adapter.GetItemPosition(position, out int ContainerID);
            //HomeSection section = adapterItems[ContainerID];
            //Song item = section.contentValue[pos];

            //if(pos == 0)
            //{
            //    if (section.contentType == SectionType.SinglePlaylist)
            //    {
            //        MainActivity.parcelableSender = "Home";
            //        MainActivity.parcelable = ListView.GetLayoutManager().OnSaveInstanceState();

            //        AppCompatActivity act = (AppCompatActivity)Activity;
            //        act.SupportActionBar.SetHomeButtonEnabled(true);
            //        act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            //        act.SupportActionBar.Title = section.SectionTitle;

            //        MainActivity.instance.HideTabs();
            //        MainActivity.instance.HomeDetails = true;
            //        MainActivity.instance.Transition(Resource.Id.contentView, PlaylistTracks.NewInstance(section.data, item.GetName(), false, item.GetArtist(), -1, item.GetAlbum()), true);
            //    }
            //}
            //else
            //{
            //    if (section.contentType == SectionType.SinglePlaylist)
            //    {
            //        YoutubeEngine.Play(item.youtubeID, item.GetName(), item.GetArtist(), item.GetAlbum());
            //    }
            //    else if (section.contentType == SectionType.PlaylistList)
            //    {
            //        MainActivity.parcelableSender = "Home";
            //        MainActivity.parcelable = ListView.GetLayoutManager().OnSaveInstanceState();

            //        AppCompatActivity act = (AppCompatActivity)Activity;
            //        act.SupportActionBar.SetHomeButtonEnabled(true);
            //        act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            //        act.SupportActionBar.Title = section.SectionTitle;

            //        MainActivity.instance.HideTabs();
            //        MainActivity.instance.HomeDetails = true;
            //        MainActivity.instance.Transition(Resource.Id.contentView, PlaylistTracks.NewInstance(item.youtubeID, item.GetName(), false, item.GetArtist(), -1, item.GetAlbum()), true);
            //    }
            //}
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
            if (MainActivity.parcelableSender == "Home" && !MainActivity.instance.ResumeKiller)
            {
                ListView.GetLayoutManager().OnRestoreInstanceState(MainActivity.parcelable);
                MainActivity.parcelable = null;
                MainActivity.parcelableSender = null;
            }
        }
    }
}
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.Preferences;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.Linq;

namespace MusicApp.Resources.Portable_Class
{
    public class TopicSelector : ListFragment
    {
        public static TopicSelector instance;
        public string path;
        public List<string> selectedTopics = new List<string>();
        public List<string> selectedTopicsID = new List<string>();
        private List<Song> channels = new List<Song>();
        private ChannelAdapter adapter;
        private View emptyView;


        public async override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            ListView.SetPadding(0, Preferences.instance.toolbar.Height, 0, 0);

            if (!await MainActivity.instance.WaitForYoutube())
            {
                System.Console.WriteLine("&Youtube awaited");
                ListAdapter = null;
                emptyView = LayoutInflater.Inflate(Resource.Layout.EmptyYoutubeSearch, null);
                ((TextView)emptyView).Text = "Error while loading.\nCheck your internet connection and check if your logged in.";
                ((TextView)emptyView).SetTextColor(Color.Red);
                Activity.AddContentView(emptyView, ListView.LayoutParameters);
                return;
            }

            if (instance == null)
                return;

            List<Song> channelList = new List<Song>();

            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            List<string> topics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToList();
            foreach(string topic in topics)
            {
                selectedTopics.Add(topic.Substring(0, topic.IndexOf("/#-#/")));
                selectedTopicsID.Add(topic.Substring(topic.IndexOf("/#-#/") + 5));
            }

            string nextPageToken = "";
            while (nextPageToken != null)
            {
                YouTubeService youtube = YoutubeEngine.youtubeService;
                SubscriptionsResource.ListRequest request = youtube.Subscriptions.List("snippet,contentDetails");
                request.ChannelId = "UCRPb0XKQwDoHbgvtawH-gGw";
                request.MaxResults = 50;
                request.PageToken = nextPageToken;

                SubscriptionListResponse response = await request.ExecuteAsync();

                foreach (var item in response.Items)
                {
                    Song channel = new Song(item.Snippet.Title.Substring(0, item.Snippet.Title.IndexOf(" - Topic")), item.Snippet.Description, item.Snippet.Thumbnails.Default__.Url, item.Snippet.ResourceId.ChannelId, -1, -1, null, true);
                    channelList.Add(channel);
                }

                nextPageToken = response.NextPageToken;
            }

            List<string> topicList = channelList.ConvertAll(x => x.youtubeID);
            foreach (string channelID in selectedTopicsID.Except(topicList))
            {
                YouTubeService youtube = YoutubeEngine.youtubeService;

                ChannelsResource.ListRequest req = youtube.Channels.List("snippet");
                req.Id = channelID;

                ChannelListResponse resp = await req.ExecuteAsync();

                foreach (var ytItem in resp.Items)
                {
                    Song channel = new Song(ytItem.Snippet.Title.Contains(" - Topic") ? ytItem.Snippet.Title.Substring(0, ytItem.Snippet.Title.IndexOf(" - Topic")) : ytItem.Snippet.Title, "", ytItem.Snippet.Thumbnails.Default__.Url, channelID, -1, -1, null, true);
                    channelList.Add(channel);

                    if (instance == null)
                        return;
                }
            }
            channels = channelList.OrderBy(x => x.Title).ToList();

            adapter = new ChannelAdapter(Application.Context, Resource.Layout.ChannelList, channels);
            ListAdapter = adapter;
            ListView.TextFilterEnabled = true;
            ListView.ItemClick += ListView_ItemClick;
        }

        public static Fragment NewInstance()
        {
            instance = new TopicSelector { Arguments = new Bundle() };
            return instance;
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Song channel = channels[e.Position];
            bool Checked = selectedTopics.Contains(channel.Title);
            e.View.FindViewById<CheckBox>(Resource.Id.checkBox).Checked = !Checked;

            if (!Checked)
            {
                selectedTopics.Add(channel.Title);
                selectedTopicsID.Add(channel.youtubeID);
            }
            else
            {
                int index = selectedTopics.IndexOf(channel.Title);
                selectedTopics.RemoveAt(index);
                selectedTopicsID.RemoveAt(index);
            }
        }

        public override void OnStop()
        {
            if (emptyView != null)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
            base.OnStop();
            Preferences.instance.toolbar.Title = "Settings";
        }
    }
}
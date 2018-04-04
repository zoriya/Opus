using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.Preferences;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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


        public async override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            if (YoutubeEngine.youtubeService == null)
                MainActivity.instance.Login();

            if (MainActivity.instance.TokenHasExpire())
            {
                YoutubeEngine.youtubeService = null;
                MainActivity.instance.Login();

                while (YoutubeEngine.youtubeService == null)
                    await Task.Delay(500);
            }

            List<Song> channelLits = new List<Song>();

            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            selectedTopics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToList();
            selectedTopicsID = prefManager.GetStringSet("selectedTopicsID", new string[] { }).ToList();

            string nextPageToken = "";
            while(nextPageToken != null)
            {
                YouTubeService youtube = YoutubeEngine.youtubeService;
                SubscriptionsResource.ListRequest request = youtube.Subscriptions.List("snippet,contentDetails");
                request.ChannelId = "UCh3mHcmSMffgVxFniKQrpug";
                request.MaxResults = 50;
                request.PageToken = nextPageToken;

                SubscriptionListResponse response = await request.ExecuteAsync();

                foreach (var item in response.Items)
                {
                    Song channel = new Song(item.Snippet.Title.Substring(0, item.Snippet.Title.IndexOf(" - Topic")), item.Snippet.Description, item.Snippet.Thumbnails.Default__.Url, item.Snippet.ResourceId.ChannelId, -1, -1, null, true);
                    channelLits.Add(channel);
                }

                nextPageToken = response.NextPageToken;
            }

            channels = channelLits.OrderBy(x => x.GetName()).ToList();

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

        public override void OnDestroy()
        {
            base.OnDestroy();
            instance = null;
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Song channel = channels[e.Position];
            bool Checked = selectedTopics.Contains(channel.GetName());
            e.View.FindViewById<CheckBox>(Resource.Id.checkBox).Checked = !Checked;

            if (!Checked)
            {
                selectedTopics.Add(channel.GetName());
                selectedTopicsID.Add(channel.youtubeID);
            }
            else
            {
                int index = selectedTopics.IndexOf(channel.GetName());
                selectedTopics.RemoveAt(index);
                selectedTopicsID.RemoveAt(index);
            }
        }
    }
}
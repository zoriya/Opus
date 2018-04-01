using Android.App;
using Android.OS;
using Android.Views;
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
        private List<string> selectedTopicsID = new List<string>();
        private List<Song> channels = new List<Song>();
        private ChannelAdapter adapter;


        public async override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            if (MainActivity.instance.TokenHasExpire())
            {
                YoutubeEngine.youtubeService = null;
                MainActivity.instance.Login();

                while (YoutubeEngine.youtubeService == null)
                    await Task.Delay(500);
            }


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
                    Song channel = new Song(item.Snippet.Title, item.Snippet.Description, item.Snippet.Thumbnails.Default__.Url, item.Snippet.ResourceId.ChannelId, -1, -1, null, true);
                    channels.Add(channel);
                }

                nextPageToken = response.NextPageToken;
            }

            List<Song> channelList = channels.OrderBy(x => x.GetName()).ToList();

            adapter = new ChannelAdapter(Application.Context, Resource.Layout.ChannelList, channelList);
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
            System.Console.WriteLine("&Item clicked");
            Song channel = channels[e.Position];
            bool Checked = selectedTopics.Contains(channel.GetName());
            System.Console.WriteLine("&" + Checked);
            e.View.FindViewById<CheckBox>(Resource.Id.checkBox).Checked = Checked;

            if (Checked)
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
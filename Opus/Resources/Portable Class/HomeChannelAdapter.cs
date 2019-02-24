using Android.App;
using Android.Content;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Opus.Resources.values;
using Square.Picasso;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Opus.Resources.Portable_Class
{
    public class HomeChannelAdapter : RecyclerView.Adapter
    {
        public RecyclerView recycler;
        public int listPadding = 0;
        public List<Song> songList;
        public List<Song> allItems;
        private bool useTopic = false;

        public override int ItemCount => useTopic ? 3 : songList.Count;

        public HomeChannelAdapter(List<Song> songList, RecyclerView recycler)
        {
            this.recycler = recycler;
            this.songList = songList;
        }

        public HomeChannelAdapter(List<Song> songList)
        {
            this.songList = songList;
            useTopic = true;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            RecyclerHolder holder = (RecyclerHolder)viewHolder;

            holder.Title.Text = songList[position].Title;
            var songAlbumArtUri = Android.Net.Uri.Parse(songList[position].Album);
            Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.background_material_dark).Resize(400, 400).CenterCrop().Transform(new CircleTransformation()).Into(holder.AlbumArt);

            if (!useTopic)
            {
                if (songList[position].Artist == "Follow")
                {
                    if(Home.instance.selectedTopicsID.Contains(songList[position].YoutubeID))
                        holder.action.Text = "Unfollow";
                    else
                        holder.action.Text = "Follow";
                }
                else if (songList[position].Artist != null)
                    holder.action.Text = "Mix";

                if (!holder.action.HasOnClickListeners)
                {
                    holder.action.Click += async (sender, e) =>
                    {
                        if(holder.action.Text == "Following")
                        {
                            holder.action.Text = "Unfollowed";
                            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(MainActivity.instance);
                            List<string> topics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToList();

                            ISharedPreferencesEditor editor = prefManager.Edit();
                            topics.Remove(songList[position].Title + "/#-#/" + songList[position].YoutubeID);
                            editor.PutStringSet("selectedTopics", topics);
                            editor.Apply();
                            Home.instance.selectedTopics.Remove(songList[position].Title);
                            Home.instance.selectedTopicsID.Remove(songList[position].YoutubeID);

                            await Task.Delay(1000);
                            holder.action.Text = "Follow";
                        }
                        else if (songList[position].Artist == "Follow")
                        {
                            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(MainActivity.instance);
                            List<string> topics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToList();

                            ISharedPreferencesEditor editor = prefManager.Edit();
                            topics.Add(songList[position].Title + "/#-#/" + songList[position].YoutubeID);
                            editor.PutStringSet("selectedTopics", topics);
                            editor.Apply();
                            Home.instance.selectedTopics.Add(songList[position].Title);
                            Home.instance.selectedTopicsID.Add(songList[position].YoutubeID);

                            holder.action.Text = "Following";
                            await Task.Delay(1000);

                            if(holder.action.Text != "Unfollowed")
                            {
                                if (allItems.Count > 0 && songList.Count < 5)
                                {
                                    songList[position] = allItems[allItems.Count - 1];
                                    NotifyItemChanged(position);
                                    allItems.RemoveAt(allItems.Count - 1);
                                }
                                else
                                {
                                    songList.RemoveAt(position);
                                    allItems.RemoveAt(position);
                                    NotifyItemRemoved(position);
                                }
                            }
                        }
                        else if (songList[position].Artist != null)
                            Playlist.PlayInOrder(songList[position].Artist);
                    };
                }
            }
            else
            {
                holder.action.Text = "Follow";

                if (!holder.action.HasOnClickListeners)
                {
                    holder.action.Click += async (sender, e) =>
                    {
                        if (holder.action.Text == "Following")
                        {
                            holder.action.Text = "Unfollowed";
                            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(MainActivity.instance);
                            List<string> topics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToList();

                            ISharedPreferencesEditor editor = prefManager.Edit();
                            topics.Remove(songList[position].Title + "/#-#/" + songList[position].YoutubeID);
                            editor.PutStringSet("selectedTopics", topics);
                            editor.Apply();

                            await Task.Delay(1000);
                            holder.action.Text = "Follow";
                        }
                        else
                        {
                            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(MainActivity.instance);
                            List<string> topics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToList();

                            ISharedPreferencesEditor editor = prefManager.Edit();
                            topics.Add(songList[position].Title + "/#-#/" + songList[position].YoutubeID);
                            editor.PutStringSet("selectedTopics", topics);
                            editor.Apply();

                            holder.action.Text = "Following";
                            await Task.Delay(1000);

                            if (holder.action.Text != "Unfollowed")
                            {
                                if (position == 0 || position == 1)
                                {
                                    if (songList.Count < 4)
                                        return;

                                    songList[position] = songList[songList.Count - 1];
                                    songList.RemoveAt(songList.Count - 1);
                                }
                                else
                                    songList.RemoveAt(position);

                                NotifyItemChanged(position);
                            }
                        }
                    };
                }
                holder.ItemView.SetPadding(4, 1, 4, 1);
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.HomeChannel, parent, false);
            return new RecyclerHolder(itemView, OnClick, OnLongClick);
        }

        void OnClick(int position) { }

        void OnLongClick(int position) { }
    }
}
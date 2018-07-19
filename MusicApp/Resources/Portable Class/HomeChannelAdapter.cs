using Android.App;
using Android.Content;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    public class HomeChannelAdapter : RecyclerView.Adapter
    {
        public RecyclerView recycler;
        public int listPadding = 0;
        public List<Song> songList;
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

            holder.Title.Text = songList[position].GetName();
            var songAlbumArtUri = Android.Net.Uri.Parse(songList[position].GetAlbum());
            Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Transform(new CircleTransformation()).Into(holder.AlbumArt);

            if (!useTopic)
            {
                if (songList[position].GetArtist() != null)
                    holder.action.Text = "Mix";

                if (!holder.action.HasOnClickListeners)
                {
                    holder.action.Click += (sender, e) =>
                    {
                        if (songList[position].GetArtist() != null)
                            Playlist.PlayInOrder(songList[position].GetArtist());
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
                        ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(MainActivity.instance);
                        List<string> topics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToList();

                        ISharedPreferencesEditor editor = prefManager.Edit();
                        topics.Add(songList[position].GetName() + "/#-#/" + songList[position].youtubeID);
                        editor.PutStringSet("selectedTopics", topics);
                        editor.Apply();

                        holder.action.Text = "Following";
                        await Task.Delay(1000);

                        if(position == 0 || position == 1)
                        {
                            if (songList.Count < 4)
                                return;

                            songList[position] = songList[songList.Count - 1];
                            songList.RemoveAt(songList.Count - 1);
                        }
                        else
                            songList.RemoveAt(position);

                        NotifyItemChanged(position);
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
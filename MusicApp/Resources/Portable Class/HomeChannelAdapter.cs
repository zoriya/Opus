using Android.App;
using Android.Support.V7.Widget;
using Android.Views;
using MusicApp.Resources.values;
using Square.Picasso;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class HomeChannelAdapter : RecyclerView.Adapter
    {
        public RecyclerView recycler;
        public int listPadding = 0;
        public List<Song> songList;

        public override int ItemCount => songList.Count;

        public HomeChannelAdapter(List<Song> songList, RecyclerView recycler)
        {
            this.recycler = recycler;
            this.songList = songList;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            RecyclerHolder holder = (RecyclerHolder)viewHolder;

            holder.Title.Text = songList[position].GetName();
            var songAlbumArtUri = Android.Net.Uri.Parse(songList[position].GetAlbum());
            Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Transform(new CircleTransformation()).Into(holder.AlbumArt);

            if (songList[position].GetArtist() != null)
                holder.action.Text = "Mix";

            if(!holder.action.HasOnClickListeners)
            {
                holder.action.Click += (sender, e) =>
                {
                    if (songList[position].GetArtist() != null)
                        Playlist.PlayInOrder(songList[position].GetArtist());
                };
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
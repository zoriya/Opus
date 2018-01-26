using Android.App;
using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using MusicApp.Resources.values;
using Square.Picasso;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class RecyclerAdapter : RecyclerView.Adapter
    {
        private Song[] songList;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongCLick;

        public RecyclerAdapter(Song[] songList)
        {
            Console.WriteLine("&Recycler adapter created");
            this.songList = songList;
        }

        public override int ItemCount => songList.Length;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            RecyclerHolder holder = (RecyclerHolder)viewHolder;

            holder.Title.Text += songList[position].GetName();
            holder.Artist.Text = songList[position].GetArtist();

            if (songList[position].GetAlbumArt() == -1 || songList[position].IsYt)
            {
                var songAlbumArtUri = Android.Net.Uri.Parse(songList[position].GetAlbum());
                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
            }
            else
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, songList[position].GetAlbumArt());

                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
            }

            if (!holder.more.HasOnClickListeners)
            {
                holder.more.Click += (sender, e) =>
                {
                    Queue.instance?.More(songList[position]);
                    Browse.instance?.More(songList[position]);
                    YoutubeEngine.instance?.More(songList[position]);
                    YtPlaylist.instance?.More(position);
                    PlaylistTracks.instance?.More(songList[position], position);
                    FolderTracks.instance?.More(songList[position]);
                };
            }

            float scale = MainActivity.instance.Resources.DisplayMetrics.Density;
            int padding = 135;
            padding = (int)(padding * scale + 0.5f);

            holder.reorder.Visibility = ViewStates.Visible;
            holder.textLayout.SetPadding(padding, 0, 0, 0);

            if (songList[position].queueSlot == MusicPlayer.CurrentID())
            {
                holder.Title.SetTextSize(Android.Util.ComplexUnitType.Dip, 18);
            }

            if (!songList[position].isParsed && songList[position].IsYt)
            {
                holder.youtubeIcon.SetImageResource(Resource.Drawable.needProcessing);
                holder.youtubeIcon.Visibility = ViewStates.Visible;
            }
            else if (songList[position].IsYt)
            {
                holder.youtubeIcon.SetImageResource(Resource.Drawable.youtubeIcon);
                holder.youtubeIcon.Visibility = ViewStates.Visible;
            }
            else
            {
                holder.youtubeIcon.Visibility = ViewStates.Gone;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
            return new RecyclerHolder(itemView, OnClick, OnLongClick);
        }

        void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        void OnLongClick(int position)
        {
            ItemLongCLick?.Invoke(this, position);
        }
    }
}
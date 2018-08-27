using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class PlaylistTrackAdapter : RecyclerView.Adapter, IItemTouchAdapter
    {
        public List<Song> songList;
        private bool refreshDisabled = true;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongClick;
        public int listPadding;

        public PlaylistTrackAdapter(List<Song> songList)
        {
            this.songList = songList;
        }

        public void AddToList(List<Song> songList)
        {
            int positionStart = this.songList.Count;
            this.songList.AddRange(songList);
            NotifyItemRangeInserted(positionStart, songList.Count);
        }

        public void Remove(Song song)
        {
            int position = songList.IndexOf(song);
            songList.Remove(song);
            NotifyItemRemoved(position);
        }

        public override int ItemCount => songList.Count + (PlaylistTracks.instance.fullyLoadded ? 0 : 1);

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if(position >= songList.Count)
                return;

            RecyclerHolder holder = (RecyclerHolder)viewHolder;

            holder.Title.Text = songList[position].Title;
            holder.Artist.Text = songList[position].Artist;

            if (songList[position].AlbumArt == -1 || songList[position].IsYt)
            {
                var songAlbumArtUri = Android.Net.Uri.Parse(songList[position].Album);
                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
            }
            else
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, songList[position].AlbumArt);

                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
            }

            holder.more.Tag = position;
            if (!holder.more.HasOnClickListeners)
            {
                holder.more.Click += (sender, e) =>
                {
                    int tagPosition = (int)((ImageView)sender).Tag;
                    PlaylistTracks.instance.More(songList[tagPosition], tagPosition);
                };
            }

            float scale = MainActivity.instance.Resources.DisplayMetrics.Density;

            if (MainActivity.Theme == 1)
            {
                holder.more.SetColorFilter(Color.White);
                holder.Title.SetTextColor(Color.White);
                holder.Artist.SetTextColor(Color.White);
                holder.Artist.Alpha = 0.7f;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if(viewType == 0)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
                return new RecyclerHolder(itemView, OnClick, OnLongClick);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.smallLoading, parent, false);
                return new UslessHolder(itemView);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position >= songList.Count)
                return 1;
            return 0;
        }

        void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        void OnLongClick(int position)
        {
            ItemLongClick?.Invoke(this, position);
        }

        public void ItemMoved(int fromPosition, int toPosition)
        {
            //Enable this only if the user is editing the playlist

            //if (fromPosition < toPosition)
            //{
            //    for (int i = fromPosition; i < toPosition; i++)
            //        songList = Swap(songList, i, i + 1);
            //}
            //else
            //{
            //    for (int i = fromPosition; i > toPosition; i--)
            //        songList = Swap(songList, i, i - 1);
            //}

            //NotifyItemMoved(fromPosition, toPosition);
        }

        public void ItemMoveEnded(int fromPosition, int toPosition)
        {
            //if (MusicPlayer.CurrentID() == fromPosition)
            //    MusicPlayer.currentID = toPosition;

            //MusicPlayer.instance.UpdateQueueSlots();
        }

        //List<T> Swap<T>(List<T> list, int fromPosition, int toPosition)
        //{
        //    T item = list[fromPosition];
        //    list[fromPosition] = list[toPosition];
        //    list[toPosition] = item;
        //    return list;
        //}

        public void ItemDismissed(int position)
        {
            //Queue.RemoveFromQueue(songList[position]);
            //NotifyItemRemoved(position);
        }

        public bool RefreshDisabled()
        {
            return refreshDisabled;
        }

        public void DisableRefresh(bool disable)
        {
            refreshDisabled = disable;
        }
    }
}
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
    public class RecyclerAdapter : RecyclerView.Adapter, IItemTouchAdapter
    {
        public List<Song> songList;
        private bool refreshDisabled = true;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongCLick;
        public int listPadding;

        public RecyclerAdapter(List<Song> songList)
        {
            this.songList = songList;
        }

        public void UpdateList(List<Song> songList)
        {
            this.songList = songList;
            NotifyDataSetChanged();
        }

        public void AddToList(List<Song> songList)
        {
            int positionStart = this.songList.Count;
            this.songList.AddRange(songList);
            NotifyItemRangeInserted(positionStart, songList.Count);
        }

        public override int ItemCount => songList.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            RecyclerHolder holder = (RecyclerHolder)viewHolder;

            holder.Title.Text = songList[position].GetName();
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

            holder.more.Tag = position;
            if (!holder.more.HasOnClickListeners)
            {
                holder.more.Click += (sender, e) =>
                {
                    int tagPosition = (int)((ImageView)sender).Tag;

                    if(Queue.instance != null)
                        Queue.instance.More(songList[tagPosition]);
                };
            }

            //if (!holder.reorder.HasOnClickListeners)
            //{
            //    holder.reorder.Click += (sender, e) =>
            //    {
            //        Queue.instance.itemTouchHelper.StartDrag(viewHolder);
            //        MainActivity.instance.contentRefresh.SetEnabled(false);
            //        Queue.instance.adapter.DisableRefresh(true);
            //    };
            //}


            if (songList[position].queueSlot == MusicPlayer.CurrentID())
            {
                holder.Title.SetTextSize(Android.Util.ComplexUnitType.Dip, 18);
            }

            float scale = MainActivity.instance.Resources.DisplayMetrics.Density;
            if (Queue.instance != null)
            {
                holder.reorder.Visibility = ViewStates.Visible;

                int padding = 135;
                padding = (int)(padding * scale + 0.5f);
                holder.textLayout.SetPadding(padding, 0, 0, 0);

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
            else
            {
                holder.reorder.Visibility = ViewStates.Gone;
            }

            if (MainActivity.Theme == 1)
            {

                holder.more.SetColorFilter(Color.White);
                holder.reorder.SetColorFilter(Color.White);
                holder.Title.SetTextColor(Color.White);
                holder.Artist.SetTextColor(Color.White);
                holder.Artist.Alpha = 0.7f;
            }

            if (position == MusicPlayer.CurrentID())
                holder.ItemView.SetBackgroundColor(Color.DimGray);
            else if (MainActivity.Theme == 0)
                holder.ItemView.SetBackgroundColor(Color.White);
            else
                holder.ItemView.SetBackgroundColor(Color.ParseColor("#424242"));

            if (position + 1 == songList.Count)
            {
                holder.ItemView.SetPadding((int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), listPadding);
                LinearLayout.LayoutParams layoutParams = (LinearLayout.LayoutParams)holder.more.LayoutParameters;
                layoutParams.SetMargins(0, 0, 0, listPadding);
                holder.more.LayoutParameters = layoutParams;
            }
            else
            {
                holder.ItemView.SetPadding((int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f));
                LinearLayout.LayoutParams layoutParams = (LinearLayout.LayoutParams)holder.more.LayoutParameters;
                layoutParams.SetMargins(0, 0, 0, 0);
                holder.more.LayoutParameters = layoutParams;
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

        public void ItemMoved(int fromPosition, int toPosition)
        {
            if(fromPosition < toPosition)
            {
                for(int i = fromPosition; i < toPosition; i++)
                {
                    songList = Swap(songList, i, i + 1);
                }
            }
            else
            {
                for(int i = fromPosition; i > toPosition; i--)
                {
                    songList = Swap(songList, i, i - 1);
                }
            }

            NotifyItemMoved(fromPosition, toPosition);
        }

        public void ItemMoveEnded(int fromPosition, int toPosition)
        {
            MusicPlayer.Swap(fromPosition, toPosition);
        }

        List<T> Swap<T>(List<T> list, int fromPosition, int toPosition)
        {
            T item = list[fromPosition];
            list[fromPosition] = list[toPosition];
            list[toPosition] = item;
            return list;
        }

        public void ItemDismissed(int position)
        {
            Queue.RemoveFromQueue(songList[position]);
            NotifyItemRemoved(position);
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
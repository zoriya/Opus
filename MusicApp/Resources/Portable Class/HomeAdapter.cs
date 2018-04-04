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
    public class HomeAdapter : RecyclerView.Adapter
    {
        public List<HomeItem> items;
        private bool refreshDisabled = false;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongCLick;

        public HomeAdapter(List<HomeItem> items)
        {
            this.items = items;
        }

        public void UpdateList(List<HomeItem> items)
        {
            this.items = items;
            NotifyDataSetChanged();
        }

        public void AddToList(List<HomeItem> items)
        {
            int positionStart = this.items.Count;
            this.items.AddRange(items);
            NotifyItemRangeInserted(positionStart, items.Count);
        }

        public override int ItemCount => items.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            HomeHolder holder = (HomeHolder)viewHolder;

            //holder.Title.Text = items[position].GetName();
            //holder.Artist.Text = items[position].GetArtist();

            //var songAlbumArtUri = Android.Net.Uri.Parse(items[position].GetAlbum());
            //Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);

            //holder.more.Tag = position;
            //if (!holder.more.HasOnClickListeners)
            //{
            //    holder.more.Click += (sender, e) =>
            //    {
            //        int tagPosition = (int)((ImageView)sender).Tag;
            //        Queue.instance.More(items[tagPosition]);
            //    };
            //}

            float scale = MainActivity.instance.Resources.DisplayMetrics.Density;
            int padding = 135;
            padding = (int)(padding * scale + 0.5f);

            holder.textLayout.SetPadding(padding, 0, 0, 0);

            if (MainActivity.Theme == 1)
            {
                holder.more.SetColorFilter(Color.White);
                holder.reorder.SetColorFilter(Color.White);
                holder.Title.SetTextColor(Color.White);
                holder.Artist.SetTextColor(Color.White);
                holder.Artist.Alpha = 0.7f;
            }

            if (MainActivity.Theme == 0)
                holder.ItemView.SetBackgroundColor(Color.White);
            else
                holder.ItemView.SetBackgroundColor(Color.ParseColor("#424242"));
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
            return new RecyclerHolder(itemView, OnClick, null);
        }

        void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
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
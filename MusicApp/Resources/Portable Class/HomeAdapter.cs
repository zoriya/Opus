using Android.App;
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
        public List<HomeSection> items;
        private bool refreshDisabled = false;

        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongClick;


        public HomeAdapter(List<HomeSection> items)
        {
            this.items = items;
        }

        public void UpdateList(List<HomeSection> items)
        {
            this.items = items;
            NotifyDataSetChanged();
        }

        public void AddToList(List<HomeSection> items)
        {
            int positionStart = this.items.Count;
            this.items.AddRange(items);
            NotifyItemRangeInserted(positionStart, items.Count);
        }

        public override int ItemCount
        {
            get
            {
                int count = 0;
                for(int i = 0; i < items.Count; i++) { count += items[i].contentValue.Count; }
                return count;
            }
        }

        public int GetItemPosition(int position, out int containerID)
        {
            int pos = 0;
            containerID = 0;
            for(pos = position; pos - /*(items[containerID].contentType == SectionType.SinglePlaylist ? 2 :*/ items[containerID].contentValue.Count/*)*/ >= 0; pos -= /*(items[containerID - 1].contentType == SectionType.SinglePlaylist ? 2 : */items[containerID - 1].contentValue.Count/*)*/)
            {
                if (containerID + 1 < items.Count)
                {
                    containerID++;
                    //Console.WriteLine("&Switching to the next container, containerID = " + containerID + " pos = " + pos + " Container value count : " + items[containerID]?.contentValue.Count);
                }
            }

            //Console.WriteLine("&Container ID : " + containerID + " Item Position : " + pos);
            return pos;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if(viewType == (int)HomeRow.Header)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Android.Resource.Layout.PreferenceCategory, parent, false);
                return new HeaderHolder(itemView, OnClick, OnLongClick);
            }
            if(viewType == (int)HomeRow.ChannelHolder)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ChannelList, parent, false);
                return new HomeChannelHolder(itemView, OnClick, OnLongClick);
            }
            if (viewType == (int)HomeRow.SongHolder)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
                return new HomeHolder(itemView, OnClick, OnLongClick);
            }
            if(viewType == (int)HomeRow.MultipleSong)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SquareSong, parent, false);
                return new HomeMultipleSong(itemView, OnClick, OnLongClick);
            }
            return null;
        }

        public override int GetItemViewType(int position)
        {
            int pos = GetItemPosition(position, out int containerID);
            if (pos == 0)
                return (int)HomeRow.Header;
            else if (items[containerID].contentType == SectionType.PlaylistList)
                return (int)HomeRow.SongHolder;
            else if (items[containerID].contentType == SectionType.SinglePlaylist)
                return (int)HomeRow.MultipleSong;
            else
                return (int)HomeRow.ChannelHolder;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            position = GetItemPosition(position, out int containerID);

            if(position == 0)
            {
                HeaderHolder holder = (HeaderHolder)viewHolder;
                holder.headerText.Text = items[containerID].SectionTitle;
            }
            else if(items[containerID].contentType == SectionType.ChannelList)
            {
                HomeChannelHolder holder = (HomeChannelHolder)viewHolder;

                holder.Title.Text = items[containerID].contentValue[position].GetName();
                holder.Artist.Text = items[containerID].contentValue[position].GetArtist();
                holder.CheckBox.Visibility = ViewStates.Gone;

                Picasso.With(Application.Context).Load(items[containerID].contentValue[position].GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Transform(new CircleTransformation()).Into(holder.AlbumArt);

                if (MainActivity.Theme == 1)
                {
                    holder.Title.SetTextColor(Color.White);
                    holder.Artist.SetTextColor(Color.White);
                    holder.Artist.Alpha = 0.7f;
                }
            }
            else if(items[containerID].contentType == SectionType.SinglePlaylist)
            {
                HomeMultipleSong holder = (HomeMultipleSong)viewHolder;

                holder.Title.Text = items[containerID].contentValue[position].GetName();
                holder.Artist.Text = items[containerID].contentValue[position].GetArtist();

                int width = MainActivity.instance.Resources.DisplayMetrics.WidthPixels - (int)((20 - 0.5f) / MainActivity.instance.Resources.DisplayMetrics.Density);
                int height = width / 16 * 9;
                Picasso.With(Application.Context).Load(items[containerID].contentValue[position].GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(width, height).CenterCrop().Into(holder.AlbumArt);

                holder.more.Tag = position;
                if (!holder.more.HasOnClickListeners)
                {
                    holder.more.Click += (sender, e) =>
                    {
                        int tagPosition = (int)((ImageView)sender).Tag;
                        //Home.instance.More(items[tagPosition]);
                    };
                }

                if (MainActivity.Theme == 1)
                {
                    holder.Title.SetTextColor(Color.White);
                    holder.Artist.SetTextColor(Color.White);
                    holder.Artist.Alpha = 0.7f;
                    holder.more.SetColorFilter(Color.White);
                }

                if (MainActivity.Theme == 0)
                    holder.ItemView.SetBackgroundColor(Color.White);
                else
                    holder.ItemView.SetBackgroundColor(Color.ParseColor("#424242"));
            }
            else
            {
                HomeHolder holder = (HomeHolder)viewHolder;

                holder.Title.Text = items[containerID].contentValue[position].GetName();
                holder.Artist.Text = items[containerID].contentValue[position].GetArtist();

                Picasso.With(Application.Context).Load(items[containerID].contentValue[position].GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);

                //holder.more.Tag = position;
                //if (!holder.more.HasOnClickListeners)
                //{
                //    holder.more.Click += (sender, e) =>
                //    {
                //        int tagPosition = (int)((ImageView)sender).Tag;
                //        Home.instance.More(items[tagPosition]);
                //    };
                //}

                if (MainActivity.Theme == 1)
                {
                    holder.more.SetColorFilter(Color.White);
                    holder.Title.SetTextColor(Color.White);
                    holder.Artist.SetTextColor(Color.White);
                    holder.Artist.Alpha = 0.7f;
                }

                if (MainActivity.Theme == 0)
                    holder.ItemView.SetBackgroundColor(Color.White);
                else
                    holder.ItemView.SetBackgroundColor(Color.ParseColor("#424242"));
            }
        }

        void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        void OnLongClick(int position)
        {
            ItemLongClick?.Invoke(this, position);
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


    public enum HomeRow
    {
        Header = 0,
        SongHolder = 1,
        ChannelHolder = 2,
        MultipleSong = 3,
    }
}
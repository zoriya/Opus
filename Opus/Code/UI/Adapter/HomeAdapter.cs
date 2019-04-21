using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Opus.Api.Services;
using Opus.DataStructure;
using Opus.Fragments;
using System;
using System.Collections.Generic;

namespace Opus.Adapter
{
    public class HomeAdapter : RecyclerView.Adapter
    {
        public List<HomeSection> items;
        private bool refreshDisabled = true;

        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongClick;


        public HomeAdapter(List<HomeSection> items)
        {
            this.items = items;
        }

        public override int ItemCount { get { return items.Count; } }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if(viewType == 0)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.LineSongs, parent, false);
                return new LineSongHolder(itemView, OnClick, OnLongClick);
            }
            else if (viewType == 1)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.HomeChannels, parent, false);
                return new LineSongHolder(itemView, OnClick, OnLongClick);
            }
            else if (viewType == 2)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.HomePlaylists, parent, false);
                return new LineSongHolder(itemView, OnClick, OnLongClick);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ShuffleButton, parent, false);
                return new UslessHolder(itemView, OnClick);
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (items[position].contentType == SectionType.SinglePlaylist)
            {
                LineSongHolder holder = (LineSongHolder)viewHolder;
                items[position].recycler = holder.recycler;

                holder.recycler.SetLayoutManager(new LinearLayoutManager(MainActivity.instance, LinearLayoutManager.Horizontal, false));
                if (items[position].SectionTitle == "Queue")
                {
                    holder.title.Text = MainActivity.instance.GetString(Resource.String.queue);
                    LineAdapter adapter = new LineAdapter(holder.recycler);
                    Home.instance.QueueAdapter = adapter;
                    holder.recycler.SetAdapter(adapter);
                    holder.more.Click += (sender, e) =>
                    {
                        MainActivity.instance.ShowPlayer();
                        Player.instance.ShowQueue();
                    };
                    if(MusicPlayer.CurrentID() != -1 && MusicPlayer.CurrentID() <= MusicPlayer.queue.Count)
                        holder.recycler.ScrollToPosition(MusicPlayer.CurrentID());
                }
                else
                {
                    holder.title.Text = items[position].SectionTitle;
                    holder.recycler.SetAdapter(new LineAdapter(items[position].contentValue.GetRange(0, items[position].contentValue.Count > 20 ? 20 : items[position].contentValue.Count), holder.recycler));
                    holder.more.Click += (sender, e) =>
                    {
                        position = holder.AdapterPosition;
                        MainActivity.instance.contentRefresh.Refresh -= Home.instance.OnRefresh;
                        Home.instance = null;
                        //MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(items[position].contentValue, items[position].SectionTitle)).AddToBackStack(null).Commit();
                    };
                }

                if (MainActivity.Theme == 1)
                    holder.ItemView.SetBackgroundColor(Color.Argb(255, 62, 62, 62));
            }
            else if(items[position].contentType == SectionType.ChannelList)
            {
                LineSongHolder holder = (LineSongHolder)viewHolder;
                items[position].recycler = holder.recycler;

                holder.title.Text = items[position].SectionTitle;
                holder.recycler.SetLayoutManager(new LinearLayoutManager(MainActivity.instance, LinearLayoutManager.Vertical, false));
                holder.recycler.SetAdapter(new HomeListAdapter(items[position].contentValue.GetRange(0, items[position].contentValue.Count > 4 ? 4 : items[position].contentValue.Count), holder.recycler) { allItems = items[position].contentValue.GetRange(4, items[position].contentValue.Count - 4) });

                ((GradientDrawable)holder.more.Background).SetStroke(5, Android.Content.Res.ColorStateList.ValueOf(Color.Argb(255, 21, 183, 237)));
                holder.more.SetTextColor(Color.Argb(255, 21, 183, 237));
                holder.more.Text = ((HomeListAdapter)holder.recycler.GetAdapter()).channels.Count > 4 ? "View Less" : "View More";
                holder.more.Click += (sender, e) =>
                {
                    HomeListAdapter adapter = (HomeListAdapter)holder.recycler.GetAdapter();
                    if(adapter.ItemCount == 4)
                    {
                        adapter.channels.AddRange(items[position].contentValue.GetRange(4, items[position].contentValue.Count - 4));
                        adapter.NotifyItemRangeInserted(4, items[position].contentValue.Count - 4);
                        holder.more.Text = "View Less";
                    }
                    else
                    {
                        int count = adapter.channels.Count - 4;
                        adapter.channels.RemoveRange(4, count);
                        adapter.NotifyItemRangeRemoved(4, count);
                        holder.more.Text = "View More";
                    }
                };

                if (MainActivity.Theme == 1)
                    holder.ItemView.SetBackgroundColor(Color.Argb(255, 62, 62, 62));
            }
            else if (items[position].contentType == SectionType.PlaylistList)
            {
                LineSongHolder holder = (LineSongHolder)viewHolder;
                holder.title.Text = items[position].SectionTitle;
                holder.recycler.SetLayoutManager(new LinearLayoutManager(MainActivity.instance, LinearLayoutManager.Vertical, false));
                holder.recycler.SetAdapter(new HomeListAdapter(items[position].playlistContent.GetRange(0, items[position].playlistContent.Count > 4 ? 4 : items[position].playlistContent.Count), holder.recycler));
                holder.more.Click += (sender, e) => { MainActivity.instance.FindViewById<BottomNavigationView>(Resource.Id.bottomView).SelectedItemId = Resource.Id.playlistLayout; };


                if (MainActivity.Theme == 1)
                    holder.ItemView.SetBackgroundColor(Color.Argb(255, 62, 62, 62));
            }
            //else if(items[position].contentType == SectionType.TopicSelector)
            //{
            //    LineSongHolder holder = (LineSongHolder)viewHolder;
            //    items[position].recycler = holder.recycler;

            //    holder.title.Text = items[position].SectionTitle;
            //    holder.recycler.SetLayoutManager(new LinearLayoutManager(MainActivity.instance, LinearLayoutManager.Vertical, false));
            //    holder.recycler.SetAdapter(new HomeListAdapter(items[position].contentValue));
            //    holder.more.Click += (sender, e) =>
            //    {
            //        Intent intent = new Intent(MainActivity.instance, typeof(Preferences));
            //        MainActivity.instance.StartActivity(intent);
            //    };

            //    if (MainActivity.Theme == 1)
            //        holder.ItemView.SetBackgroundColor(Color.Argb(255, 62, 62, 62));
            //}
            else if (items[position].contentType == SectionType.Shuffle)
            {
                if (MainActivity.Theme == 1)
                    ((CardView)viewHolder.ItemView).SetCardBackgroundColor(Color.ParseColor("#212121"));
                else
                    ((CardView)viewHolder.ItemView).SetCardBackgroundColor(Color.White);
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

        public override int GetItemViewType(int position)
        {
            if (items[position].contentType == SectionType.SinglePlaylist)
                return 0;
            else if (items[position].contentType == SectionType.ChannelList)
                return 1;
            else if (items[position].contentType == SectionType.PlaylistList)
                return 2;
            else
                return 4;
        }
    }
}
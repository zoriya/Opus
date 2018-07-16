using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using MusicApp.Resources.values;
using System;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
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

        public override int ItemCount { get { return items.Count; } }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.LineSongs, parent, false);
            return new LineSongHolder(itemView, OnClick, OnLongClick);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            LineSongHolder holder = (LineSongHolder)viewHolder;
            holder.title.Text = items[position].SectionTitle;
            holder.recycler.SetLayoutManager(new LinearLayoutManager(MainActivity.instance, LinearLayoutManager.Horizontal, false));
            holder.recycler.SetAdapter(new LineAdapter(items[position].contentValue.GetRange(0, 20), holder.recycler));
            holder.more.Click += (sender, e) => 
            {
                Home.savedState = Home.instance.ListView.GetLayoutManager().OnSaveInstanceState();
                Home.instance.RetainInstance = true;
                MainActivity.instance.SupportActionBar.SetHomeButtonEnabled(true);
                MainActivity.instance.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                MainActivity.instance.SupportActionBar.Title = items[position].SectionTitle;
                MainActivity.instance.contentRefresh.Refresh -= Home.instance.OnRefresh;
                Home.instance = null;
                MainActivity.instance.Transition(Resource.Id.contentView, PlaylistTracks.NewInstance(items[position].contentValue, items[position].SectionTitle), true);
            };

            if(MainActivity.Theme == 1)
                holder.ItemView.SetBackgroundColor(Color.Argb(255, 62, 62, 62));
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
    }
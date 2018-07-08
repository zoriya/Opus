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
            holder.recycler.SetAdapter(new LineAdapter(items[position].contentValue));
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
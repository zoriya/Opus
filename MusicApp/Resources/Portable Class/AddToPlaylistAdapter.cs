using Android.App;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class AddToPlaylistAdapter : RecyclerView.Adapter
    {
        private List<PlaylistItem> LocalPlaylists = new List<PlaylistItem>();
        private List<PlaylistItem> YoutubePlaylists = new List<PlaylistItem>();
        public event EventHandler<int> ItemClick;

        public AddToPlaylistAdapter(List<PlaylistItem> LocalPlaylists, List<PlaylistItem> YoutubePlaylists)
        {
            this.LocalPlaylists = LocalPlaylists;
            this.YoutubePlaylists = YoutubePlaylists;
        }

        public override int ItemCount => LocalPlaylists.Count + YoutubePlaylists.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (position >= LocalPlaylists.Count && YoutubePlaylists[position - LocalPlaylists.Count].Name == "Loading" && YoutubePlaylists[position - LocalPlaylists.Count].YoutubeID == null)
                return;

            AddToPlaylistHolder holder = (AddToPlaylistHolder)viewHolder;
            holder.Title.Text = LocalPlaylists[position].Name;

            if((LocalPlaylists.Count > position && LocalPlaylists[position].SongContained) || (position > LocalPlaylists.Count && YoutubePlaylists[position - LocalPlaylists.Count].SongContained))
                holder.Added.Checked = true;
            else
                holder.Added.Checked = false;


            if ((LocalPlaylists.Count > position && LocalPlaylists[position].SyncState == SyncState.True) || (position > LocalPlaylists.Count && YoutubePlaylists[position - LocalPlaylists.Count].SyncState == SyncState.True))
            {
                holder.Status.Visibility = ViewStates.Visible;
                holder.Status.SetImageResource(Resource.Drawable.Sync);                    
            }
            else if(position >= LocalPlaylists.Count)
            {
                holder.Status.Visibility = ViewStates.Visible;
                holder.Status.SetImageResource(Resource.Drawable.PublicIcon);
            }
            else
            {
                holder.Status.Visibility = ViewStates.Gone;
            }

            if (MainActivity.Theme == 1)
            {
                holder.Status.SetColorFilter(Color.White);
                holder.Title.SetTextColor(Color.White);
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == 0)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.AddToPlaylistItem, parent, false);
                return new AddToPlaylistHolder(itemView, OnClick);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.smallLoading, parent, false);
                return new UslessHolder(itemView);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position == LocalPlaylists.Count + YoutubePlaylists.Count - 1 && YoutubePlaylists[position - LocalPlaylists.Count].Name == "Loading")
                return 1;
            else
                return 0;
        }

        void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }
    }

    public class AddToPlaylistHolder : RecyclerView.ViewHolder
    {
        public TextView Title;
        public CheckBox Added;
        public ImageView Status;

        public AddToPlaylistHolder(View itemView, Action<int> listener) : base(itemView)
        {
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Added = itemView.FindViewById<CheckBox>(Resource.Id.added);
            Status = itemView.FindViewById<ImageView>(Resource.Id.status);

            itemView.Click += (sender, e) => listener(AdapterPosition);
        }
    }
}
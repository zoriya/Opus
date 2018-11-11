using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class PlaylistAdapter : RecyclerView.Adapter
    {
        private List<PlaylistItem> LocalPlaylists = new List<PlaylistItem>();
        private List<PlaylistItem> YoutubePlaylists = new List<PlaylistItem>();
        private bool? forkSaved = false;
        public int listPadding;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongCLick;

        public PlaylistAdapter(List<PlaylistItem> LocalPlaylists, List<PlaylistItem> YoutubePlaylists)
        {
            this.LocalPlaylists = LocalPlaylists;
            this.YoutubePlaylists = YoutubePlaylists;
        }

        public void UpdateElement(int position, PlaylistItem newPlaylist)
        {
            if(position < LocalPlaylists.Count)
                LocalPlaylists[position] = newPlaylist;
            else
                YoutubePlaylists[position] = newPlaylist;

            NotifyItemChanged(position);
        }

        public void Remove(int position)
        {
            if (position < LocalPlaylists.Count)
            {
                LocalPlaylists.RemoveAt(position);

                if (LocalPlaylists.Count == 1)
                {
                    LocalPlaylists.Add(new PlaylistItem("EMPTY - You don't have any playlist on your device.", null));
                }
            }
            else
            {
                YoutubePlaylists.RemoveAt(position - LocalPlaylists.Count);

                if (YoutubePlaylists.Count == 1)
                {
                    YoutubePlaylists.Add(new PlaylistItem("EMPTY", "You don't have any youtube playlist on your account. \nWarning: Only playlist from your google account are displayed", null));
                }
            }
            NotifyDataSetChanged();
        }

        public void SetYtPlaylists(List<PlaylistItem> YoutubePlaylists, bool forkSaved)
        {
            this.forkSaved = forkSaved;

            if (this.YoutubePlaylists.Count > 0)
                NotifyItemRangeRemoved(LocalPlaylists.Count + 1, this.YoutubePlaylists.Count);

            this.YoutubePlaylists = YoutubePlaylists;
            if (YoutubePlaylists.Count > 0)
                NotifyItemRangeInserted(LocalPlaylists.Count + 1, YoutubePlaylists.Count);
        }

        public override int ItemCount => LocalPlaylists.Count + YoutubePlaylists.Count + (forkSaved == true ? 1 : 0);

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (position == 0)
            {
                HeaderHolder holder = (HeaderHolder)viewHolder;
                holder.headerText.Text = "Local Playlists";
            }
            else if (position - LocalPlaylists.Count == 0)
            {
                HeaderHolder holder = (HeaderHolder)viewHolder;
                holder.headerText.Text = "Youtube Playlists";
            }
            else if (position == 1 && LocalPlaylists[1].Name.StartsWith("EMPTY - "))
            {
                EmptyHolder holder = (EmptyHolder)viewHolder;
                holder.text.Text = LocalPlaylists[1].Name.Substring(8);
            }
            else if (position - LocalPlaylists.Count == 1 && YoutubePlaylists[1].Name == "EMPTY")
            {
                EmptyHolder holder = (EmptyHolder)viewHolder;
                holder.text.Text = YoutubePlaylists[1].Owner;
            }
            else if (position == LocalPlaylists.Count + YoutubePlaylists.Count)
            {
                ButtonHolder holder = (ButtonHolder)viewHolder;
                if (MainActivity.Theme == 1)
                {
                    ((GradientDrawable)holder.ItemView.Background).SetStroke(5, Android.Content.Res.ColorStateList.ValueOf(Color.Argb(255, 62, 80, 180)));
                    holder.Button.SetTextColor(Color.Argb(255, 62, 80, 180));
                }
                else
                {
                    ((GradientDrawable)holder.ItemView.Background).SetStroke(5, Android.Content.Res.ColorStateList.ValueOf(Color.Argb(255, 21, 183, 237)));
                    holder.Button.SetTextColor(Color.Argb(255, 21, 183, 237));
                }

                float scale = MainActivity.instance.Resources.DisplayMetrics.Density;
                if (position + 1 == YoutubePlaylists.Count + LocalPlaylists.Count)
                    holder.ItemView.SetPadding((int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), listPadding);
                else
                    holder.ItemView.SetPadding((int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f));
            }
            else if(position >= LocalPlaylists.Count && YoutubePlaylists[position - LocalPlaylists.Count].Name == "Loading" && YoutubePlaylists[position - LocalPlaylists.Count].YoutubeID == null) { }
            else if(position >= LocalPlaylists.Count && YoutubePlaylists[position - LocalPlaylists.Count].Name == "Error" && YoutubePlaylists[position - LocalPlaylists.Count].YoutubeID == null)
            {
                EmptyHolder holder = (EmptyHolder)viewHolder;
                holder.text.Text = "Error while loading.\nCheck your internet connection and check if your logged in.";
                holder.text.SetTextColor(Color.Red);
            }
            else if (LocalPlaylists.Count >= position)
            {
                TwoLineHolder holder = (TwoLineHolder) viewHolder;
                holder.Line1.Text = LocalPlaylists[position].Name;
                holder.Line2.Text = LocalPlaylists[position].Count.ToString() + ((LocalPlaylists[position].Count > 1) ? " elements" : " element");

                holder.more.Tag = position;
                if (!holder.more.HasOnClickListeners)
                {
                    holder.more.Click += (sender, e) =>
                    {
                        int tagPosition = (int)((ImageView)sender).Tag;
                        Playlist.instance.More(tagPosition);
                    };
                }

                if (LocalPlaylists[position].SyncState == SyncState.Error)
                {
                    holder.sync.Visibility = ViewStates.Visible;
                    if (MainActivity.Theme == 1)
                        holder.sync.SetColorFilter(Color.White);
                }
                else
                {
                    holder.sync.Visibility = ViewStates.Gone;
                }

                if (MainActivity.Theme == 1)
                {
                    holder.more.SetColorFilter(Color.White);
                    holder.Line1.SetTextColor(Color.White);
                    holder.Line2.SetTextColor(Color.White);
                    holder.Line2.Alpha = 0.7f;
                }
            }
            else if (position > LocalPlaylists.Count && YoutubePlaylists.Count >= position - LocalPlaylists.Count)
            {
                PlaylistHolder holder = (PlaylistHolder)viewHolder;
                PlaylistItem playlist = YoutubePlaylists[position - LocalPlaylists.Count];

                holder.Title.Text = playlist.Name;
                holder.Owner.Text = playlist.Owner;
                Picasso.With(Application.Context).Load(playlist.ImageURL).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);

                if (playlist.HasWritePermission)
                {
                    holder.edit.Visibility = ViewStates.Visible;
                    if (MainActivity.Theme == 1)
                        holder.edit.SetColorFilter(Color.White);
                }
                else
                    holder.edit.Visibility = ViewStates.Gone;

                if (playlist.SyncState == SyncState.True)
                {
                    holder.sync.SetImageResource(Resource.Drawable.Sync);
                    holder.sync.Visibility = ViewStates.Visible;
                    if (MainActivity.Theme == 1)
                        holder.sync.SetColorFilter(Color.White);
                }
                else if (playlist.SyncState == SyncState.Error)
                {
                    holder.sync.SetImageResource(Resource.Drawable.SyncError);
                    holder.sync.Visibility = ViewStates.Visible;
                    if (MainActivity.Theme == 1)
                        holder.sync.SetColorFilter(Color.White);
                }
                else if (playlist.SyncState == SyncState.Loading)
                {
                    //holder.sync.SetImageResource(Resource.Drawable);
                    //holder.sync.Visibility = ViewStates.Visible;
                }
                else if (playlist.SyncState == SyncState.False)
                {
                    holder.sync.Visibility = ViewStates.Gone;
                }

                holder.more.Tag = position;
                if (!holder.more.HasOnClickListeners)
                {
                    holder.more.Click += (sender, e) =>
                    {
                        int tagPosition = (int)((ImageView)sender).Tag;
                        Playlist.instance.More(tagPosition);
                    };
                }

                if (MainActivity.Theme == 1)
                {
                    holder.more.SetColorFilter(Color.White);
                    holder.Title.SetTextColor(Color.White);
                    holder.Owner.SetTextColor(Color.White);
                    holder.Owner.Alpha = 0.7f;
                }
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == 0)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Android.Resource.Layout.PreferenceCategory, parent, false);
                return new HeaderHolder(itemView, null, null);
            }
            else if(viewType == 1)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TwoLineLayout, parent, false);
                return new TwoLineHolder(itemView, OnClick, OnLongClick);
            }
            else if(viewType == 2)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.smallLoading, parent, false);
                return new UslessHolder(itemView);
            }
            else if(viewType == 3)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.PlaylistItem, parent, false);
                return new PlaylistHolder(itemView, OnClick, OnLongClick);
            }
            else if (viewType == 4)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.BorderlessButton, parent, false);
                return new ButtonHolder(itemView, OnClick);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.EmptyListCategory, parent, false);
                return new EmptyHolder(itemView);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position == 0 || position - LocalPlaylists.Count == 0)
                return 0;
            else if (LocalPlaylists.Count >= position && (LocalPlaylists.Count > 2 || !LocalPlaylists[1].Name.StartsWith("EMPTY - ")))
                return 1;
            else if (position == LocalPlaylists.Count + YoutubePlaylists.Count)
                return 4;
            else if (position >= LocalPlaylists.Count && YoutubePlaylists[position - LocalPlaylists.Count].Name == "Loading" && YoutubePlaylists[position - LocalPlaylists.Count].YoutubeID == null)
                return 2;
            else if (position >= LocalPlaylists.Count && YoutubePlaylists[position - LocalPlaylists.Count].Name == "Error" && YoutubePlaylists[position - LocalPlaylists.Count].YoutubeID == null)
                return 5;
            else if (position > LocalPlaylists.Count && position < LocalPlaylists.Count + YoutubePlaylists.Count)
                return 3;
            else
                return 5;
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
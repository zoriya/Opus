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
        private List<string> playlistsName;
        private List<int> playlistCount;
        private List<Song> ytPlaylists;
        private bool? forkSaved = false;
        public int listPadding;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongCLick;

        public PlaylistAdapter(List<string> playlistsName, List<int> playlistCount, List<Song> ytPlaylists)
        {
            this.playlistsName = playlistsName;
            this.playlistCount = playlistCount;
            this.ytPlaylists = ytPlaylists;
        }

        public void UpdateElement(int position, Song newPlaylist)
        {
            if(position < playlistsName.Count)
            {
                playlistsName[position] = newPlaylist.Title;
                playlistCount[position] = int.Parse(newPlaylist.Album);
            }
            else
                ytPlaylists[position] = newPlaylist;

            NotifyItemChanged(position);
        }

        public void Remove(int position)
        {
            if (position < playlistsName.Count)
            {
                playlistsName.RemoveAt(position);
                playlistCount.RemoveAt(position);

                if (playlistsName.Count == 1)
                {
                    playlistsName.Add("EMPTY - You don't have any playlist on your device.");
                    playlistCount.Add(-1);
                }
            }
            else
            {
                ytPlaylists.RemoveAt(position - playlistsName.Count);

                if (ytPlaylists.Count == 1)
                {
                    ytPlaylists.Add(new Song("EMPTY", "You don't have any youtube playlist on your account. \nWarning: Only playlist from your google account are displayed", null, null, -1, -1, null));
                }
            }
            NotifyDataSetChanged();
        }

        public void SetYtPlaylists(List<Song> ytPlaylists, bool forkSaved)
        {
            this.forkSaved = forkSaved;

            if (this.ytPlaylists.Count > 0)
                NotifyItemRangeRemoved(playlistsName.Count + 1, this.ytPlaylists.Count);

            this.ytPlaylists = ytPlaylists;
            if (ytPlaylists.Count > 0)
                NotifyItemRangeInserted(playlistsName.Count + 1, ytPlaylists.Count);
        }

        public override int ItemCount => playlistsName.Count + ytPlaylists.Count + (forkSaved == true ? 1 : 0);

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (position == 0)
            {
                HeaderHolder holder = (HeaderHolder)viewHolder;
                holder.headerText.Text = "Local Playlists";
            }
            else if (position - playlistsName.Count == 0)
            {
                HeaderHolder holder = (HeaderHolder)viewHolder;
                holder.headerText.Text = "Youtube Playlists";
            }
            else if (position == 1 && playlistsName[1].StartsWith("EMPTY - "))
            {
                EmptyHolder holder = (EmptyHolder)viewHolder;
                holder.text.Text = playlistsName[1].Substring(8);
            }
            else if (position - playlistsName.Count == 1 && ytPlaylists[1].Title == "EMPTY")
            {
                EmptyHolder holder = (EmptyHolder)viewHolder;
                holder.text.Text = ytPlaylists[1].Artist;
            }
            else if (position == playlistsName.Count + ytPlaylists.Count)
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
                if (position + 1 == ytPlaylists.Count + playlistsName.Count)
                    holder.ItemView.SetPadding((int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), listPadding);
                else
                    holder.ItemView.SetPadding((int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f), (int)(8 * scale + 0.5f));
            }
            else if(position >= playlistsName.Count && ytPlaylists[position - playlistsName.Count].Title == "Loading" && ytPlaylists[position - playlistsName.Count].youtubeID == null) { }
            else if(position - playlistsName.Count == 1 && ytPlaylists[1].Title == "Error" && ytPlaylists[1].youtubeID == null)
            {
                EmptyHolder holder = (EmptyHolder)viewHolder;
                holder.text.Text = "Error while loading.\nCheck your internet connection and check if your logged in.";
                holder.text.SetTextColor(Color.Red);
            }
            else if (playlistsName.Count >= position)
            {
                TwoLineHolder holder = (TwoLineHolder) viewHolder;
                holder.Line1.Text = playlistsName[position];
                holder.Line2.Text = playlistCount[position].ToString() + ((playlistCount[position] > 1) ? " elements" : " element");

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
                    holder.Line1.SetTextColor(Color.White);
                    holder.Line2.SetTextColor(Color.White);
                    holder.Line2.Alpha = 0.7f;
                }

                float scale = MainActivity.instance.Resources.DisplayMetrics.Density;
                if (position + 1 == playlistsName.Count && ytPlaylists.Count == 2 && ytPlaylists[1]?.Title == "EMPTY")
                {
                    holder.ItemView.SetPadding(0, 0, 0, listPadding);
                    RelativeLayout.LayoutParams layoutParams = (RelativeLayout.LayoutParams)holder.more.LayoutParameters;
                    layoutParams.SetMargins(0, 0, 0, listPadding);
                    holder.more.LayoutParameters = layoutParams;
                }
                else
                {
                    holder.ItemView.SetPadding(0, 0, 0, 0);
                    RelativeLayout.LayoutParams layoutParams = (RelativeLayout.LayoutParams)holder.more.LayoutParameters;
                    layoutParams.SetMargins(0, 0, 0, 0);
                    holder.more.LayoutParameters = layoutParams;
                }
            }
            else if (position > playlistsName.Count && ytPlaylists.Count >= position - playlistsName.Count)
            {
                RecyclerHolder holder = (RecyclerHolder)viewHolder;
                Song song = ytPlaylists[position - playlistsName.Count];

                holder.Title.Text = song.Title;
                holder.Artist.Text = song.Artist;

                var songAlbumArtUri = Android.Net.Uri.Parse(song.Album);
                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);

                if (song.isParsed)
                {
                    holder.edit.Visibility = ViewStates.Visible;
                    if (MainActivity.Theme == 1)
                        holder.edit.SetColorFilter(Color.White);
                }
                else
                    holder.edit.Visibility = ViewStates.Gone;

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
                    holder.Artist.SetTextColor(Color.White);
                    holder.Artist.Alpha = 0.7f;
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
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
                return new RecyclerHolder(itemView, OnClick, OnLongClick);
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
            if (position == 0 || position - playlistsName.Count == 0)
                return 0;
            else if (playlistsName.Count >= position && (playlistsName.Count > 2 || !playlistsName[1].StartsWith("EMPTY - ")))
                return 1;
            else if (position == playlistsName.Count + ytPlaylists.Count)
                return 4;
            else if (position >= playlistsName.Count && ytPlaylists[position - playlistsName.Count].Title == "Loading" && ytPlaylists[position - playlistsName.Count].youtubeID == null)
                return 2;
            else if (position == playlistsName.Count + 1 && ytPlaylists[1].Title == "Error" && ytPlaylists[1].youtubeID == null)
                return 5;
            else if (position > playlistsName.Count && position < playlistsName.Count + ytPlaylists.Count && (ytPlaylists.Count > 2 || ytPlaylists[1].Title != "EMPTY"))
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
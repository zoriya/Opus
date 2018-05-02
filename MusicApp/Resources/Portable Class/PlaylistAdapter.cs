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
    public class PlaylistAdapter : RecyclerView.Adapter
    {
        private List<string> playlistsName;
        private List<int> playlistCount;
        private List<Song> ytPlaylists;
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
                playlistsName[position] = newPlaylist.GetName();
                playlistCount[position] = int.Parse(newPlaylist.GetAlbum());
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
                //NotifyItemRangeInserted(position, 1);

                if (playlistsName.Count == 1)
                {
                    playlistsName.Add("EMPTY - You don't have any playlist on your device.");
                    playlistCount.Add(-1);
                    //NotifyItemRangeInserted(2, 1);
                }
            }
            else
            {
                ytPlaylists.RemoveAt(position - playlistsName.Count);
                //NotifyItemRangeInserted(position - playlistsName.Count, 1);

                if (ytPlaylists.Count == 1)
                {
                    ytPlaylists.Add(new Song("EMPTY", "You don't have any youtube playlist on your account. \nWarning: Only playlist from your google account are displayed", null, null, -1, -1, null));
                    //NotifyItemRangeInserted(playlistsName.Count + 2, 1);
                }
            }
            NotifyDataSetChanged();
        }

        public void SetYtPlaylists(List<Song> ytPlaylists)
        {
            if(this.ytPlaylists.Count > 0)
                NotifyItemRangeRemoved(playlistsName.Count + 1, this.ytPlaylists.Count);

            this.ytPlaylists = ytPlaylists;
            if (ytPlaylists.Count > 0)
                NotifyItemRangeInserted(playlistsName.Count + 1, ytPlaylists.Count);
        }

        public override int ItemCount => playlistsName.Count + ytPlaylists.Count;

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
            else if(position == 1 && playlistsName[1].StartsWith("EMPTY - "))
            {
                EmptyCategoryHolder holder = (EmptyCategoryHolder)viewHolder;
                holder.text.Text = playlistsName[1].Substring(8);
            }
            else if(position - playlistsName.Count == 1 && ytPlaylists[1].GetName() == "EMPTY")
            {
                EmptyCategoryHolder holder = (EmptyCategoryHolder)viewHolder;
                holder.text.Text = ytPlaylists[1].GetArtist();
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
                if (position + 1 == playlistsName.Count)
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
            else
            {
                RecyclerHolder holder = (RecyclerHolder)viewHolder;
                Song song = ytPlaylists[position - playlistsName.Count];

                holder.Title.Text = song.GetName();
                holder.Artist.Text = song.GetArtist();

                var songAlbumArtUri = Android.Net.Uri.Parse(song.GetAlbum());
                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);

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

                float scale = MainActivity.instance.Resources.DisplayMetrics.Density;
                if (position + 1 == ytPlaylists.Count + playlistsName.Count)
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
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
                return new RecyclerHolder(itemView, OnClick, OnLongClick);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.EmptyListCategory, parent, false);
                return new EmptyCategoryHolder(itemView);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position == 0 || position - playlistsName.Count == 0)
                return 0;
            else if (playlistsName.Count >= position && (playlistsName.Count > 2 || !playlistsName[1].StartsWith("EMPTY - ")))
                return 1;
            else if(ytPlaylists.Count > 1 && (ytPlaylists.Count > 2 || ytPlaylists[1].GetName() != "EMPTY")) 
                return 2;
            else
                return 3;
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
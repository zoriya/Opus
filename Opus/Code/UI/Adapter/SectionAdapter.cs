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
    public class SectionAdapter : RecyclerView.Adapter
    {
        public List<Section> items;
        private bool refreshDisabled = true;

        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongClick;


        public SectionAdapter(List<Section> items)
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
                    if (MusicPlayer.CurrentID() != -1 && MusicPlayer.CurrentID() <= MusicPlayer.queue.Count)
                        holder.recycler.ScrollToPosition(MusicPlayer.CurrentID());
                }
                else if (items[position].SectionTitle == null)
                {
                    //The playlist is loading
                    holder.recycler.SetAdapter(new LineAdapter(new List<Song>(), holder.recycler));
                }
                else
                {
                    holder.title.Text = items[position].SectionTitle;
                    holder.recycler.SetAdapter(new LineAdapter(items[position].contentValue.GetRange(0, items[position].contentValue.Count > 20 ? 20 : items[position].contentValue.Count), holder.recycler));
                    holder.more.Click += (sender, e) =>
                    {
                        position = holder.AdapterPosition;
                        if (items[position].playlist == null)
                            MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(items[position].contentValue, items[position].SectionTitle)).AddToBackStack(null).Commit();
                        else
                            MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(items[position].playlist)).AddToBackStack(null).Commit();
                    };
                }
            }
            else if (items[position].contentType == SectionType.ChannelList)
            {
                LineSongHolder holder = (LineSongHolder)viewHolder;
                items[position].recycler = holder.recycler;

                holder.title.Text = items[position].SectionTitle;
                holder.recycler.SetLayoutManager(new LinearLayoutManager(MainActivity.instance, LinearLayoutManager.Vertical, false));

                if(items[position].channelContent != null)
                {
                    holder.recycler.SetAdapter(new SmallListAdapter(items[position].channelContent.GetRange(0, items[position].channelContent.Count > 4 ? 4 : items[position].channelContent.Count), holder.recycler));

                    if (items[position].channelContent.Count > 4)
                    {
                        holder.more.Visibility = ViewStates.Visible;
                        ((GradientDrawable)holder.more.Background).SetStroke(5, Android.Content.Res.ColorStateList.ValueOf(Color.Argb(255, 21, 183, 237)));
                        holder.more.SetTextColor(Color.Argb(255, 21, 183, 237));
                        holder.more.Text = ((SmallListAdapter)holder.recycler.GetAdapter()).channels.Count > 4 ? MainActivity.instance.GetString(Resource.String.view_less) : MainActivity.instance.GetString(Resource.String.view_more);
                        holder.more.Click += (sender, e) =>
                        {
                            SmallListAdapter adapter = (SmallListAdapter)holder.recycler.GetAdapter();
                            if (adapter.ItemCount == 4)
                            {
                                adapter.channels.AddRange(items[position].channelContent.GetRange(4, items[position].channelContent.Count - 4));
                                adapter.NotifyItemRangeInserted(4, items[position].channelContent.Count - 4);
                                holder.more.Text = MainActivity.instance.GetString(Resource.String.view_less);
                            }
                            else
                            {
                                int count = adapter.channels.Count - 4;
                                adapter.channels.RemoveRange(4, count);
                                adapter.NotifyItemRangeRemoved(4, count);
                                holder.more.Text = MainActivity.instance.GetString(Resource.String.view_more);
                            }
                        };
                    }
                    else
                        holder.more.Visibility = ViewStates.Gone;
                }
                else
                {
                    holder.recycler.SetAdapter(new SmallListAdapter(false, holder.recycler));
                }
                
            }
            else if (items[position].contentType == SectionType.PlaylistList)
            {
                LineSongHolder holder = (LineSongHolder)viewHolder;
                items[position].recycler = holder.recycler;
                holder.title.Text = items[position].SectionTitle;
                holder.recycler.SetLayoutManager(new LinearLayoutManager(MainActivity.instance, LinearLayoutManager.Vertical, false));
                if (items[position].playlistContent != null)
                {
                    holder.recycler.SetAdapter(new SmallListAdapter(items[position].playlistContent.GetRange(0, items[position].playlistContent.Count > 4 ? 4 : items[position].playlistContent.Count), holder.recycler));

                    if (ChannelDetails.instance != null)
                    {
                        if (items[position].playlistContent.Count > 4)
                        {
                            holder.more.Visibility = ViewStates.Visible;
                            ((GradientDrawable)holder.more.Background).SetStroke(5, Android.Content.Res.ColorStateList.ValueOf(Color.Argb(255, 21, 183, 237)));
                            holder.more.SetTextColor(Color.Argb(255, 21, 183, 237));
                            holder.more.Text = ((SmallListAdapter)holder.recycler.GetAdapter()).playlists.Count > 4 ? MainActivity.instance.GetString(Resource.String.view_less) : MainActivity.instance.GetString(Resource.String.view_more);
                            holder.more.Click += (sender, e) =>
                            {
                                SmallListAdapter adapter = (SmallListAdapter)holder.recycler.GetAdapter();
                                if (adapter.ItemCount == 4)
                                {
                                    adapter.playlists.AddRange(items[position].playlistContent.GetRange(4, items[position].playlistContent.Count - 4));
                                    adapter.NotifyItemRangeInserted(4, items[position].playlistContent.Count - 4);
                                    holder.more.Text = MainActivity.instance.GetString(Resource.String.view_less);
                                }
                                else
                                {
                                    int count = adapter.playlists.Count - 4;
                                    adapter.playlists.RemoveRange(4, count);
                                    adapter.NotifyItemRangeRemoved(4, count);
                                    holder.more.Text = MainActivity.instance.GetString(Resource.String.view_more);
                                }
                            };
                        }
                        else
                            holder.more.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        holder.more.Click += (sender, e) => { MainActivity.instance.FindViewById<BottomNavigationView>(Resource.Id.bottomView).SelectedItemId = Resource.Id.playlistLayout; };
                        holder.more.Visibility = ViewStates.Visible;
                    }
                }
                else
                {
                    holder.recycler.SetAdapter(new SmallListAdapter(true, holder.recycler));
                    holder.more.Visibility = ViewStates.Gone;
                }
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
            {
                if (ChannelDetails.instance != null)
                    return 1; //We want to display the playlists with the same view as the channels for this fragment.
                return 2;
            }
            else
                return 4;
        }
    }
}
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Opus.Api;
using Opus.DataStructure;
using Opus.Fragments;
using Opus.Others;
using Square.Picasso;
using System;
using System.Collections.Generic;

namespace Opus.Adapter
{
    public class YtAdapter : RecyclerView.Adapter
    {
        public int listPadding;
        private readonly List<YtFile> items;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongCLick;

        public YtAdapter(List<YtFile> items)
        {
            this.items = items;
        }

        public override int ItemCount => items.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if(items[position].Kind == YtKind.Video)
            {
                SongHolder holder = (SongHolder)viewHolder;
                Song song = items[position].song;

                holder.Title.Text = song.Title;
                holder.Artist.Text = song.Artist;
                holder.reorder.Visibility = ViewStates.Gone;

                var songAlbumArtUri = Android.Net.Uri.Parse(song.Album);
                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.placeholder).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);

                holder.more.Tag = position;
                if (!holder.more.HasOnClickListeners)
                {
                    holder.more.Click += (sender, e) =>
                    {
                        int tagPosition = (int)((ImageView)sender).Tag;
                        YoutubeSearch.instances[0].More(items[tagPosition].song);
                    };
                }

                if (song.IsLiveStream)
                    holder.Live.Visibility = ViewStates.Visible;
                else
                    holder.Live.Visibility = ViewStates.Gone;

                float scale = MainActivity.instance.Resources.DisplayMetrics.Density;
                if (position + 1 == items.Count)
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
            else if (items[position].Kind == YtKind.Playlist)
            {
                PlaylistHolder holder = (PlaylistHolder)viewHolder;
                PlaylistItem playlist = items[position].playlist;

                holder.Title.Text = playlist.Name;
                holder.Owner.Text = playlist.Owner;

                var songAlbumArtUri = Android.Net.Uri.Parse(playlist.ImageURL);
                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.placeholder).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);

                holder.more.Tag = position;
                if (!holder.more.HasOnClickListeners)
                {
                    holder.more.Click += (sender, e) =>
                    {
                        int tagPosition = (int)((ImageView)sender).Tag;
                        YoutubeSearch.instances[0].PlaylistMore(items[tagPosition].playlist);
                    };
                }
            }
            else if(items[position].Kind == YtKind.Channel)
            {
                RecyclerChannelHolder holder = (RecyclerChannelHolder)viewHolder;
                Channel channel = items[position].channel;

                holder.Title.Text = channel.Name;
                Picasso.With(Android.App.Application.Context).Load(channel.ImageURL).Transform(new CircleTransformation(false)).Into(holder.AlbumArt);

                if (!holder.action.HasOnClickListeners)
                {
                    holder.action.Click += (sender, e) =>
                    {
                        YoutubeManager.MixFromChannel(channel.YoutubeID);
                    };
                }
            }
            else if(items[position].Kind == YtKind.ChannelPreview)
            {
                ChannelPreviewHolder holder = (ChannelPreviewHolder)viewHolder;
                Channel channel = items[position].channel;

                holder.Name.Text = channel.Name;
                Picasso.With(Android.App.Application.Context).Load(channel.ImageURL).Placeholder(Resource.Color.placeholder).Transform(new CircleTransformation(true)).Into(holder.Logo);


                DisplayMetrics displayMetrics = new DisplayMetrics();
                MainActivity.instance.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
                int wholeWidth = (displayMetrics.WidthPixels - MainActivity.instance.DpToPx(15)) / 2;
                int mixHeight = (int)(wholeWidth * 0.5625); /// width / 16 */ 9
                holder.MixOne.LayoutParameters.Height = mixHeight;
                holder.MixOne.RequestLayout();
                holder.MixTwo.LayoutParameters.Height = mixHeight;
                holder.ChannelLogo.LayoutParameters.Height = mixHeight * 2;

                Picasso.With(Android.App.Application.Context).Load(channel.ImageURL).Placeholder(Resource.Color.placeholder).Fit().CenterCrop().Into(holder.ChannelLogo);

                List<YtFile> files = items.FindAll(x => x.Kind == YtKind.Video && x.song.Artist == channel.Name);
                if (files.Count > 0)
                    Picasso.With(Android.App.Application.Context).Load(files[0].song.Album).Placeholder(Resource.Color.placeholder).Transform(new RemoveBlackBorder()).Into(holder.MixOne);
                if (files.Count > 1)
                    Picasso.With(Android.App.Application.Context).Load(files[1].song.Album).Placeholder(Resource.Color.placeholder).Transform(new RemoveBlackBorder()).Into(holder.MixTwo);

                if (!holder.ChannelHolder.HasOnClickListeners)
                {
                    holder.ChannelHolder.Click += (sender, e) =>
                    {
                        MainActivity.instance.menu.FindItem(Resource.Id.search).ActionView.Focusable = false;
                        MainActivity.instance.menu.FindItem(Resource.Id.search).CollapseActionView();
                        MainActivity.instance.menu.FindItem(Resource.Id.search).ActionView.Focusable = true;
                        MainActivity.instance.FindViewById(Resource.Id.tabs).Visibility = ViewStates.Gone;
                        MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, ChannelDetails.NewInstance(channel)).AddToBackStack("Channel Details").Commit();
                    };
                }

                if (!holder.MixHolder.HasOnClickListeners)
                {
                    holder.MixHolder.Click += (sender, e) => 
                    {
                        YoutubeManager.MixFromChannel(channel.YoutubeID);
                    };
                }

            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if(viewType == 0)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
                return new SongHolder(itemView, OnClick, OnLongClick);
            }
            else if(viewType == 1)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.PlaylistItem, parent, false);
                return new PlaylistHolder(itemView, OnClick, OnLongClick);
            }
            else if(viewType == 2)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ChannelList, parent, false);
                return new RecyclerChannelHolder(itemView, OnClick, OnLongClick);
            }
            else if(viewType == 3)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ChannelPreview, parent, false);
                return new ChannelPreviewHolder(itemView);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.smallLoading, parent, false);
                return new UslessHolder(itemView);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (items[position].Kind == YtKind.Video)
                return 0;
            else if (items[position].Kind == YtKind.Playlist)
                return 1;
            else if (items[position].Kind == YtKind.Channel)
                return 2;
            else if (items[position].Kind == YtKind.ChannelPreview)
                return 3;
            else
                return 4;

            /*
             * 0: Video
             * 1: Playlist
             * 2: Channel
             * 3: ChannelPreview
             * 4: LoadingBar
             */
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
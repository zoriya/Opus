using Android.Graphics;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class YtAdapter : RecyclerView.Adapter
    {
        public int listPadding;
        private List<YtFile> items;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongCLick;

        public YtAdapter(List<YtFile> items)
        {
            this.items = items;
        }

        public override int ItemCount => items.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            Song song = items[position].item;

            if(items[position].Kind == YtKind.Video)
            {
                RecyclerHolder holder = (RecyclerHolder)viewHolder;

                holder.Title.Text = song.GetName();
                holder.Artist.Text = song.GetArtist();
                holder.reorder.Visibility = ViewStates.Gone;

                var songAlbumArtUri = Android.Net.Uri.Parse(song.GetAlbum());
                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400 * 16 / 9, 400).CenterCrop().Into(holder.AlbumArt);

                holder.more.Tag = position;
                if (!holder.more.HasOnClickListeners)
                {
                    holder.more.Click += (sender, e) =>
                    {
                        int tagPosition = (int)((ImageView)sender).Tag;
                        YoutubeEngine.instances[0].More(items[tagPosition].item);
                    };
                }

                if (MainActivity.Theme == 1)
                {
                    holder.more.SetColorFilter(Color.White);
                    holder.reorder.SetColorFilter(Color.White);
                    holder.Title.SetTextColor(Color.White);
                    holder.Artist.SetTextColor(Color.White);
                    holder.Artist.Alpha = 0.7f;
                }

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
                RecyclerHolder holder = (RecyclerHolder)viewHolder;

                holder.Title.Text = song.GetName();
                holder.Artist.Text = song.GetArtist();

                var songAlbumArtUri = Android.Net.Uri.Parse(song.GetAlbum());
                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);

                holder.more.Tag = position;
                if (!holder.more.HasOnClickListeners)
                {
                    holder.more.Click += (sender, e) =>
                    {
                        int tagPosition = (int)((ImageView)sender).Tag;
                        Song playlist = items[tagPosition].item;
                        
                        AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme);
                        builder.SetTitle("Pick an action");
                        builder.SetItems(new string[] { "Random play", "Download" }, (senderAlert, args) =>
                        {
                            switch (args.Which)
                            {
                                case 0:
                                    YoutubeEngine.RandomPlay(playlist.GetPath());
                                    break;
                                case 1:
                                    YoutubeEngine.DownloadPlaylist(playlist.GetPath());
                                    break;
                                default:
                                    break;
                            }
                        });
                        builder.Show();
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
            else
            {
                RecyclerChannelHolder holder = (RecyclerChannelHolder)viewHolder;

                holder.Title.Text = song.GetName();
                Picasso.With(Android.App.Application.Context).Load(song.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Transform(new CircleTransformation()).Into(holder.AlbumArt);

                if (MainActivity.Theme == 1)
                {
                    holder.Title.SetTextColor(Color.White);
                    holder.Artist.SetTextColor(Color.White);
                    holder.Artist.Alpha = 0.7f;
                }
                holder.CheckBox.Visibility = ViewStates.Gone;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if(viewType == 0)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
                return new RecyclerHolder(itemView, OnClick, OnLongClick);
            }
            else if(viewType == 1)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
                return new RecyclerHolder(itemView, OnClick, OnLongClick);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ChannelList, parent, false);
                return new RecyclerChannelHolder(itemView, OnClick, OnLongClick);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (items[position].Kind == YtKind.Video)
                return 0;
            else if (items[position].Kind == YtKind.Playlist)
                return 1;
            else
                return 2;

            /*
             * 0: Video
             * 1: Playlist
             * 2: Channel
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
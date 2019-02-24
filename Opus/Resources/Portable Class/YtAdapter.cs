using Android.Content;
using Android.Graphics;
using Android.Support.V7.App;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Opus.Resources.values;
using Square.Picasso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Opus.Resources.Portable_Class
{
    public class YtAdapter : RecyclerView.Adapter
    {
        public int listPadding;
        private List<YtFile> items;
        public List<string> selectedTopicsID;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongCLick;

        public YtAdapter(List<YtFile> items, List<string> selectedTopicsID)
        {
            this.items = items;
            this.selectedTopicsID = selectedTopicsID;
        }

        public override int ItemCount => items.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            Song song = items[position].item;

            if(items[position].Kind == YtKind.Video)
            {
                RecyclerHolder holder = (RecyclerHolder)viewHolder;

                holder.Title.Text = song.Title;
                holder.Artist.Text = song.Artist;
                holder.reorder.Visibility = ViewStates.Gone;

                var songAlbumArtUri = Android.Net.Uri.Parse(song.Album);
                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);

                holder.more.Tag = position;
                if (!holder.more.HasOnClickListeners)
                {
                    holder.more.Click += (sender, e) =>
                    {
                        int tagPosition = (int)((ImageView)sender).Tag;
                        YoutubeEngine.instances[0].More(items[tagPosition].item);
                    };
                }

                if (song.IsLiveStream)
                    holder.Live.Visibility = ViewStates.Visible;
                else
                    holder.Live.Visibility = ViewStates.Gone;

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
                PlaylistHolder holder = (PlaylistHolder)viewHolder;

                holder.Title.Text = song.Title;
                holder.Owner.Text = song.Artist;

                var songAlbumArtUri = Android.Net.Uri.Parse(song.Album);
                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);

                holder.more.Tag = position;
                if (!holder.more.HasOnClickListeners)
                {
                    holder.more.Click += (sender, e) =>
                    {
                        int tagPosition = (int)((ImageView)sender).Tag;
                        YoutubeEngine.instances[0].PlaylistMore(items[tagPosition].item);
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
            else if(items[position].Kind == YtKind.Channel)
            {
                RecyclerChannelHolder holder = (RecyclerChannelHolder)viewHolder;

                holder.Title.Text = song.Title;
                Picasso.With(Android.App.Application.Context).Load(song.Album).Placeholder(Resource.Color.background_material_dark).Transform(new CircleTransformation(false)).Into(holder.AlbumArt);

                holder.action.Visibility = ViewStates.Visible;
                holder.CheckBox.Visibility = ViewStates.Gone;

                if (selectedTopicsID.Contains(song.YoutubeID))
                    holder.action.Text = "Unfollow";
                else
                    holder.action.Text = "Follow";

                if (!holder.action.HasOnClickListeners)
                {
                    holder.action.Click += async (sender, e) =>
                    {
                        if (holder.action.Text == "Following" || holder.action.Text == "Unfollow")
                        {
                            holder.action.Text = "Unfollowed";
                            selectedTopicsID.Remove(song.YoutubeID);
                            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(MainActivity.instance);
                            List<string> topics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToList();

                            ISharedPreferencesEditor editor = prefManager.Edit();
                            topics.Remove(song.Title + "/#-#/" + song.YoutubeID);
                            editor.PutStringSet("selectedTopics", topics);
                            editor.Apply();

                            await Task.Delay(1000);
                            holder.action.Text = "Follow";
                        }
                        else if (holder.action.Text == "Follow")
                        {
                            selectedTopicsID.Add(song.YoutubeID);
                            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(MainActivity.instance);
                            List<string> topics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToList();

                            ISharedPreferencesEditor editor = prefManager.Edit();
                            topics.Add(song.Title + "/#-#/" + song.YoutubeID);
                            editor.PutStringSet("selectedTopics", topics);
                            editor.Apply();

                            holder.action.Text = "Following";
                            await Task.Delay(1000);
                            holder.action.Text = "Unfollow";
                        }
                    };
                }

                if (MainActivity.Theme == 1)
                {
                    holder.Title.SetTextColor(Color.White);
                }
            }
            else if(items[position].Kind == YtKind.ChannelPreview)
            {
                ChannelPreviewHolder holder = (ChannelPreviewHolder)viewHolder;

                holder.Name.Text = song.Title;
                Picasso.With(Android.App.Application.Context).Load(song.Album).Placeholder(Resource.Color.background_material_dark).Transform(new CircleTransformation(true)).Into(holder.Logo);

                List<YtFile> files = items.FindAll(x => x.item.Artist == song.Title && x.Kind == YtKind.Video);
                if(files.Count > 0)
                    Picasso.With(Android.App.Application.Context).Load(files[0].item.Album).Transform(new RemoveBlackBorder()).Into(holder.MixOne);
                if (files.Count > 1)
                    Picasso.With(Android.App.Application.Context).Load(files[1].item.Album).Transform(new RemoveBlackBorder()).Into(holder.MixTwo);

                holder.MixOne.ViewTreeObserver.Draw += (sender, e) => 
                {
                    Picasso.With(Android.App.Application.Context).Load(song.Album).Placeholder(Resource.Color.background_material_dark).Fit().CenterCrop().Into(holder.ChannelLogo);
                };

                if (!holder.MixHolder.HasOnClickListeners)
                {
                    holder.MixHolder.Click += (sender, e) => 
                    {
                        YoutubeEngine.instances?[0]?.MixFromChannel(song.YoutubeID);
                    };
                }

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
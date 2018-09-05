using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicApp.Resources.Portable_Class
{
    public class PlaylistTrackAdapter : RecyclerView.Adapter, IItemTouchAdapter
    {
        public List<Song> songList;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongClick;
        public int listPadding;
        private bool empty = false;

        public PlaylistTrackAdapter(List<Song> songList)
        {
            this.songList = songList;
        }

        public void AddToList(List<Song> songList)
        {
            int positionStart = this.songList.Count;
            this.songList.AddRange(songList);
            NotifyItemRangeInserted(positionStart, songList.Count);
        }

        public void Remove(Song song)
        {
            int position = songList.IndexOf(song);
            songList.Remove(song);
            NotifyItemRemoved(position);
        }

        public override int ItemCount
        {
            get
            {
                int count = songList.Count + (PlaylistTracks.instance.fullyLoadded ? 0 : 1) + (PlaylistTracks.instance.useHeader ? 0 : 1);
                if (count == 0 || (count == 1 && !PlaylistTracks.instance.useHeader))
                {
                    empty = true;
                    count++;
                }

                return count;
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if(empty && position + 1 == ItemCount)
            {
                if(PlaylistTracks.instance.tracks.Count == 0)
                    ((TextView)viewHolder.ItemView).Text = "This playlist is empty.";
                else
                    ((TextView)viewHolder.ItemView).Text = "No track found.";
                return;
            }

            if (!PlaylistTracks.instance.useHeader) 
                position--;

            if(position == -1 && !PlaylistTracks.instance.useHeader)
            {
                View header = viewHolder.ItemView;
                header.FindViewById<TextView>(Resource.Id.headerNumber).Text = PlaylistTracks.instance.tracks.Count + " songs";
                header.FindViewById<ImageButton>(Resource.Id.headerPlay).Click += (sender, e0) => { PlaylistTracks.instance.PlayInOrder(0, false); };
                header.FindViewById<ImageButton>(Resource.Id.headerShuffle).Click += (sender, e0) =>
                {
                    if (PlaylistTracks.instance.tracks[0].IsYt)
                    {
                        Random r = new Random();
                        Song[] songs = PlaylistTracks.instance.tracks.OrderBy(x => r.Next()).ToArray();
                        YoutubeEngine.PlayFiles(songs);
                    }
                    else
                    {
                        List<string> tracksPath = new List<string>();
                        foreach (Song song in PlaylistTracks.instance.tracks)
                            tracksPath.Add(song.Path);

                        Intent intent = new Intent(MainActivity.instance, typeof(MusicPlayer));
                        intent.PutStringArrayListExtra("files", tracksPath);
                        intent.SetAction("RandomPlay");
                        MainActivity.instance.StartService(intent);

                        MainActivity.instance.ShowSmallPlayer();
                        MainActivity.instance.ShowPlayer();
                    }
                };
                header.FindViewById<ImageButton>(Resource.Id.headerMore).Click += (sender, e0) =>
                {
                    Android.Support.V7.Widget.PopupMenu menu = new Android.Support.V7.Widget.PopupMenu(MainActivity.instance, header.FindViewById<ImageButton>(Resource.Id.headerMore));
                    menu.Inflate(Resource.Menu.playlist_smallheader_more);
                    menu.SetOnMenuItemClickListener(PlaylistTracks.instance);
                    menu.Show();
                };

                if (MainActivity.Theme != 1)
                {
                    header.SetBackgroundColor(Color.Argb(255, 255, 255, 255));
                    header.FindViewById<ImageButton>(Resource.Id.headerPlay).ImageTintList = ColorStateList.ValueOf(Color.Black);
                    header.FindViewById<ImageButton>(Resource.Id.headerShuffle).ImageTintList = ColorStateList.ValueOf(Color.Black);
                    header.FindViewById<ImageButton>(Resource.Id.headerMore).ImageTintList = ColorStateList.ValueOf(Color.Black);
                }

                return;
            }

            if(position >= songList.Count)
                return;

            RecyclerHolder holder = (RecyclerHolder)viewHolder;

            holder.Title.Text = songList[position].Title;
            holder.Artist.Text = songList[position].Artist;

            if (songList[position].AlbumArt == -1 || songList[position].IsYt)
            {
                var songAlbumArtUri = Android.Net.Uri.Parse(songList[position].Album);
                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
            }
            else
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, songList[position].AlbumArt);

                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
            }

            holder.more.Tag = position;
            if (!holder.more.HasOnClickListeners)
            {
                holder.more.Click += (sender, e) =>
                {
                    int tagPosition = (int)((ImageView)sender).Tag;
                    PlaylistTracks.instance.More(songList[tagPosition], tagPosition);
                };
            }

            float scale = MainActivity.instance.Resources.DisplayMetrics.Density;

            if (MainActivity.Theme == 1)
            {
                holder.more.SetColorFilter(Color.White);
                holder.Title.SetTextColor(Color.White);
                holder.Artist.SetTextColor(Color.White);
                holder.Artist.Alpha = 0.7f;
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
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.smallLoading, parent, false);
                return new UslessHolder(itemView);
            }
            else if(viewType == 2)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.PlaylistSmallHeader, parent, false);
                return new UslessHolder(itemView);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.EmptyView, parent, false);
                return new EmptyHolder(itemView);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (empty && position + 1 == ItemCount)
                return 3;

            if (position == 0 && !PlaylistTracks.instance.useHeader)
                return 2;
            else if (position - (PlaylistTracks.instance.useHeader ? 0 : 1) >= songList.Count)
                return 1;
            return 0;
        }

        void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        void OnLongClick(int position)
        {
            ItemLongClick?.Invoke(this, position);
        }

        public void ItemMoved(int fromPosition, int toPosition) { }

        public void ItemMoveEnded(int fromPosition, int toPosition) { }

        public void ItemDismissed(int position)
        {
            PlaylistTracks.instance.DeleteDialog(position);
        }
    }
}
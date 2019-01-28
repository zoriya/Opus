using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using MusicApp.Resources.values;
using Square.Picasso;
using System;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class BrowseAdapter : RecyclerView.Adapter
    {
        public List<Song> songList;
        public bool displayShuffle;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongCLick;

        public BrowseAdapter(List<Song> songList, bool displayShuffle)
        {
            this.songList = songList;
            this.displayShuffle = displayShuffle;
        }

        public override int ItemCount => songList.Count + (displayShuffle ? 1 : 0);

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if(position == 0 && displayShuffle)
            {
                if (MainActivity.Theme == 1)
                    ((CardView)viewHolder.ItemView).SetCardBackgroundColor(Color.ParseColor("#212121"));
                else
                    ((CardView)viewHolder.ItemView).SetCardBackgroundColor(Color.White);
            }
            else
            {
                RecyclerHolder holder = (RecyclerHolder)viewHolder;

                if (MainActivity.Theme == 1)
                {
                    holder.more.SetColorFilter(Color.White);
                    holder.Title.SetTextColor(Color.White);
                    holder.Artist.SetTextColor(Color.White);
                    holder.Artist.Alpha = 0.7f;
                    holder.ItemView.SetBackgroundColor(Color.ParseColor("#424242"));
                }
                else
                    holder.ItemView.SetBackgroundColor(Color.White);

                Song song = songList[position - (displayShuffle ? 1 : 0)];

                holder.Title.Text = song.Title;
                holder.Artist.Text = song.Artist;

                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, song.AlbumArt);
                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.background_material_dark).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);

                if (!holder.more.HasOnClickListeners)
                {
                    holder.more.Click += (sender, e) =>
                    {
                        if (Browse.instance != null)
                        {
                            Browse.instance.More(songList[holder.AdapterPosition - (displayShuffle ? 1 : 0)], holder.AdapterPosition - (displayShuffle ? 1 : 0));
                        }
                    };
                }
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == 0)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.BrowseShuffle, parent, false);
                return new UslessHolder(itemView, OnClick);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
                return new RecyclerHolder(itemView, OnClick, OnLongClick);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position == 0 && displayShuffle)
                return 0;
            else
                return 1;
        }

        void OnClick(int position)
        {
            if (position == 0 && displayShuffle)
                MainActivity.instance.ShuffleAll();
            else
                ItemClick?.Invoke(this, position - (displayShuffle ? 1 : 0));
        }

        void OnLongClick(int position)
        {
            ItemLongCLick?.Invoke(this, position - (displayShuffle ? 1 : 0));
        }
    }
}
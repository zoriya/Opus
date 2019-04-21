using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Opus.Api;
using Opus.DataStructure;
using Opus.Fragments;
using Square.Picasso;
using System;

namespace Opus.Adapter
{
    public class BrowseAdapter : BaseCursor<Song>
    {
        public bool displayShuffle;
        public override int ItemBefore => (displayShuffle && BaseCount != 0) ? 1 : 0;

        private Action<Song, int> clickAction;
        private Action<Song, int> longClickAction;
        private Action<int> headerAction;

        public BrowseAdapter(Action<Song, int> clickAction, Action<Song, int> longClickAction, Action<int> headerAction = null)
        {
            cursor = null;
            displayShuffle = true;
            this.clickAction = clickAction;
            this.longClickAction = longClickAction;
            this.headerAction = headerAction;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (position == 0 && displayShuffle)
            {
                if (MainActivity.Theme == 1)
                    ((CardView)viewHolder.ItemView).SetCardBackgroundColor(Color.ParseColor("#212121"));
                else
                    ((CardView)viewHolder.ItemView).SetCardBackgroundColor(Color.White);
            }
            else
                base.OnBindViewHolder(viewHolder, position);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, Song song)
        {
            SongHolder holder = (SongHolder)viewHolder;

            if (MainActivity.Theme == 1)
            {
                holder.more.SetColorFilter(Color.White);
                holder.Title.SetTextColor(Color.White);
                holder.Artist.SetTextColor(Color.White);
                holder.Artist.Alpha = 0.7f;
            }

            holder.Title.Text = song.Title;
            holder.Artist.Text = song.Artist;

            var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
            var songAlbumArtUri = ContentUris.WithAppendedId(songCover, song.AlbumArt);
            Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.placeholder).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);

            if (!holder.more.HasOnClickListeners)
            {
                holder.more.Click += (sender, e) =>
                {
                    Browse.instance?.More(GetItem(holder.AdapterPosition));
                };
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == 0)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ShuffleButton, parent, false);
                return new UslessHolder(itemView, OnClick);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
                return new SongHolder(itemView, OnClick, OnLongClick);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position == 0 && displayShuffle)
                return 0;
            else
                return 1;
        }

        public override void HeaderClicked(int position)
        {
            headerAction?.Invoke(position);
        }

        public override void Clicked(Song song, int position)
        {
            clickAction?.Invoke(song, position);
        }

        public override void LongClicked(Song song, int position)
        {
            longClickAction?.Invoke(song, position);
        }

        public override Song Convert(ICursor cursor)
        {
            return Song.FromCursor(cursor);
        }
    }
}
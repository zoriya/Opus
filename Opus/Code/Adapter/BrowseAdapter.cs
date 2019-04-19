using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Opus.DataStructure;
using Opus.Fragments;
using Square.Picasso;

namespace Opus.Adapter
{
    public class BrowseAdapter : BaseCursor<Song>
    {
        public bool displayShuffle;
        public override int ItemBefore => (displayShuffle && BaseCount != 0) ? 1 : 0;


        public BrowseAdapter()
        {
            cursor = null;
            displayShuffle = true;
        }

        public BrowseAdapter(ICursor cursor, bool displayShuffle)
        {
            this.cursor = cursor;
            this.displayShuffle = displayShuffle;
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
            Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.background_material_dark).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);

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
            MainActivity.instance.ShuffleAll();
        }

        public override void Clicked(Song song)
        {
            song = Browse.CompleteItem(song);
            Browse.Play(song);
        }

        public override void LongClicked(Song song)
        {
            Browse.instance?.More(song);
        }

        public override Song Convert(ICursor cursor)
        {
            return Song.FromCursor(cursor);
        }
    }
}
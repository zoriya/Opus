using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Database;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Opus.Api;
using Opus.DataStructure;
using Opus.Fragments;
using Opus.Others;
using Square.Picasso;

namespace Opus.Adapter
{
    public class PlaylistTrackAdapter : BaseCursor<Song>, IItemTouchAdapter
    {
        public SearchableList<Song> tracks;
        public bool IsSliding { get; set; }


        public PlaylistTrackAdapter() { }
        public PlaylistTrackAdapter(SearchableList<Song> tracks)
        {
            this.tracks = tracks;
            cursor = null;
        }

        public override int ItemBefore
        {
            get
            {
                int count = PlaylistTracks.instance.useHeader ? 0 : 1; //Display the smallheader if the playlist doesnt use the big header
                if (BaseCount == 0 && PlaylistTracks.instance.fullyLoadded) //Display an empty view if the playlist is fully loaded and there is no tracks
                    count++;

                return count;
            }
        }

        private int ItemAfter
        {
            get
            {
                return PlaylistTracks.instance.fullyLoadded ? 0 : 1; //Display a loading bar if the playlist is not fully loaded 
            }
        }

        public override int BaseCount => tracks == null ? base.BaseCount : tracks.Count;
        public override int ItemCount => base.ItemCount + ItemAfter;


        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            System.Console.WriteLine("&Binding at " + position);
            System.Console.WriteLine("&ItemCount " + ItemCount);
            System.Console.WriteLine("&ItemBefore " + ItemBefore);
            System.Console.WriteLine("&ItemAfter " + ItemAfter);

            if (position == ItemCount - 1 && !PlaylistTracks.instance.fullyLoadded)
            {
                int pad = MainActivity.instance.DpToPx(30);
                ((RecyclerView.LayoutParams)viewHolder.ItemView.LayoutParameters).TopMargin = pad;
                ((RecyclerView.LayoutParams)viewHolder.ItemView.LayoutParameters).BottomMargin = pad;
            }
            else if (position == 0 && !PlaylistTracks.instance.useHeader)
            {
                View header = viewHolder.ItemView;
                header.FindViewById<TextView>(Resource.Id.headerNumber).Text = tracks.Count + " " + (tracks.Count < 2 ? MainActivity.instance.GetString(Resource.String.element) : MainActivity.instance.GetString(Resource.String.elements));
                if (!header.FindViewById<ImageButton>(Resource.Id.headerPlay).HasOnClickListeners)
                {
                    header.FindViewById<ImageButton>(Resource.Id.headerPlay).Click += (sender, e0) => { SongManager.PlayInOrder(tracks); };
                    header.FindViewById<ImageButton>(Resource.Id.headerShuffle).Click += (sender, e0) =>
                    {
                        SongManager.Shuffle(tracks);
                    };
                    header.FindViewById<ImageButton>(Resource.Id.headerMore).Click += (sender, e0) =>
                    {
                        Android.Support.V7.Widget.PopupMenu menu = new Android.Support.V7.Widget.PopupMenu(MainActivity.instance, header.FindViewById<ImageButton>(Resource.Id.headerMore));
                        menu.Inflate(Resource.Menu.playlist_smallheader_more);
                        menu.SetOnMenuItemClickListener(PlaylistTracks.instance);
                        menu.Show();
                    };
                }

                if (MainActivity.Theme != 1)
                {
                    header.SetBackgroundColor(Color.Argb(255, 255, 255, 255));
                    header.FindViewById<ImageButton>(Resource.Id.headerPlay).ImageTintList = ColorStateList.ValueOf(Color.Black);
                    header.FindViewById<ImageButton>(Resource.Id.headerShuffle).ImageTintList = ColorStateList.ValueOf(Color.Black);
                    header.FindViewById<ImageButton>(Resource.Id.headerMore).ImageTintList = ColorStateList.ValueOf(Color.Black);
                }
            }
            else if (BaseCount == 0)
            {
                ((TextView)viewHolder.ItemView).Text = MainActivity.instance.GetString(Resource.String.playlist_empty);
            }
            else if (tracks != null)
                OnBindViewHolder(viewHolder, tracks[position - ItemBefore]);
            else
                base.OnBindViewHolder(viewHolder, position);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, Song item)
        {
            SongHolder holder = (SongHolder)viewHolder;

            holder.Title.Text = item.Title;
            holder.Artist.Text = item.Artist;

            if (item.AlbumArt == -1 || item.IsYt)
            {
                var songAlbumArtUri = Android.Net.Uri.Parse(item.Album);
                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.placeholder).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);
            }
            else
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, item.AlbumArt);

                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.placeholder).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
            }

            if (item.IsLiveStream)
                holder.Live.Visibility = ViewStates.Visible;
            else
                holder.Live.Visibility = ViewStates.Gone;

            if (!holder.more.HasOnClickListeners)
            {
                holder.more.Click += (sender, e) =>
                {
                    PlaylistTracks.instance.More(tracks == null ? GetItem(holder.AdapterPosition) : tracks[holder.AdapterPosition], holder.AdapterPosition);
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

        public override void OnClick(int position)
        {
            if (tracks != null && position >= ItemBefore)
                Clicked(tracks[position - ItemBefore], position - ItemBefore);
            else
                base.OnClick(position);
        }

        public override void Clicked(Song item, int position)
        {
            if (PlaylistTracks.instance.useHeader)
                PlaylistManager.PlayInOrder(PlaylistTracks.instance.item, position);
            else
                SongManager.Play(item);
        }

        public override void OnLongClick(int position)
        {
            if (tracks != null && position >= ItemBefore)
                LongClicked(tracks[position - ItemBefore], position - ItemBefore);
            else
                base.OnLongClick(position);
        }

        public override void LongClicked(Song item, int position)
        {
            PlaylistTracks.instance.More(item, position);
        }

        public override Song Convert(ICursor cursor)
        {
            return Song.FromCursor(cursor);
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
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SmallEmptyView, parent, false);
                return new EmptyHolder(itemView);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position == ItemCount - 1 && !PlaylistTracks.instance.fullyLoadded)
                return 1;
            else if (position == 0 && !PlaylistTracks.instance.useHeader)
                return 2;
            else if (BaseCount == 0)
                return 3;
            else
                return 0;
        }


        public void ItemMoved(int fromPosition, int toPosition) { }

        public void ItemMoveEnded(int fromPosition, int toPosition) { }

        public void ItemDismissed(int position)
        {
            PlaylistTracks.instance.RemoveFromPlaylist(tracks[position], position);
        }
    }
}
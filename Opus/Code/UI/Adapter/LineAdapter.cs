using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Opus.Api;
using Opus.Api.Services;
using Opus.DataStructure;
using Opus.Fragments;
using Opus.Others;
using Square.Picasso;
using System.Collections.Generic;

namespace Opus.Adapter
{
    public class LineAdapter : RecyclerView.Adapter
    {
        private enum ListType { Song, Queue, Favs }

        public RecyclerView recycler;
        public int listPadding = 0;
        private readonly ListType type = ListType.Song;
        private List<Song> songs;
        private bool isEmpty = false;

        public override int ItemCount
        {
            get
            {
                int count = type == ListType.Queue && MusicPlayer.UseCastPlayer ? MusicPlayer.RemotePlayer.MediaQueue.ItemCount : songs.Count;

                if (type == ListType.Favs)
                    count++;

                if (count == 0)
                {
                    count++;
                    isEmpty = true;
                }
                else
                    isEmpty = false;

                return count;
            }
        }

        public LineAdapter(List<Song> songList, RecyclerView recycler)
        {
            this.recycler = recycler;
            this.songs = songList;
        }

        public LineAdapter(bool FavPlaylist, List<Song> songList, RecyclerView recycler)
        {
            if (FavPlaylist)
                type = ListType.Favs;
            this.recycler = recycler;
            this.songs = songList;
        }
        /*
         * Use this method if the songList is the queue
         */
        public LineAdapter(RecyclerView recycler)
        {
            this.recycler = recycler;
            type = ListType.Queue;
            songs = MusicPlayer.queue;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (position == 0 && isEmpty)
            {
                EmptyHolder holder = (EmptyHolder)viewHolder;
                holder.text.Text = type == ListType.Queue ? MainActivity.instance.GetString(Resource.String.empty_queue) : MainActivity.instance.GetString(Resource.String.long_loading);
                holder.text.SetHeight(MainActivity.instance.DpToPx(157));
                return;
            }
            else
            {
                SongHolder holder = (SongHolder)viewHolder;

                if (type == ListType.Favs)
                    position--;

                if(position == -1)
                {
                    holder.Title.Text = MainActivity.instance.GetString(Resource.String.shuffle_all);
                    holder.AlbumArt.SetImageResource(Resource.Drawable.Shuffle);

                    Color color;
                    TypedValue value = new TypedValue();
                    if (MainActivity.instance.Theme.ResolveAttribute(Android.Resource.Attribute.ColorForeground, value, true))
                        color = Color.ParseColor("#" + Integer.ToHexString(value.Data));
                    else
                        color = Color.Black;

                    holder.AlbumArt.SetColorFilter(color);
                }
                else
                {
                    holder.AlbumArt.ClearColorFilter();


                    Song song = songs.Count <= position ? null : songs[position];


                    if (song == null && type == ListType.Queue)
                    {
                        holder.Title.Text = "";
                        holder.AlbumArt.SetImageResource(Resource.Color.background_material_dark);

                        MusicPlayer.RemotePlayer.MediaQueue.GetItemAtIndex(position);
                        return;
                    }

                    holder.Title.Text = song.Title;

                    if (song.AlbumArt == -1 || song.IsYt)
                    {
                        if (song.Album != null)
                            Picasso.With(Application.Context).Load(song.Album).Placeholder(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);
                        else
                            Picasso.With(Application.Context).Load(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);
                    }
                    else
                    {
                        var songCover = Uri.Parse("content://media/external/audio/albumart");
                        var songAlbumArtUri = ContentUris.WithAppendedId(songCover, song.AlbumArt);

                        Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.background_material_dark).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
                    }
                }
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position, IList<Java.Lang.Object> payloads)
        {
            if (payloads.Count > 0)
            {
                SongHolder holder = (SongHolder)viewHolder;

                if(payloads[0].ToString() == holder.Title.Text)
                    return;

                if (payloads[0].ToString() != null)
                {
                    Song song = MusicPlayer.queue[position];

                    if (holder.Title.Text == "" || holder.Title.Text == null)
                        holder.Title.Text = song.Title;

                    if (song.IsYt)
                    {
                        Picasso.With(Application.Context).Load(song.Album).Placeholder(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);
                    }
                    return;
                }
            }

            base.OnBindViewHolder(viewHolder, position, payloads);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == 0)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.LineSong, parent, false);
                return new SongHolder(itemView, OnClick, OnLongClick);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.EmptyView, parent, false);
                return new EmptyHolder(itemView);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (isEmpty && position == 0)
                return 1;
            else
                return 0;
        }

        public async void Refresh()
        {
            if(type == ListType.Favs)
                songs = await SongManager.GetFavorites();

            if(songs.Count > 0)
                NotifyDataSetChanged();
            else
            {
                int pos = Home.sections.FindIndex(x => x.SectionTitle == "Fav");
                Home.sections.RemoveAt(pos);
                Home.instance.adapter.NotifyItemRemoved(pos);
            }
        }

        void OnClick(int position)
        {
            if (type == ListType.Favs)
                position--;

            if (type == ListType.Queue)
            {
                if (MusicPlayer.instance != null)
                    MusicPlayer.instance.SwitchQueue(position);
                else
                {
                    Intent intent = new Intent(MainActivity.instance, typeof(MusicPlayer));
                    intent.SetAction("SwitchQueue");
                    intent.PutExtra("queueSlot", position);
                    MainActivity.instance.StartService(intent);
                }
            }
            else if (position == -1)
                SongManager.Shuffle(songs);
            else
                SongManager.Play(songs[position]);
        }

        void OnLongClick(int position)
        {
            if (type == ListType.Favs)
                position--;

            if (position == -1)
                return;

            Song item = songs[position];

            if (type == ListType.Queue)
            {
                BottomSheetAction endAction = new BottomSheetAction(Resource.Drawable.Close, MainActivity.instance.GetString(Resource.String.remove_from_queue), (sender, eventArg) =>
                {
                    MusicPlayer.RemoveFromQueue(position);
                });

                MainActivity.instance.More(item, () => { OnClick(position); }, endAction);
            }
            else
                MainActivity.instance.More(item);
        }
    }
}
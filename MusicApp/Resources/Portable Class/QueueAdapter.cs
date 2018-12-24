using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Java.Lang;
using MusicApp.Resources.values;
using Square.Picasso;
using System;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class QueueAdapter : RecyclerView.Adapter, IItemTouchAdapter
    {
        public List<Song> songList;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongCLick;
        public int listPadding;

        public QueueAdapter(List<Song> songList)
        {
            this.songList = songList;
        }

        public void UpdateList(List<Song> songList)
        {
            this.songList = songList;
            NotifyDataSetChanged();
        }

        public void AddToList(List<Song> songList)
        {
            int positionStart = this.songList.Count;
            this.songList.AddRange(songList);
            NotifyItemRangeInserted(positionStart, songList.Count);
        }

        public override int ItemCount => MusicPlayer.UseCastPlayer ? MusicPlayer.RemotePlayer.MediaQueue.ItemCount + 1 : songList.Count + 1;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (position + 1 == ItemCount)
            {
                QueueFooter holder = (QueueFooter)viewHolder;
                holder.switchButton.Checked = MusicPlayer.useAutoPlay;
                if (!holder.switchButton.HasOnClickListeners)
                {
                    holder.switchButton.Click += (sender, e) =>
                    {
                        MusicPlayer.useAutoPlay = !MusicPlayer.useAutoPlay;
                        if (MusicPlayer.useAutoPlay)
                        {
                            MusicPlayer.repeat = false;
                            Queue.instance.menu.FindItem(Resource.Id.repeat).Icon.ClearColorFilter();
                        }
                    };
                }
            }
            else
            {
                RecyclerHolder holder = (RecyclerHolder)viewHolder;

                if (MainActivity.Theme == 1)
                {
                    holder.more.SetColorFilter(Color.White);
                    holder.reorder.SetColorFilter(Color.White);
                    holder.Title.SetTextColor(Color.White);
                    holder.Artist.SetTextColor(Color.White);
                    holder.Artist.Alpha = 0.7f;
                    holder.youtubeIcon.SetColorFilter(Color.White);
                    holder.ItemView.SetBackgroundColor(Color.ParseColor("#424242"));
                }
                else
                    holder.ItemView.SetBackgroundColor(Color.White);

                holder.reorder.Visibility = ViewStates.Visible;
                if (position == MusicPlayer.CurrentID())
                {
                    holder.status.Visibility = ViewStates.Visible;
                    holder.status.Text = MusicPlayer.isRunning ? "Playing" : "Paused";
                    holder.status.SetTextColor(MusicPlayer.isRunning ? Color.Argb(255, 244, 81, 30) : Color.Argb(255, 66, 165, 245));
                }
                else
                    holder.status.Visibility = ViewStates.Gone;


                Song song = songList.Count <= position ? null : songList[position];
                if (song == null)
                {
                    if (holder.Title.Text.Length == 0)
                        holder.Title.Text = "aizquruhgqognbq";
                    if (holder.Artist.Text.Length == 0)
                        holder.Artist.Text = "ZJKGNZgzn";

                    holder.Title.SetTextColor(Color.Transparent);
                    holder.Title.SetBackgroundResource(Resource.Color.background_material_dark);
                    holder.Artist.SetTextColor(Color.Transparent);
                    holder.Artist.SetBackgroundResource(Resource.Color.background_material_dark);
                    holder.AlbumArt.SetImageResource(Resource.Color.background_material_dark);

                    MusicPlayer.RemotePlayer.MediaQueue.GetItemAtIndex(position);
                    return;
                }
                else
                {
                    holder.Title.SetBackgroundResource(0);
                    holder.Artist.SetBackgroundResource(0);
                }

                holder.Title.Text = song.Title;
                holder.Artist.Text = song.Artist;

                if (song.AlbumArt == -1 || song.IsYt)
                {
                    var songAlbumArtUri = Android.Net.Uri.Parse(song.Album);
                    Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);
                }
                else
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var songAlbumArtUri = ContentUris.WithAppendedId(songCover, song.AlbumArt);

                    Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
                }

                if (!holder.more.HasOnClickListeners)
                {
                    holder.more.Click += (sender, e) =>
                    {
                        if (Queue.instance != null)
                            Queue.instance.More(holder.AdapterPosition);
                    };
                }

                if (song.IsLiveStream)
                    holder.Live.Visibility = ViewStates.Visible;
                else
                    holder.Live.Visibility = ViewStates.Gone;


                if (!holder.reorder.HasOnClickListeners)
                {
                    holder.reorder.Touch += (sender, e) =>
                    {
                        Queue.instance.itemTouchHelper.StartDrag(viewHolder);
                        MainActivity.instance.contentRefresh.SetEnabled(false);
                    };
                }

                if (position == MusicPlayer.CurrentID())
                    holder.Title.SetTextSize(Android.Util.ComplexUnitType.Dip, 18);
                else
                    holder.Title.SetTextSize(Android.Util.ComplexUnitType.Dip, 14);

                if (!song.IsParsed && song.IsYt)
                {
                    holder.youtubeIcon.SetImageResource(Resource.Drawable.needProcessing);
                    holder.youtubeIcon.Visibility = ViewStates.Visible;
                }
                else if (song.IsYt)
                {
                    holder.youtubeIcon.SetImageResource(Resource.Drawable.PublicIcon);
                    holder.youtubeIcon.Visibility = ViewStates.Visible;
                }
                else
                {
                    holder.youtubeIcon.Visibility = ViewStates.Gone;
                }
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position, IList<Java.Lang.Object> payloads)
        {
            if (payloads.Count > 0 && payloads[0].ToString() == ((RecyclerHolder)holder).Title.Text)
                return;

            base.OnBindViewHolder(holder, position, payloads);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == 0)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.SongList, parent, false);
                return new RecyclerHolder(itemView, OnClick, OnLongClick);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.QueueFooter, parent, false);
                return new QueueFooter(itemView);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position + 1 == ItemCount)
                return 1;
            else
                return 0;
        }

        void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        void OnLongClick(int position)
        {
            ItemLongCLick?.Invoke(this, position);
        }

        public void ItemMoved(int fromPosition, int toPosition)
        {
            if(fromPosition < toPosition)
            {
                for(int i = fromPosition; i < toPosition; i++)
                    songList = Swap(songList, i, i + 1);
            }
            else
            {
                for(int i = fromPosition; i > toPosition; i--)
                    songList = Swap(songList, i, i - 1);
            }

            NotifyItemMoved(fromPosition, toPosition);
        }

        public void ItemMoveEnded(int fromPosition, int toPosition)
        {
            if (MusicPlayer.CurrentID() == fromPosition)
                MusicPlayer.currentID = toPosition;

            if (MusicPlayer.CurrentID() > fromPosition && MusicPlayer.CurrentID() <= toPosition)
                MusicPlayer.currentID--;

            MusicPlayer.UpdateQueueSlots();
        }
        
        List<T> Swap<T>(List<T> list, int fromPosition, int toPosition)
        {
            T item = list[fromPosition];
            list[fromPosition] = list[toPosition];
            list[toPosition] = item;
            return list;
        }

        public void ItemDismissed(int position)
        {
            Song song = songList[position];
            Queue.RemoveFromQueue(position);
            NotifyItemRemoved(position);
            Snackbar.Make(Queue.instance.FindViewById(Resource.Id.recycler), (song.Title.Length > 20 ? song.Title.Substring(0, 17) + "..." : song.Title) + " has been removed from the queue.", Snackbar.LengthShort)
                .SetAction("Undo", (view) => 
                {
                    Queue.InsertToQueue(position, song);
                    NotifyItemInserted(position);
                })
                .Show();
        }
    }
}
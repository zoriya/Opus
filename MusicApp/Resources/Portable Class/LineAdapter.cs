using Android.App;
using Android.Content;
using Android.Gms.Cast.Framework.Media;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System.Collections.Generic;
using System.Linq;

namespace MusicApp.Resources.Portable_Class
{
    public class LineAdapter : RecyclerView.Adapter
    {
        public RecyclerView recycler;
        public int listPadding = 0;
        private readonly bool UseQueue = false;
        private List<Song> songList;
        private bool isEmpty = false;

        private readonly string[] actions = new string[] { "Play", "Play Next", "Play Last", "Add To Playlist" };

        public override int ItemCount
        {
            get
            {
                int count = UseQueue && MusicPlayer.UseCastPlayer ? MusicPlayer.RemotePlayer.MediaQueue.ItemCount : songList.Count;
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
            this.songList = songList;
        }
        /*
         * Use this method if the songList is the queue
         */
        public LineAdapter(RecyclerView recycler)
        {
            this.recycler = recycler;
            UseQueue = true;
            songList = MusicPlayer.queue;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (position == 0 && isEmpty)
            {
                EmptyHolder holder = (EmptyHolder)viewHolder;
                holder.text.Text = "No song currently in queue,\nstart playing song now !";
                holder.text.SetHeight(MainActivity.instance.DpToPx(157));
                return;
            }
            else
            {
                RecyclerHolder holder = (RecyclerHolder)viewHolder;

                Song song = songList.Count <= position ? null : songList[position];


                if (song == null && UseQueue)
                {
                    holder.Title.Text = "";
                    holder.AlbumArt.SetImageResource(Resource.Color.background_material_dark);

                    MusicPlayer.RemotePlayer.MediaQueue.GetItemAtIndex(position);
                    return;
                }

                holder.Title.Text = song.Title;

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
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.LineSong, parent, false);
                return new RecyclerHolder(itemView, OnClick, OnLongClick);
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

        void OnClick(int position)
        {
            if (UseQueue)
            {
                if(MusicPlayer.instance != null)
                    MusicPlayer.instance.SwitchQueue(position);
                else
                {
                    Intent intent = new Intent(MainActivity.instance, typeof(MusicPlayer));
                    intent.SetAction("SwitchQueue");
                    intent.PutExtra("queueSlot", position);
                    MainActivity.instance.StartService(intent);
                }
            }
            else if (!songList[position].IsYt)
                Browse.Play(songList[position], recycler.GetLayoutManager().FindViewByPosition(position).FindViewById<ImageView>(Resource.Id.albumArt));
            else
                YoutubeEngine.Play(songList[position].YoutubeID, songList[position].Title, songList[position].Artist, songList[position].Album);
        }

        void OnLongClick(int position)
        {
            Song item = songList[position];

            if (!UseQueue)
            {
                List<string> action = actions.ToList();

                if (!item.IsYt)
                {
                    action.Add("Edit Metadata");
                    Browse.act = MainActivity.instance;
                    Browse.inflater = MainActivity.instance.LayoutInflater;
                }
                else
                {
                    action.Add("Download");
                }

                AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity.instance);
                builder.SetTitle("Pick an action");
                builder.SetItems(action.ToArray(), (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            if (!item.IsYt)
                                Browse.Play(item, recycler.GetLayoutManager().FindViewByPosition(position).FindViewById<ImageView>(Resource.Id.albumArt));
                            else
                                YoutubeEngine.Play(item.YoutubeID, item.Title, item.Artist, item.Album);
                            break;

                        case 1:
                            if (!item.IsYt)
                                Browse.PlayNext(item);
                            else
                                YoutubeEngine.PlayNext(item.Path, item.Title, item.Artist, item.Album);
                            break;

                        case 2:
                            if (!item.IsYt)
                                Browse.PlayLast(item);
                            else
                                YoutubeEngine.PlayLast(item.Path, item.Title, item.Artist, item.Album);
                            break;

                        case 3:
                            Browse.GetPlaylist(item);
                            break;

                        case 5:
                            if (item.IsYt)
                                YoutubeEngine.Download(item.Title, item.Path);
                            else
                                Browse.EditMetadata(item, "PlaylistTracks", Home.instance.ListView.GetLayoutManager().OnSaveInstanceState());
                            break;

                        default:
                            break;
                    }
                });
                builder.Show();
            }
            else
            {
                List<string> action = new List<string> { actions[0], actions[3] };

                if (!item.IsYt)
                {
                    action.Add("Edit Metadata");
                    Browse.act = MainActivity.instance;
                    Browse.inflater = MainActivity.instance.LayoutInflater;
                }
                else
                {
                    action.Add("Download");
                }

                AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity.instance);
                builder.SetTitle("Pick an action");
                builder.SetItems(action.ToArray(), (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            if (!item.IsYt)
                                Browse.Play(item, recycler.GetLayoutManager().FindViewByPosition(position).FindViewById<ImageView>(Resource.Id.albumArt));
                            else
                                YoutubeEngine.Play(item.YoutubeID, item.Title, item.Artist, item.Album);
                            break;

                        case 1:
                            Browse.GetPlaylist(item);
                            break;

                        case 2:
                            if (item.IsYt)
                                YoutubeEngine.Download(item.Title, item.Path);
                            else
                                Browse.EditMetadata(item, "PlaylistTracks", Home.instance.ListView.GetLayoutManager().OnSaveInstanceState());
                            break;

                        default:
                            break;
                    }
                });
                builder.Show();
            }
        }
    }
}
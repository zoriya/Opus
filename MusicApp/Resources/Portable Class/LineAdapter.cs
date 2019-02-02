using Android.App;
using Android.Content;
using Android.Gms.Cast.Framework.Media;
using Android.Graphics;
using Android.Net;
using Android.Support.Design.Widget;
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
                    var songAlbumArtUri = Uri.Parse(song.Album);
                    Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);
                }
                else
                {
                    var songCover = Uri.Parse("content://media/external/audio/albumart");
                    var songAlbumArtUri = ContentUris.WithAppendedId(songCover, song.AlbumArt);

                    Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.background_material_dark).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
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
                Browse.Play(songList[position]);
            else
                YoutubeEngine.Play(songList[position].YoutubeID, songList[position].Title, songList[position].Artist, songList[position].Album);
        }

        void OnLongClick(int position)
        {
            Song item = songList[position];

            if (UseQueue)
            {
                BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
                View bottomView = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
                bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Title;
                bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Artist;
                if (item.Album == null)
                {
                    var songCover = Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, item.AlbumArt);

                    Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
                }
                else
                {
                    Picasso.With(MainActivity.instance).Load(item.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
                }
                bottomSheet.SetContentView(bottomView);

                List<BottomSheetAction> actions = new List<BottomSheetAction>
                {
                    new BottomSheetAction(Resource.Drawable.Play, MainActivity.instance.Resources.GetString(Resource.String.play), (sender, eventArg) => { OnClick(position); bottomSheet.Dismiss(); }),
                    new BottomSheetAction(Resource.Drawable.Close, MainActivity.instance.Resources.GetString(Resource.String.remove_from_queue), (sender, eventArg) => { Queue.RemoveFromQueue(position); bottomSheet.Dismiss(); }),
                    new BottomSheetAction(Resource.Drawable.PlaylistAdd, MainActivity.instance.Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) => { Browse.GetPlaylist(item); bottomSheet.Dismiss(); })
                };

                if (item.IsYt)
                {
                    actions.AddRange(new BottomSheetAction[]
                    {
                        new BottomSheetAction(Resource.Drawable.PlayCircle, MainActivity.instance.Resources.GetString(Resource.String.create_mix_from_song), (sender, eventArg) =>
                        {
                            YoutubeEngine.CreateMix(item);
                            bottomSheet.Dismiss();
                        }),
                        new BottomSheetAction(Resource.Drawable.Download, MainActivity.instance.Resources.GetString(Resource.String.download), (sender, eventArg) =>
                        {
                            YoutubeEngine.Download(item.Title, item.YoutubeID);
                            bottomSheet.Dismiss();
                        })
                    });
                }
                else
                {
                    actions.Add(new BottomSheetAction(Resource.Drawable.Edit, MainActivity.instance.Resources.GetString(Resource.String.edit_metadata), (sender, eventArg) =>
                    {
                        Browse.EditMetadata(item);
                        bottomSheet.Dismiss();
                    }));
                }

                bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
                bottomSheet.Show();
            }
            else
            {
                BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
                View bottomView = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
                bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Title;
                bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Artist;
                if (item.Album == null)
                {
                    var songCover = Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, item.AlbumArt);

                    Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
                }
                else
                {
                    Picasso.With(MainActivity.instance).Load(item.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
                }
                bottomSheet.SetContentView(bottomView);

                List<BottomSheetAction> actions = new List<BottomSheetAction>
                {
                    new BottomSheetAction(Resource.Drawable.Play, MainActivity.instance.Resources.GetString(Resource.String.play), (sender, eventArg) => 
                    {
                        if (!item.IsYt)
                            Browse.Play(item);
                        else
                            YoutubeEngine.Play(item.YoutubeID, item.Title, item.Artist, item.Album);
                        bottomSheet.Dismiss();
                    }),
                    new BottomSheetAction(Resource.Drawable.PlaylistPlay, MainActivity.instance.Resources.GetString(Resource.String.play_next), (sender, eventArg) =>
                    {
                        if (!item.IsYt)
                            Browse.PlayNext(item);
                        else
                            YoutubeEngine.PlayNext(item.YoutubeID, item.Title, item.Artist, item.Album);
                        bottomSheet.Dismiss();
                    }),
                    new BottomSheetAction(Resource.Drawable.Queue, MainActivity.instance.Resources.GetString(Resource.String.play_last), (sender, eventArg) =>
                    {
                        if (!item.IsYt)
                            Browse.PlayLast(item);
                        else
                            YoutubeEngine.PlayLast(item.YoutubeID, item.Title, item.Artist, item.Album);
                        bottomSheet.Dismiss();
                    }),
                    new BottomSheetAction(Resource.Drawable.PlaylistAdd, MainActivity.instance.Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) => { Browse.GetPlaylist(item); bottomSheet.Dismiss(); })
                };

                if (!item.IsYt)
                {
                    actions.Add(new BottomSheetAction(Resource.Drawable.Edit, MainActivity.instance.Resources.GetString(Resource.String.edit_metadata), (sender, eventArg) =>
                    {
                        Browse.EditMetadata(item);
                        bottomSheet.Dismiss();
                    }));
                }
                else
                {
                    actions.AddRange(new BottomSheetAction[]
                    {
                        new BottomSheetAction(Resource.Drawable.PlayCircle, MainActivity.instance.Resources.GetString(Resource.String.create_mix_from_song), (sender, eventArg) =>
                        {
                            YoutubeEngine.CreateMix(item);
                            bottomSheet.Dismiss();
                        }),
                        new BottomSheetAction(Resource.Drawable.Download, MainActivity.instance.Resources.GetString(Resource.String.download), (sender, eventArg) =>
                        {
                            YoutubeEngine.Download(item.Title, item.YoutubeID);
                            bottomSheet.Dismiss();
                        })
                    });
                }

                bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
                bottomSheet.Show();
            }
        }
    }
}
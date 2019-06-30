using Android.App;
using Android.Content;
using Android.Net;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Opus.Api;
using Opus.Api.Services;
using Opus.DataStructure;
using Opus.Others;
using Square.Picasso;
using System.Collections.Generic;

namespace Opus.Adapter
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
                holder.text.Text = UseQueue ? MainActivity.instance.GetString(Resource.String.empty_queue) : MainActivity.instance.GetString(Resource.String.long_loading);
                holder.text.SetHeight(MainActivity.instance.DpToPx(157));
                return;
            }
            else
            {
                SongHolder holder = (SongHolder)viewHolder;

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
                    if(song.Album != null)
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

        void OnClick(int position)
        {
            if (UseQueue)
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
            else
                SongManager.Play(songList[position]);
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
                if (item.AlbumArt == -1 || item.IsYt)
                {
                    Picasso.With(MainActivity.instance).Load(item.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
                }
                else
                {
                    var songCover = Uri.Parse("content://media/external/audio/albumart");
                    var songAlbumArtUri = ContentUris.WithAppendedId(songCover, item.AlbumArt);

                    Picasso.With(MainActivity.instance).Load(songAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
                }
                bottomSheet.SetContentView(bottomView);

                List<BottomSheetAction> actions = new List<BottomSheetAction>
                {
                    new BottomSheetAction(Resource.Drawable.Play, MainActivity.instance.Resources.GetString(Resource.String.play), (sender, eventArg) => { OnClick(position); bottomSheet.Dismiss(); }),
                    new BottomSheetAction(Resource.Drawable.Close, MainActivity.instance.Resources.GetString(Resource.String.remove_from_queue), (sender, eventArg) => { MusicPlayer.RemoveFromQueue(position); bottomSheet.Dismiss(); }),
                    new BottomSheetAction(Resource.Drawable.PlaylistAdd, MainActivity.instance.Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) => { PlaylistManager.AddSongToPlaylistDialog(item); bottomSheet.Dismiss(); })
                };

                if (item.IsYt)
                {
                    actions.AddRange(new BottomSheetAction[]
                    {
                        new BottomSheetAction(Resource.Drawable.PlayCircle, MainActivity.instance.Resources.GetString(Resource.String.create_mix_from_song), (sender, eventArg) =>
                        {
                            YoutubeManager.CreateMixFromSong(item);
                            bottomSheet.Dismiss();
                        }),
                        new BottomSheetAction(Resource.Drawable.Download, MainActivity.instance.Resources.GetString(Resource.String.download), (sender, eventArg) =>
                        {
                            YoutubeManager.Download(new[] { item });
                            bottomSheet.Dismiss();
                        })
                    });

                    if(item.ChannelID != null)
                    {
                        actions.Add(new BottomSheetAction(Resource.Drawable.account, MainActivity.instance.Resources.GetString(Resource.String.goto_channel), (sender, eventArg) =>
                        {
                            ChannelManager.OpenChannelTab(item.ChannelID);
                            bottomSheet.Dismiss();
                        }));
                    }
                }
                else
                {
                    actions.Add(new BottomSheetAction(Resource.Drawable.Edit, MainActivity.instance.Resources.GetString(Resource.String.edit_metadata), (sender, eventArg) =>
                    {
                        LocalManager.EditMetadata(item);
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
                if (item.AlbumArt == -1 || item.IsYt)
                {
                    Picasso.With(MainActivity.instance).Load(item.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
                }
                else
                {
                    var songCover = Uri.Parse("content://media/external/audio/albumart");
                    var songAlbumArtUri = ContentUris.WithAppendedId(songCover, item.AlbumArt);

                    Picasso.With(MainActivity.instance).Load(songAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
                }
                bottomSheet.SetContentView(bottomView);

                List<BottomSheetAction> actions = new List<BottomSheetAction>
                {
                    new BottomSheetAction(Resource.Drawable.Play, MainActivity.instance.Resources.GetString(Resource.String.play), (sender, eventArg) => 
                    {
                        SongManager.Play(item);
                        bottomSheet.Dismiss();
                    }),
                    new BottomSheetAction(Resource.Drawable.PlaylistPlay, MainActivity.instance.Resources.GetString(Resource.String.play_next), (sender, eventArg) =>
                    {
                        SongManager.PlayNext(item);
                        bottomSheet.Dismiss();
                    }),
                    new BottomSheetAction(Resource.Drawable.Queue, MainActivity.instance.Resources.GetString(Resource.String.play_last), (sender, eventArg) =>
                    {
                        SongManager.PlayLast(item);
                        bottomSheet.Dismiss();
                    }),
                    new BottomSheetAction(Resource.Drawable.PlaylistAdd, MainActivity.instance.Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) => 
                    {
                        PlaylistManager.AddSongToPlaylistDialog(item);
                        bottomSheet.Dismiss();
                    })
                };

                if (!item.IsYt)
                {
                    actions.Add(new BottomSheetAction(Resource.Drawable.Edit, MainActivity.instance.Resources.GetString(Resource.String.edit_metadata), (sender, eventArg) =>
                    {
                        LocalManager.EditMetadata(item);
                        bottomSheet.Dismiss();
                    }));
                }
                else
                {
                    actions.AddRange(new BottomSheetAction[]
                    {
                        new BottomSheetAction(Resource.Drawable.PlayCircle, MainActivity.instance.Resources.GetString(Resource.String.create_mix_from_song), (sender, eventArg) =>
                        {
                            YoutubeManager.CreateMixFromSong(item);
                            bottomSheet.Dismiss();
                        }),
                        new BottomSheetAction(Resource.Drawable.Download, MainActivity.instance.Resources.GetString(Resource.String.download), (sender, eventArg) =>
                        {
                            YoutubeManager.Download(new[] { item });
                            bottomSheet.Dismiss();
                        })
                    });

                    if (item.ChannelID != null)
                    {
                        actions.Add(new BottomSheetAction(Resource.Drawable.account, MainActivity.instance.Resources.GetString(Resource.String.goto_channel), (sender, eventArg) =>
                        {
                            ChannelManager.OpenChannelTab(item.ChannelID);
                            bottomSheet.Dismiss();
                        }));
                    }
                }

                bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
                bottomSheet.Show();
            }
        }
    }
}
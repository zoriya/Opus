using Android.App;
using Android.Content;
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
        private List<Song> songList;

        private readonly string[] actions = new string[] { "Play", "Play Next", "Play Last", "Add To Playlist" };
        public override int ItemCount => songList.Count;

        public LineAdapter(List<Song> songList, RecyclerView recycler)
        {
            this.recycler = recycler;
            this.songList = songList;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            RecyclerHolder holder = (RecyclerHolder)viewHolder;

            holder.Title.Text = songList[position].Name;

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
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.LineSong, parent, false);
            return new RecyclerHolder(itemView, OnClick, OnLongClick);
        }

        void OnClick(int position)
        {
            if (!songList[position].IsYt)
                Browse.Play(songList[position], recycler.GetLayoutManager().FindViewByPosition(position).FindViewById<ImageView>(Resource.Id.albumArt));
            else
                YoutubeEngine.Play(songList[position].youtubeID, songList[position].Name, songList[position].Artist, songList[position].Album);
        }

        void OnLongClick(int position)
        {
            Song item = songList[position];

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
                            YoutubeEngine.Play(item.youtubeID, item.Name, item.Artist, item.Album);
                        break;

                    case 1:
                        if (!item.IsYt)
                            Browse.PlayNext(item);
                        else
                            YoutubeEngine.PlayNext(item.Path, item.Name, item.Artist, item.Album);
                        break;

                    case 2:
                        if (!item.IsYt)
                            Browse.PlayLast(item);
                        else
                            YoutubeEngine.PlayLast(item.Path, item.Name, item.Artist, item.Album);
                        break;

                    case 3:
                        if (item.IsYt)
                            YoutubeEngine.GetPlaylists(item.Path, MainActivity.instance);
                        else
                            Browse.GetPlaylist(item);
                        break;

                    case 5:
                        if (item.IsYt)
                            YoutubeEngine.Download(item.Name, item.Path);
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
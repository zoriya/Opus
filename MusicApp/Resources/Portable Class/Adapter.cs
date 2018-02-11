using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class Adapter : ArrayAdapter
    {
        private Context context;
        private List<Song> songList;
        private LayoutInflater inflater;
        private int resource;

        public Adapter(Context context, int resource, List<Song> songList) : base(context, resource, songList)
        {
            this.context = context;
            this.resource = resource;
            this.songList = songList;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (position > songList.Count || position < 0)
                return convertView;

            if (convertView != null)
                convertView.FindViewById<ImageView>(Resource.Id.moreButton).Click -= MoreClick;

            if (inflater == null)
            {
                inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            }
            if (convertView == null)
            {
                convertView = inflater.Inflate(resource, parent, false);
            }
            Holder holder = new Holder(convertView)
            {
                Title = { Text = songList[position].GetName() },
                Artist = { Text = songList[position].GetArtist() },
            };
            if(songList[position].GetAlbumArt() == -1 || songList[position].IsYt)
            {
                var songAlbumArtUri = Android.Net.Uri.Parse(songList[position].GetAlbum()); 
                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
            }
            else
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, songList[position].GetAlbumArt());

                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
            }

            if (!holder.more.HasOnClickListeners)
            {
                holder.more.Tag = position;
                holder.more.Click += MoreClick;
            }

            return convertView;
        }

        private void MoreClick(object sender, EventArgs e)
        {
            int position = (int)((ImageView)sender).Tag;
            Queue.instance?.More(songList[position]);
            Browse.instance?.More(songList[position]);
            YoutubeEngine.instance?.More(songList[position]);
            YtPlaylist.instance?.More(position);
            PlaylistTracks.instance?.More(songList[position], position);
            FolderTracks.instance?.More(songList[position]);
        }
    }
}
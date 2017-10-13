using System.Collections.Generic;

using Android.Content;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Android.Graphics;
using Android.Util;
using System.IO;
using Android.OS;
using Android.App;
using Square.Picasso;

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
            if(songList[position].GetAlbumArt() == -1)
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

            convertView.SetBackgroundColor(Color.White);
            //if (position % 2 == 0)
            //{
            //    convertView.SetBackgroundColor(Color.LightGreen);
            //}
            return convertView;
        }
    }
}
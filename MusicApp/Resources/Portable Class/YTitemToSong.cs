using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using YoutubeSearch;

namespace MusicApp.Resources.Portable_Class
{
    public class YTitemToSong
    {
        public YTitemToSong(VideoInformation item, out Song song)
        {
            string title = item.Title;
            string artist = item.Author;
            string album = item.Thumbnail;
            long albumArt = -1;
            long id = 1;
            string path = item.Url;

            song = new Song(title, artist, album, albumArt, id, path);
        }
    }
}
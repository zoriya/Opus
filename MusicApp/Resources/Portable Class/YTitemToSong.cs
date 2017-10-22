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
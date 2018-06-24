using System;

namespace MusicApp.Resources.values
{
    [Serializable]
    public class Song
    {
        private string Name;
        private string Artist;
        private string Album;
        private readonly long AlbumArt;
        public string youtubeID;
        private readonly long id;
        private string path;
        public bool isParsed;
        public int queueSlot;

        public string GetName() { return Name; }
        public string GetArtist() { return Artist; }
        public string GetAlbum() { return Album; }
        public long GetAlbumArt() { return AlbumArt; }
        public long GetID() { return id; }
        public string GetPath() { return path; }
        public bool IsYt { get; }

        public Song(string Name, string Artist, string Album, string youtubeID, long AlbumArt, long id, string path, bool isYT = false, bool isParsed = true, int queueSlot = -1)
        {
            this.Name = Name;
            this.Artist = Artist;
            this.Album = Album;
            this.youtubeID = youtubeID;
            this.AlbumArt = AlbumArt;
            this.id = id;
            this.path = path;
            IsYt = isYT;
            this.isParsed = isParsed;
            this.queueSlot = queueSlot;
        }

        public void SetName(string Name) { this.Name = Name; }
        public void SetArtist(string Artist) { this.Artist = Artist; }
        public void SetAlbum(string Album) { this.Album = Album; }
        public void SetPath(string path) { this.path = path; }

        public override string ToString()
        {
            return Name + " Artist: " + Artist + " Album: " + Album + " youtubeID: " + youtubeID + " AlbumArt: " + AlbumArt + " Id: " + id + " Path: " + path + " isYT: " + IsYt + " isParsed: " + isParsed + " queueSlot: " + queueSlot;
        }

        public static explicit operator Song(string v)
        {
            if (v == null)
                return null;

            string Name = v.Substring(0, v.IndexOf(" Artist: "));
            string Artist = v.Substring(v.IndexOf(" Artist: ") + 9, v.IndexOf(" Album: ") - Name.Length - 8);
            string Album = v.Substring(v.IndexOf(" Album: ") + 8, v.IndexOf(" youtubeID: ") - v.IndexOf(" Album: ") - 8);
            string youtubeID = v.Substring(v.IndexOf(" youtubeID: ") + 12, v.IndexOf(" AlbumArt: ") - v.IndexOf(" youtubeID: ") - 12);
            long AlbumArt = long.Parse(v.Substring(v.IndexOf(" AlbumArt: ") + 11, v.IndexOf(" Id: ") - v.IndexOf(" AlbumArt: ") - 11));
            long id = long.Parse(v.Substring(v.IndexOf(" Id: ") + 5, v.IndexOf(" Path: ") - v.IndexOf(" Id: ") - 5));
            string path = v.Substring(v.IndexOf(" Path: ") + 7, v.IndexOf(" isYT: ") - v.IndexOf(" Path: ") - 7);
            bool isYT = bool.Parse(v.Substring(v.IndexOf(" isYT: ") + 7, v.IndexOf(" isParsed: ") - v.IndexOf(" isYT: ") - 7));
            bool isParsed = bool.Parse(v.Substring(v.IndexOf(" isParsed: ") + 11, v.IndexOf(" queueSlot: ") - v.IndexOf(" isParsed: ") - 11));
            int queueSlot = int.Parse(v.Substring(v.IndexOf(" queueSlot: ") + 12, v.Length - v.IndexOf(" queueSlot: ") - 12));

            Song song = new Song(Name, Artist, Album, youtubeID, AlbumArt, id, path, isYT, isParsed, queueSlot);
            return song;
        }
    }
}
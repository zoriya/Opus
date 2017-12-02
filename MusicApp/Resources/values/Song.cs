namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class Song
    {
        private string Name;
        private string Artist;
        private string Album;
        private long AlbumArt;
        private long id;
        private string path;
        private bool isYT;

        public string GetName() { return Name; }
        public string GetArtist() { return Artist; }
        public string GetAlbum() { return Album; }
        public long GetAlbumArt() { return AlbumArt; }
        public long GetID() { return id; }
        public string GetPath() { return path; }
        public bool IsYt { get { return isYT; } }
        
        public Song(string Name, string Artist, string Album, long AlbumArt, long id, string path, bool isYT = false)
        {
            this.Name = Name;
            this.Artist = Artist;
            this.Album = Album;
            this.AlbumArt = AlbumArt;
            this.id = id;
            this.path = path;
            this.isYT = isYT;
        }

        public void SetName(string Name) { this.Name = Name; }
    }
}
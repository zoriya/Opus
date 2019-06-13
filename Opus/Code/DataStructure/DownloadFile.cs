namespace Opus.DataStructure
{
    [System.Serializable]
    public class DownloadFile
    {
        public string Name;
        public string YoutubeID;
        public string PlaylistName;
        public DownloadState State = DownloadState.None;
        public int progress = 0;
        public string Path;
        public bool skipCheck = false;

        public DownloadFile(string name, string videoID, string playlist)
        {
            this.Name = name;
            this.YoutubeID = videoID;
            this.PlaylistName = playlist;
        }

        public static DownloadFile From(Song song, string playlistName)
        {
            return new DownloadFile(song.Title, song.YoutubeID, playlistName);
        }
    }

    public enum DownloadState
    {
        Initialization,
        Downloading,
        MetaData,
        Completed,
        Canceled,
        UpToDate,
        None,
        Error,
        Playlist
    }
}
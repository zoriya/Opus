namespace Opus.DataStructure
{
    [System.Serializable]
    public class DownloadFile
    {
        public string name;
        public string videoID;
        public string playlist;
        public DownloadState State = DownloadState.None;
        public int progress = 0;
        public string path;
        public bool skipCheck = false;

        public DownloadFile(string name, string videoID, string playlist)
        {
            this.name = name;
            this.videoID = videoID;
            this.playlist = playlist;
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
        None
    }
}
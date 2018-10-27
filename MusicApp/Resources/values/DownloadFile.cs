using MusicApp.Resources.Portable_Class;

namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class DownloadFile
    {
        public string name;
        public string videoID;
        public string playlist;
        public DownloadState State = DownloadState.None;
        public bool skipCheck = false;

        public DownloadFile(string name, string videoID, string playlist)
        {
            this.name = name;
            this.videoID = videoID;
            this.playlist = playlist;
        }
    }
}
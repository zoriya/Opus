namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class DownloadFile
    {
        public string name;
        public string videoID;

        public DownloadFile(string name, string videoID)
        {
            this.name = name;
            this.videoID = videoID;
        }
    }
}
namespace MusicApp.Resources.Portable_Class
{
    [System.Serializable]
    public class YtFile
    {
        public string Title;
        public string channelTitle;
        public string Id;
        public string thumbnailUrl;

        public YtFile(string title, string channelTitle, string id, string thumbnailUrl)
        {
            Title = title;
            this.channelTitle = channelTitle;
            Id = id;
            this.thumbnailUrl = thumbnailUrl;
        }
    }
}
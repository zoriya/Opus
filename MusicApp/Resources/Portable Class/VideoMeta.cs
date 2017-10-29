namespace MusicApp.Resources.Portable_Class
{
    public class VideoMeta
    {
        private const string IMAGE_BASE_URL = "http://i.ytimg.com/vi/";

        public string videoID;
        public string title;
        public string author;
        public object channelId;
        public object length;
        public long viewCount;
        public bool isLiveStream;

        public VideoMeta(string videoID, string title, string author, object channelId, object length, long viewCount, bool isLiveStream)
        {
            this.videoID = videoID;
            this.title = title;
            this.author = author;
            this.channelId = channelId;
            this.length = length;
            this.viewCount = viewCount;
            this.isLiveStream = isLiveStream;
        }

        // 120 x 90
        public string GetThumbUrl()
        {
            return IMAGE_BASE_URL + videoID + "/default.jpg";
        }

        // 320 x 180
        public string GetMqImageUrl()
        {
            return IMAGE_BASE_URL + videoID + "/mqdefault.jpg";
        }

        // 480 x 360
        public string GetHqImageUrl()
        {
            return IMAGE_BASE_URL + videoID + "/hqdefault.jpg";
        }

        // 640 x 480
        public string GetSdImageUrl()
        {
            return IMAGE_BASE_URL + videoID + "/sddefault.jpg";
        }

        // Max Res
        public string GetMaxResImageUrl()
        {
            return IMAGE_BASE_URL + videoID + "/maxresdefault.jpg";
        }
    }
}
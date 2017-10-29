namespace MusicApp.Resources.Portable_Class
{
    public class YtFile
    {
        public Format format;
        public string url;

        public YtFile(Format format, string url)
        {
            this.format = format;
            this.url = url;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;

            YtFile ytFile = (YtFile)obj;

            if (format != null ? !format.Equals(ytFile.format) : ytFile.format != null)
                return false;
            return url != null ? url.Equals(ytFile.url) : ytFile.url == null;
        }

        public override int GetHashCode()
        {
            int result = format != null ? format.GetHashCode() : 0;
            result = 31 * result + (url != null ? url.GetHashCode() : 0);
            return result;
        }

        public override string ToString()
        {
            return "YtFile{" +
                "format=" + format +
                ", url='" + url + '\'' +
                '}';
        }
    }
}
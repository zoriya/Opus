namespace MusicApp.Resources.Portable_Class
{
    [System.Serializable]
    public class Format
    {
        public enum VCodec
        {
            H263, H264, MPEG4, VP8, VP9, NONE
        }

        public enum ACodec
        {
            MP3, AAC, VORBIS, OPUS, NONE
        }

        private int itag;
        private string ext;
        public int height;
        private int fps;
        private VCodec vCodec;
        private ACodec aCodec;
        private int audioBitrate;
        public bool isDashContainer;
        public bool isHlsContent;

        public Format(int itag, string ext, int height, VCodec vCodec, ACodec aCodec, bool isDashContainer)
        {
            this.itag = itag;
            this.ext = ext;
            this.height = height;
            this.fps = 30;
            this.audioBitrate = -1;
            this.isDashContainer = isDashContainer;
            this.isHlsContent = false;
        }

        public Format(int itag, string ext, VCodec vCodec, ACodec aCodec, int audioBitrate, bool isDashContainer)
        {
            this.itag = itag;
            this.ext = ext;
            this.height = -1;
            this.fps = 30;
            this.audioBitrate = audioBitrate;
            this.isDashContainer = isDashContainer;
            this.isHlsContent = false;
        }

        public Format(int itag, string ext, int height, VCodec vCodec, ACodec aCodec, int audioBitrate, bool isDashContainer)
        {
            this.itag = itag;
            this.ext = ext;
            this.height = height;
            this.fps = 30;
            this.audioBitrate = audioBitrate;
            this.isDashContainer = isDashContainer;
            this.isHlsContent = false;
        }

        public Format(int itag, string ext, int height, VCodec vCodec, ACodec aCodec, int audioBitrate, bool isDashContainer, bool isHlsContent)
        {
            this.itag = itag;
            this.ext = ext;
            this.height = height;
            this.fps = 30;
            this.audioBitrate = audioBitrate;
            this.isDashContainer = isDashContainer;
            this.isHlsContent = isHlsContent;
        }

        public Format(int itag, string ext, int height, VCodec vCodec, int fps, ACodec aCodec, bool isDashContainer)
        {
            this.itag = itag;
            this.ext = ext;
            this.height = height;
            this.audioBitrate = -1;
            this.fps = fps;
            this.isDashContainer = isDashContainer;
            this.isHlsContent = false;
        }

        public int GetFPS()
        {
            return fps;
        }

        public int GetAudioBitrate()
        {
            return audioBitrate;
        }

        public int GetItag()
        {
            return itag;
        }

        public string GetExt()
        {
            return ext;
        }

        public ACodec GetAudioCodec()
        {
            return aCodec;
        }

        public VCodec GetVideoCodec()
        {
            return vCodec;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || obj.GetType() != GetType())
                return false;

            Format format = (Format)obj;

            if (itag != format.itag)
                return false;
            if (height != format.height)
                return false;
            if (fps != format.fps)
                return false;
            if (audioBitrate != format.audioBitrate)
                return false;
            if (isDashContainer != format.isDashContainer)
                return false;
            if (isHlsContent != format.isHlsContent)
                return false;
            if (ext != null ? !ext.Equals(format.ext) : format.ext != null)
                return false;
            if (vCodec != format.vCodec)
                return false;
            return aCodec == format.aCodec;
        }

        public override int GetHashCode()
        {
            int result = itag;
            result = 31 * result + (ext != null ? ext.GetHashCode() : 0);
            result = 31 * result + height;
            result = 31 * result + fps;
            result = 31 * result + vCodec.GetHashCode();
            result = 31 * result + aCodec.GetHashCode();
            result = 31 * result + audioBitrate;
            result = 31 * result + (isDashContainer ? 1 : 0);
            result = 31 * result + (isHlsContent ? 1 : 0);
            return result;
        }

        public override string ToString()
        {
            return "Format{" +
               "itag=" + itag +
               ", ext='" + ext + '\'' +
               ", height=" + height +
               ", fps=" + fps +
               ", vCodec=" + vCodec +
               ", aCodec=" + aCodec +
               ", audioBitrate=" + audioBitrate +
               ", isDashContainer=" + isDashContainer +
               ", isHlsContent=" + isHlsContent +
               '}';
        }
    }
}
using System.Collections.Generic;

namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class HomeSection
    {
        public string SectionTitle;
        public SectionType contentType;
        public List<Song> contentValue;
        public string data;

        public HomeSection(string sectionTitle, SectionType contentType, List<Song> contentValue)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
            this.contentValue = contentValue;
        }
    }

    public enum SectionType
    {
        None,
        ChannelList,
        PlaylistList,
        SinglePlaylist
    };
}
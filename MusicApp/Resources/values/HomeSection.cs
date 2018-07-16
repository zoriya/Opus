using System.Collections.Generic;

namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class HomeSection
    {
        public string SectionTitle;
        public SectionType contentType;
        public List<Song> contentValue;
        public int maxItems;

        public HomeSection(string sectionTitle, SectionType contentType, List<Song> contentValue, int maxItems)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
            this.contentValue = contentValue;
            this.maxItems = maxItems;
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
namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class Folder : Java.Lang.Object
    {
        public string name;
        public string uri;
        public bool asChild = false;
        public bool isExtended = false;
        public int childCount = 0;
        public int Padding = 0;

        public Folder(string name, string uri, bool asChild)
        {
            this.name = name;
            this.uri = uri;
            this.asChild = asChild;
        }
    }
}
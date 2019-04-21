using Android.Views;
using Android.Widget;

namespace Opus.DataStructure
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

    public class FolderHolder
    {
        public ImageView expandChild;
        public TextView Name;
        public RadioButton used;

        public FolderHolder(View v)
        {
            expandChild = v.FindViewById<ImageView>(Resource.Id.expendChilds);
            Name = v.FindViewById<TextView>(Resource.Id.folderName);
            used = v.FindViewById<RadioButton>(Resource.Id.folderUsed);
        }
    }
}
using Android.Views;
using Android.Widget;

namespace MusicApp.Resources.values
{
    [System.Serializable]
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
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace MusicApp.Resources.Portable_Class
{
    public class DownloadQueueAdapter : RecyclerView.Adapter
    {
        public DownloadQueueAdapter() { }

        public override int ItemCount => Downloader.queue.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            DownloadHolder holder = (DownloadHolder)viewHolder;
            holder.Title.Text = Downloader.queue[position].name;
            holder.Status.Text = Downloader.queue[position].State.ToString();

            switch (Downloader.queue[position].State)
            {
                case DownloadState.Initialization:
                case DownloadState.Downloading:
                case DownloadState.MetaData:
                    holder.Status.Visibility = ViewStates.Visible;
                    holder.Progress.Visibility = ViewStates.Visible;
                    holder.Progress.Indeterminate = true;
                    holder.Title.Alpha = 1f;
                    break;
                case DownloadState.None:
                    holder.Progress.Visibility = ViewStates.Invisible;
                    holder.Status.Visibility = ViewStates.Gone;
                    holder.Title.Alpha = 1f;
                    break;
                case DownloadState.Completed:
                    holder.Status.Visibility = ViewStates.Gone;
                    holder.Progress.Visibility = ViewStates.Invisible;
                    holder.Title.Alpha = 0.8f;
                    break;
            }

            holder.more.Tag = position;
            if (!holder.more.HasOnClickListeners)
            {
                holder.more.Click += (sender, e) =>
                {
                    int tagPosition = (int)((ImageView)sender).Tag;
                    DownloadQueue.instance.More(tagPosition);
                };
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.DownloadItem, parent, false);
            return new DownloadHolder(itemView);
        }
    }

    public class DownloadHolder : RecyclerView.ViewHolder
    {
        public TextView Title;
        public TextView Status;
        public ImageButton more;
        public ProgressBar Progress;

        public DownloadHolder(View itemView) : base(itemView)
        {
            Title = itemView.FindViewById<TextView>(Resource.Id.fileName);
            Status = itemView.FindViewById<TextView>(Resource.Id.downloadStatus);
            more = itemView.FindViewById<ImageButton>(Resource.Id.more);
            Progress = itemView.FindViewById<ProgressBar>(Resource.Id.progress);
        }
    }
}
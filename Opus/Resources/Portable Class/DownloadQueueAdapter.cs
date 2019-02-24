using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Opus.Resources.Portable_Class
{
    public class DownloadQueueAdapter : RecyclerView.Adapter
    {
        public DownloadQueueAdapter() { }

        public override int ItemCount => Downloader.queue.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            DownloadHolder holder = (DownloadHolder)viewHolder;
            holder.Title.Text = Downloader.queue[position].name;

            switch (Downloader.queue[position].State)
            {
                case DownloadState.Initialization:
                    holder.Status.Text = Downloader.instance.GetString(Resource.String.initialization);
                    holder.Status.Visibility = ViewStates.Visible;
                    holder.Progress.Visibility = ViewStates.Visible;
                    holder.Progress.Indeterminate = true;
                    holder.Title.Alpha = 1f;
                    if (MainActivity.Theme == 1)
                        holder.Title.SetTextColor(Color.White);
                    else
                        holder.Title.SetTextColor(Color.Black);
                    break;
                case DownloadState.MetaData:
                    holder.Status.Text = Downloader.instance.GetString(Resource.String.metadata);
                    holder.Status.Visibility = ViewStates.Visible;
                    holder.Progress.Visibility = ViewStates.Visible;
                    holder.Progress.Indeterminate = true;
                    holder.Title.Alpha = 1f;
                    if (MainActivity.Theme == 1)
                        holder.Title.SetTextColor(Color.White);
                    else
                        holder.Title.SetTextColor(Color.Black);
                    break;
                case DownloadState.Downloading:
                    holder.Status.Text = Downloader.instance.GetString(Resource.String.downloading_status);
                    holder.Status.Visibility = ViewStates.Visible;
                    holder.Progress.Visibility = ViewStates.Visible;
                    holder.Title.Alpha = 1f;
                    holder.Progress.Indeterminate = false;
                    holder.Progress.Progress = Downloader.queue[position].progress;
                    if (MainActivity.Theme == 1)
                        holder.Title.SetTextColor(Color.White);
                    else
                        holder.Title.SetTextColor(Color.Black);
                    break;
                case DownloadState.None:
                    holder.Progress.Visibility = ViewStates.Invisible;
                    holder.Status.Visibility = ViewStates.Gone;
                    holder.Title.Alpha = 1f;
                    if (MainActivity.Theme == 1)
                        holder.Title.SetTextColor(Color.White);
                    else
                        holder.Title.SetTextColor(Color.Black);
                    break;
                case DownloadState.Completed:
                    holder.Status.Text = Downloader.instance.GetString(Resource.String.completed);
                    holder.Status.Visibility = ViewStates.Gone;
                    holder.Progress.Visibility = ViewStates.Invisible;
                    holder.Title.SetTextColor(Color.Argb(255, 117, 117, 117));
                    break;
                case DownloadState.UpToDate:
                    holder.Status.Text = Downloader.instance.GetString(Resource.String.up_to_date_status);
                    holder.Status.Visibility = ViewStates.Visible;
                    holder.Progress.Visibility = ViewStates.Invisible;
                    holder.Title.SetTextColor(Color.Argb(255, 76, 175, 80));
                    break;
                case DownloadState.Canceled:
                    holder.Status.Visibility = ViewStates.Gone;
                    holder.Progress.Visibility = ViewStates.Invisible;
                    holder.Title.SetTextColor(Color.Red);
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

            if (MainActivity.Theme == 1)
                holder.more.SetColorFilter(Color.White);

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
﻿using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using MusicApp.Resources.values;
using Square.Picasso;

namespace MusicApp.Resources.Portable_Class
{
    public class CurrentItemDecoration : RecyclerView.ItemDecoration
    {
        public QueueAdapter adapter;

        public CurrentItemDecoration(QueueAdapter adapter) { this.adapter = adapter; }

        public override void OnDrawOver(Canvas c, RecyclerView parent, RecyclerView.State state)
        {
            base.OnDrawOver(c, parent, state);

            if (adapter.IsSliding)
            {
                parent.SetPadding(0, 0, 0, 0);
                return;
            }


            if (parent.ChildCount > 1 && parent.Width > 0)
            {
                int firstPos = parent.GetChildAdapterPosition(parent.GetChildAt(0));
                int lastPos = parent.GetChildAdapterPosition(parent.GetChildAt(parent.ChildCount - 1));
                int currentPos = MusicPlayer.CurrentID() + 1;

                if (currentPos <= firstPos)
                {
                    View header = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.QueueCurrent, parent, false);
                    int parentWidth = View.MeasureSpec.MakeMeasureSpec(parent.Width, MeasureSpecMode.Exactly);
                    int parentHeight = View.MeasureSpec.MakeMeasureSpec(parent.Height, MeasureSpecMode.Unspecified);
                    int headerWidth = ViewGroup.GetChildMeasureSpec(parentWidth, 0, header.LayoutParameters.Width);
                    int headerHeight = ViewGroup.GetChildMeasureSpec(parentHeight, 0, header.LayoutParameters.Height);

                    header.Measure(headerWidth, headerHeight);
                    header.Layout(0, 0, header.MeasuredWidth, header.MeasuredHeight);

                    header.FindViewById(Resource.Id.topDivider).Visibility = ViewStates.Gone;
                    BindHolder(new RecyclerHolder(header, null, null));
                    Queue.instance.HeaderHeight = header.MeasuredHeight;

                    c.Save();
                    c.Translate(0, 0);
                    header.Draw(c);
                    c.Restore();
                    parent.SetPadding(0, header.MeasuredHeight, 0, 0);
                }
                else if (currentPos >= lastPos)
                {
                    View header = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.QueueCurrent, parent, false);
                    int parentWidth = View.MeasureSpec.MakeMeasureSpec(parent.Width, MeasureSpecMode.Exactly);
                    int parentHeight = View.MeasureSpec.MakeMeasureSpec(parent.Height, MeasureSpecMode.Unspecified);
                    int headerWidth = ViewGroup.GetChildMeasureSpec(parentWidth, 0, header.LayoutParameters.Width);
                    int headerHeight = ViewGroup.GetChildMeasureSpec(parentHeight, 0, header.LayoutParameters.Height);

                    header.Measure(headerWidth, headerHeight);
                    header.Layout(0, 0, header.MeasuredWidth, header.MeasuredHeight);

                    header.FindViewById(Resource.Id.bottomDivider).Visibility = ViewStates.Gone;
                    BindHolder(new RecyclerHolder(header, null, null));
                    Queue.instance.HeaderHeight = -header.MeasuredHeight;

                    c.Save();
                    c.Translate(0, parentHeight - header.MeasuredHeight);
                    header.Draw(c);
                    c.Restore();
                    parent.SetPadding(0, 0, 0, header.MeasuredHeight);
                }
                else
                {
                    Queue.instance.HeaderHeight = 0;
                    parent.SetPadding(0, 0, 0, 0);
                }
            }

            void BindHolder(RecyclerHolder holder)
            {
                Song current = MusicPlayer.queue[MusicPlayer.CurrentID()];

                holder.more.SetColorFilter(Color.White);
                holder.youtubeIcon.SetColorFilter(Color.White);
                holder.status.SetTextColor(MusicPlayer.isRunning ? Color.Argb(255, 244, 81, 30) : Color.Argb(255, 66, 165, 245));
                string status = MusicPlayer.isRunning ? Queue.instance.GetString(Resource.String.playing) : Queue.instance.GetString(Resource.String.paused);
                SpannableString statusText = new SpannableString(status);
                statusText.SetSpan(new BackgroundColorSpan(Color.ParseColor("#8C000000")), 0, status.Length, SpanTypes.InclusiveInclusive);
                holder.status.TextFormatted = statusText;

                SpannableString titleText = new SpannableString(current.Title);
                titleText.SetSpan(new BackgroundColorSpan(Color.ParseColor("#8C000000")), 0, current.Title.Length, SpanTypes.InclusiveInclusive);
                holder.Title.TextFormatted = titleText;

                if (current.AlbumArt == -1 || current.IsYt)
                {
                    var songAlbumArtUri = Android.Net.Uri.Parse(current.Album);
                    Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);
                }
                else
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var songAlbumArtUri = ContentUris.WithAppendedId(songCover, current.AlbumArt);

                    Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Color.background_material_dark).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
                }

                if (current.IsLiveStream)
                    holder.Live.Visibility = ViewStates.Visible;
                else
                    holder.Live.Visibility = ViewStates.Gone;

                if (current.IsParsed != true && current.IsYt)
                {
                    holder.youtubeIcon.SetImageResource(Resource.Drawable.needProcessing);
                    holder.youtubeIcon.Visibility = ViewStates.Visible;
                }
                else if (current.IsParsed == true && current.IsYt)
                {
                    holder.youtubeIcon.SetImageResource(Resource.Drawable.PublicIcon);
                    holder.youtubeIcon.Visibility = ViewStates.Visible;
                }
                else
                {
                    holder.youtubeIcon.Visibility = ViewStates.Gone;
                }
            }
        }
    }
}
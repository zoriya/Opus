using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Opus.Api;
using Opus.DataStructure;
using Opus.Fragments;
using Opus.Others;
using Square.Picasso;
using System.Collections.Generic;

namespace Opus.Adapter
{
    public class VerticalSectionAdapter : RecyclerView.Adapter
    {
        public RecyclerView recycler;
        public List<Channel> channels;
        public List<PlaylistItem> playlists;
        private readonly bool UseChannel;
        public bool expanded = false;

        public override int ItemCount
        {
            get
            {
                if ((UseChannel && channels == null) || (!UseChannel && playlists == null))
                    return 1;

                return UseChannel? (expanded? (channels.Count > 3 ? 3 : channels.Count) : channels.Count) : 
                expanded? (playlists.Count > 3 ? 3 : playlists.Count) : playlists.Count;
            }
        }

        public VerticalSectionAdapter(List<Channel> channels, RecyclerView recycler)
        {
            this.recycler = recycler;
            this.channels = channels;
            UseChannel = true;
        }

        public VerticalSectionAdapter(List<PlaylistItem> playlists, RecyclerView recycler)
        {
            this.recycler = recycler;
            this.playlists = playlists;
            UseChannel = false;
        }

        public VerticalSectionAdapter(bool UseChannel, RecyclerView recycler)
        {
            this.recycler = recycler;
            this.UseChannel = UseChannel;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (position == 0 && ((UseChannel && channels == null) || (!UseChannel && playlists == null)))
            {
                EmptyHolder holder = (EmptyHolder)viewHolder;
                holder.text.Text = MainActivity.instance.GetString(Resource.String.long_loading);
                holder.text.SetHeight(MainActivity.instance.DpToPx(157));
            }
            else
            {
                SongHolder holder = (SongHolder)viewHolder;

                if (UseChannel)
                {
                    holder.Title.Text = channels[position].Name;
                    Picasso.With(Application.Context).Load(channels[position].ImageURL).Placeholder(Resource.Color.background_material_dark).Transform(new CircleTransformation()).Into(holder.AlbumArt);
                    holder.ItemView.SetPadding(4, 1, 4, 1);
                }
                else
                {
                    holder.Title.Text = playlists[position].Name;
                    Picasso.With(Application.Context).Load(playlists[position].ImageURL).Placeholder(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(holder.AlbumArt);
                    if (!holder.RightButtons.FindViewById<ImageButton>(Resource.Id.play).HasOnClickListeners)
                    {
                        holder.RightButtons.FindViewById<ImageButton>(Resource.Id.play).Click += (sender, e) => { PlaylistManager.PlayInOrder(playlists[position]); };
                        holder.RightButtons.FindViewById<ImageButton>(Resource.Id.shuffle).Click += (sender, e) => { PlaylistManager.Shuffle(playlists[position]); };
                    }
                }
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if ((UseChannel && channels == null) || (!UseChannel && playlists == null))
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.EmptyView, parent, false);
                return new EmptyHolder(itemView);
            }
            else if (UseChannel)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.HomeChannel, parent, false);
                return new SongHolder(itemView, OnClick, OnLongClick);
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.HomePlaylist, parent, false);
                return new SongHolder(itemView, OnClick, OnLongClick);
            }
        }

        void OnClick(int position)
        {
            if(UseChannel)
                MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, ChannelDetails.NewInstance(channels[position])).AddToBackStack("Channel Details").Commit();
            else
                MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlists[position])).AddToBackStack("Playlist Track").Commit();
        }

        void OnLongClick(int position) { }
    }
}
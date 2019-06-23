using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Opus.Adapter;
using Opus.Api;
using Opus.DataStructure;
using Opus.Others;
using Square.Picasso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Channel = Opus.DataStructure.Channel;
using PlaylistItem = Opus.DataStructure.PlaylistItem;
using SearchView = Android.Support.V7.Widget.SearchView;

namespace Opus.Fragments
{
    public class YoutubeSearch : Fragment
    {
        public static YoutubeSearch[] instances;
        public string Query;
        private string nextPageToken = null;
        public string querryType;

        public bool IsFocused = false;
        public RecyclerView ListView;
        public List<YtFile> result;

        private YtAdapter adapter;
        public TextView EmptyView;
        public ProgressBar LoadingView;
        private bool searching;


        public YoutubeSearch(string Query, string querryType)
        {
            this.Query = Query;
            this.querryType = querryType;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
        }

        private async void OnRefresh(object sender, EventArgs e)
        {
            if (IsFocused)
            {
                await Search(Query, querryType, false);
                MainActivity.instance.contentRefresh.Refreshing = false;
            }
        }

        private void OnScroll(object sender, View.ScrollChangeEventArgs e)
        {
            if (((LinearLayoutManager)ListView.GetLayoutManager()).FindLastVisibleItemPosition() == result.Count - 1)
                LoadMore();
        }

        async void LoadMore()
        {
            if(nextPageToken != null && !searching)
            {
                try
                {
                    searching = true;
                    SearchResource.ListRequest searchResult = YoutubeManager.YoutubeService.Search.List("snippet");
                    searchResult.Q = Query;
                    searchResult.PageToken = nextPageToken;
                    searchResult.TopicId = "/m/04rlf";
                    switch (querryType)
                    {
                        case "All":
                            searchResult.Type = "video,channel,playlist";
                            searchResult.EventType = null;
                            break;
                        case "Tracks":
                            searchResult.Type = "video";
                            searchResult.EventType = null;
                            break;
                        case "Playlists":
                            searchResult.Type = "playlist";
                            searchResult.EventType = null;
                            break;
                        case "Lives":
                            searchResult.Type = "video";
                            searchResult.EventType = SearchResource.ListRequest.EventTypeEnum.Live;
                            break;
                        case "Channels":
                            searchResult.Type = "channel";
                            searchResult.EventType = null;
                            break;
                        default:
                            searchResult.Type = "video";
                            searchResult.EventType = null;
                            break;
                    }
                    searchResult.MaxResults = 50;

                    var searchReponse = await searchResult.ExecuteAsync();
                    nextPageToken = searchReponse.NextPageToken;

                    int loadPos = result.Count - 1;
                    result.RemoveAt(loadPos);
                    adapter.NotifyItemRemoved(loadPos);

                    foreach (var video in searchReponse.Items)
                    {
                        result.Add(GetYtFileFromSearchResult(video));
                    }

                    if (nextPageToken != null)
                        result.Add(new YtFile(new Song(), YtKind.Loading));

                    adapter.NotifyItemRangeInserted(loadPos, result.Count - loadPos);
                    searching = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("&Exception catched in the youtube load more (search tab): " + ex.Source + " - " + ex.Message);
                    if (ex is System.Net.Http.HttpRequestException)
                        MainActivity.instance.Timout();
                }
            }
        }

        public void OnFocus() { }
        public void OnUnfocus() { }

        public static Fragment[] NewInstances(string searchQuery)
        {
            instances = new YoutubeSearch[]
            {
                new YoutubeSearch(searchQuery, "All"),
                new YoutubeSearch(searchQuery, "Tracks"),
                new YoutubeSearch(searchQuery, "Playlists"),
                new YoutubeSearch(searchQuery, "Lives"),
                new YoutubeSearch(searchQuery, "Channels")
            };
            return instances;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.CompleteRecycler, container, false);
            EmptyView = view.FindViewById<TextView>(Resource.Id.empty);
            LoadingView = view.FindViewById<ProgressBar>(Resource.Id.loading);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));
            ListView.SetItemAnimator(new DefaultItemAnimator());
            ListView.ScrollChange += OnScroll;

            if (savedInstanceState != null)
                Query = savedInstanceState.GetString("Query");

#pragma warning disable CS4014
            Search(Query, querryType, true);
            return view;
        }

        public async Task Search(string search, string querryType, bool loadingBar)
        {
            if (search == null || search == "")
                return;

            searching = true;
            Query = search;

            if (loadingBar)
            {
                adapter = null;
                ListView.SetAdapter(null);
                EmptyView.Visibility = ViewStates.Gone;
                LoadingView.Visibility = ViewStates.Visible;
            }

            if (!await MainActivity.instance.WaitForYoutube())
            {
                ListView.SetAdapter(null);
                EmptyView.Text = MainActivity.instance.GetString(Resource.String.youtube_loading_error);
                EmptyView.SetTextColor(Color.Red);
                EmptyView.Visibility = ViewStates.Visible;
                return;
            }

            try
            {
                SearchResource.ListRequest searchResult = YoutubeManager.YoutubeService.Search.List("snippet");
                searchResult.Q = search;
                searchResult.TopicId = "/m/04rlf";
                switch (querryType)
                {
                    case "All":
                        searchResult.Type = "video,channel,playlist";
                        searchResult.EventType = null;
                        break;
                    case "Tracks":
                        searchResult.Type = "video";
                        searchResult.EventType = null;
                        break;
                    case "Playlists":
                        searchResult.Type = "playlist";
                        searchResult.EventType = null;
                        break;
                    case "Lives":
                        searchResult.Type = "video";
                        searchResult.EventType = SearchResource.ListRequest.EventTypeEnum.Live;
                        break;
                    case "Channels":
                        searchResult.Type = "channel";
                        searchResult.EventType = null;
                        break;
                    default:
                        searchResult.Type = "video";
                        searchResult.EventType = null;
                        break;
                }
                searchResult.MaxResults = 50;

                var searchReponse = await searchResult.ExecuteAsync();
                nextPageToken = searchReponse.NextPageToken;
                result = new List<YtFile>();

                foreach (var video in searchReponse.Items)
                    result.Add(GetYtFileFromSearchResult(video));

                LoadingView.Visibility = ViewStates.Gone;
                if (nextPageToken != null)
                    result.Add(new YtFile(new Song(), YtKind.Loading));

                if(result.Count > 0 && result[0].Kind == YtKind.Channel && result.Count(x => x.Kind == YtKind.Video && x.song.Artist == result[0].channel.Name) > 1)
                {
                    YtFile channelPreview = new YtFile(result[0].channel, YtKind.ChannelPreview);
                    result.Insert(0, channelPreview);
                }
                else if (result.Count > 0 && querryType == "All" || querryType == "Channels")
                {
                    IEnumerable<string> artist = result.GetRange(0, result.Count > 20 ? 20 : result.Count).GroupBy(x => x.song?.Artist).Where(x => x.Count() > 5).Select(x => x.Key);
                    if (artist.Count() == 1)
                    {
                        Channel channel = result.Find(x => x.Kind == YtKind.Channel && x.channel.Name == artist.First())?.channel;

                        if (channel != null)
                        {
                            YtFile channelPreview = new YtFile(channel, YtKind.ChannelPreview);
                            result.Insert(0, channelPreview);
                        }
                    }
                }

                adapter = new YtAdapter(result);
                adapter.ItemClick += ListView_ItemClick;
                adapter.ItemLongCLick += ListView_ItemLongClick;
                ListView.SetAdapter(adapter);
                searching = false;

                if (result.Count == 0)
                {
                    EmptyView.Visibility = ViewStates.Visible;
                    switch (querryType)
                    {
                        case "All":
                            EmptyView.Text = GetString(Resource.String.no_result) + " " + search;
                            break;
                        case "Tracks":
                            EmptyView.Text = GetString(Resource.String.no_track) + " " + search;
                            break;
                        case "Playlists":
                            EmptyView.Text = GetString(Resource.String.no_playlist) + " " + search;
                            break;
                        case "Lives":
                            EmptyView.Text = GetString(Resource.String.no_lives) + " " + search;
                            break;
                        case "Channels":
                            EmptyView.Text = GetString(Resource.String.no_channel) + " " + search;
                            break;
                        default:
                            break;
                    }
                }
                else
                    EmptyView.Visibility = ViewStates.Gone;
            }
            catch(Exception ex)
            {
                Console.WriteLine("&Exception catched in the youtube search: " + ex.Source + " - " + ex.Message);
                EmptyView.Text = GetString(Resource.String.timout);
                EmptyView.Visibility = ViewStates.Visible;
            }
        }

        private YtFile GetYtFileFromSearchResult(SearchResult result)
        {
            switch (result.Id.Kind)
            {
                case "youtube#video":
                    Song videoInfo = new Song(System.Net.WebUtility.HtmlDecode(result.Snippet.Title), result.Snippet.ChannelTitle, result.Snippet.Thumbnails.High.Url, result.Id.VideoId, -1, -1, null, true, false);
                    if (result.Snippet.LiveBroadcastContent == "live")
                        videoInfo.IsLiveStream = true;

                    return new YtFile(videoInfo, YtKind.Video);
                case "youtube#playlist":
                    PlaylistItem playlistInfo = new PlaylistItem(result.Snippet.Title, -1, result.Id.PlaylistId)
                    {
                        HasWritePermission = false,
                        ImageURL = result.Snippet.Thumbnails.High.Url,
                        Owner = result.Snippet.ChannelTitle
                    };
                    return new YtFile(playlistInfo, YtKind.Playlist);
                case "youtube#channel":
                    Channel channelInfo = new Channel(result.Snippet.Title, result.Id.ChannelId, result.Snippet.Thumbnails.High.Url);
                    return new YtFile(channelInfo, YtKind.Channel);
                default:
                    Console.WriteLine("&Kind = " + result.Id.Kind);
                    return null;
            }
        }

        private void ListView_ItemClick(object sender, int position)
        {
            switch (result[position].Kind)
            {
                case YtKind.Video:
                    YoutubeManager.Play(result[position].song);
                    break;
                case YtKind.Playlist:
                    MainActivity.instance.menu.FindItem(Resource.Id.search).CollapseActionView();
                    MainActivity.instance.FindViewById<TabLayout>(Resource.Id.tabs).Visibility = ViewStates.Gone;
                    MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(result[position].playlist)).AddToBackStack("Playlist Track").Commit();
                    break;
                case YtKind.Channel:
                    MainActivity.instance.menu.FindItem(Resource.Id.search).CollapseActionView();
                    MainActivity.instance.FindViewById<TabLayout>(Resource.Id.tabs).Visibility = ViewStates.Gone;
                    MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, ChannelDetails.NewInstance(result[position].channel)).AddToBackStack("Channel Details").Commit();
                    break;
                default:
                    break;
            }
        }

        private void ListView_ItemLongClick(object sender, int position)
        {
            if(result[position].Kind == YtKind.Video)
            {
                Song item = result[position].song;
                More(item);
            }
            else if(result[position].Kind == YtKind.Playlist)
            {
                PlaylistItem item = result[position].playlist;
                PlaylistMore(item);
            }
        }

        public void More(Song item)
        {
            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Title;
            bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Artist;
            Picasso.With(MainActivity.instance).Load(item.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            bottomSheet.SetContentView(bottomView);

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Play, Resources.GetString(Resource.String.play), (sender, eventArg) =>
                {
                    YoutubeManager.Play(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlaylistPlay, Resources.GetString(Resource.String.play_next), (sender, eventArg) =>
                {
                    YoutubeManager.PlayNext(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Queue, Resources.GetString(Resource.String.play_last), (sender, eventArg) =>
                {
                    YoutubeManager.PlayLast(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlayCircle, Resources.GetString(Resource.String.create_mix_from_song), (sender, eventArg) =>
                {
                    YoutubeManager.CreateMixFromSong(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlaylistAdd, Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) => { PlaylistManager.AddSongToPlaylistDialog(item); bottomSheet.Dismiss(); }),
                new BottomSheetAction(Resource.Drawable.Download, Resources.GetString(Resource.String.download), (sender, eventArg) =>
                {
                    YoutubeManager.Download(new[] { item });
                    bottomSheet.Dismiss();
                })
            });
            bottomSheet.Show();
        }

        public async void PlaylistMore(PlaylistItem item)
        {
            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Name;
            bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Owner;
            Picasso.With(MainActivity.instance).Load(item.ImageURL).Placeholder(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            bottomSheet.SetContentView(bottomView);

            List<BottomSheetAction> actions = new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Play, MainActivity.instance.Resources.GetString(Resource.String.play_in_order), (sender, eventArg) =>
                {
                    PlaylistManager.PlayInOrder(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Shuffle, MainActivity.instance.Resources.GetString(Resource.String.random_play), (sender, eventArg) =>
                {
                    PlaylistManager.Shuffle(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Queue, MainActivity.instance.Resources.GetString(Resource.String.add_to_queue), (sender, eventArg) =>
                {
                    PlaylistManager.AddToQueue(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Download, MainActivity.instance.Resources.GetString(Resource.String.download), (sender, eventArg) =>
                {
                    YoutubeManager.DownloadPlaylist(item, true, false);
                    bottomSheet.Dismiss();
                })
            };

            if(await PlaylistManager.IsForked(item))
            {
                actions.Add(new BottomSheetAction(Resource.Drawable.Delete, MainActivity.instance.Resources.GetString(Resource.String.unfork), (sender, eventArg) =>
                {
                    PlaylistManager.Unfork(item);
                    bottomSheet.Dismiss();
                }));
            }
            else
            {
                actions.Add(new BottomSheetAction(Resource.Drawable.LibraryAdd, MainActivity.instance.Resources.GetString(Resource.String.add_to_library), (sender, eventArg) =>
                {
                    PlaylistManager.ForkPlaylist(item);
                    bottomSheet.Dismiss();
                }));
            }

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
            bottomSheet.Show();
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString("Query", Query);
            base.OnSaveInstanceState(outState);
        }
    }
}
 
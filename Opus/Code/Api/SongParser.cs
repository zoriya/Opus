using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using Opus.Api;
using Opus.Api.Services;
using Opus.DataStructure;
using Opus.Fragments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Models;

namespace Opus.Api
{
    public class SongParser
    {
        private static readonly List<SongParser> instances = new List<SongParser>();
        /// <summary>
        /// The reference of the play item. The song parser will start the playback of this queue item as soon as possible.
        /// </summary>
        public static int playPosition = -2;
        private int queuePosition = -1;
        private bool canceled = false;

        public SongParser()
        {
            instances.Add(this);
        }


        #region queuePosition updates
        public static void QueueSlotAdded(int newPos)
        {
            foreach(SongParser instance in instances)
            {
                if (instance.queuePosition != -1 && newPos < instance.queuePosition)
                    instance.queuePosition++;
            }
        }

        public static void QueueSlotMoved(int oldPos, int newPos)
        {
            foreach (SongParser instance in instances)
            {
                if (instance.queuePosition != -1)
                {
                    if (oldPos == instance.queuePosition)
                        instance.queuePosition = newPos;
                    else if (oldPos < instance.queuePosition)
                        instance.queuePosition--;
                    else if (oldPos > instance.queuePosition && newPos < instance.queuePosition)
                        instance.queuePosition++;
                }
            }
        }

        public static void QueueSlotRemoved(int removedPosition)
        {
            foreach (SongParser instance in instances)
            {
                if (instance.queuePosition != -1)
                {
                    if (removedPosition < instance.queuePosition)
                        instance.queuePosition--;
                    else if (removedPosition == instance.queuePosition)
                        instance.Cancel();
                }
            }
        }
        #endregion

        /// <summary>
        /// Method that complete the song you give to make it playable with the player. Used only for youtube songs but you can give local songs here, that won't hurt.
        /// </summary>
        /// <param name="song">The song you want to complete</param>
        /// <param name="position">If the song is in the queue, set this parameter to the song's queue position. It will give display callbacks.</param>
        /// <param name="startPlaybackWhenPosible">Set this to true if you want to start playing the song before the end of this method.</param>
        /// <param name="forceParse">Set this to true if you want this method to retrieve steams info from youtube even if the song said that it is already parsed.</param>
        /// <returns></returns>
        public async Task<Song> ParseSong(Song song, int position = -1, bool startPlaybackWhenPosible = false, bool forceParse = false)
        {
            queuePosition = position;
            if(startPlaybackWhenPosible)
            {
                playPosition = position;
                Queue.instance.RefreshCurrent();
            }

            if ((!forceParse && song.IsParsed == true) || !song.IsYt)
            {
                if (startPlaybackWhenPosible)
                    MusicPlayer.instance.Play(song, -1, queuePosition == -1);

                return song;
            }

            if (song.IsParsed == null)
            {
                while (song.IsParsed == null)
                    await Task.Delay(10);

                if (canceled)
                    return song;

                if (playPosition == queuePosition && (await MusicPlayer.GetItem()).YoutubeID != song.YoutubeID)
                    MusicPlayer.instance.Play(song, -1, queuePosition == -1);

                return song; //Song is a class, the youtube id will be updated with another method
            }

            try
            {
                song.IsParsed = null;
                YoutubeClient client = new YoutubeClient();
                var mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(song.YoutubeID);
                if (mediaStreamInfo.HlsLiveStreamUrl != null)
                {
                    song.Path = mediaStreamInfo.HlsLiveStreamUrl;
                    song.IsLiveStream = true;
                }
                else
                {
                    song.IsLiveStream = false;

                    if (mediaStreamInfo.Audio.Count > 0)
                        song.Path = mediaStreamInfo.Audio.OrderBy(s => s.Bitrate).Last().Url;
                    else if (mediaStreamInfo.Muxed.Count > 0)
                        song.Path = mediaStreamInfo.Muxed.OrderBy(x => x.Resolution).Last().Url;
                    else
                    {
                        MainActivity.instance.NotStreamable(song.Title);
                        return null;
                    }

                    song.ExpireDate = mediaStreamInfo.ValidUntil;
                }
                song.IsParsed = true;

                if (queuePosition != -1)
                    Queue.instance?.NotifyItemChanged(queuePosition, Resource.Drawable.PublicIcon);

                if (canceled)
                    return song;

                if (playPosition == queuePosition && song.Album != null)
                {
                    if (queuePosition != -1)
                    {
                        MusicPlayer.currentID = queuePosition;
                        Queue.instance?.RefreshCurrent();
                        Player.instance?.RefreshPlayer();
                    }

                    MusicPlayer.instance.Play(song, -1, queuePosition == -1);
                    playPosition = -2;
                }

                Video video = await client.GetVideoAsync(song.YoutubeID);
                song.Title = video.Title;
                song.Artist = video.Author;
                song.Album = await YoutubeManager.GetBestThumb(video.Thumbnails);
                song.Duration = (int)video.Duration.TotalMilliseconds;

                if (queuePosition == MusicPlayer.CurrentID())
                    Player.instance?.RefreshPlayer();

                if (queuePosition != -1)
                {
                    Queue.instance?.NotifyItemChanged(queuePosition, song.Artist);
                    Home.instance?.NotifyQueueChanged(queuePosition, song.Artist);
                }

                if (canceled)
                    return song;

                if (playPosition == queuePosition)
                    MusicPlayer.instance.Play(song, -1, queuePosition == -1);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Console.WriteLine("&Parse time out");
                if (MainActivity.instance != null)
                    MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;
                song.IsParsed = false;

                if(playPosition == queuePosition)
                {
                    playPosition = -2;
                    Queue.instance.RefreshCurrent();
                    Player.instance?.Ready();
                    MainActivity.instance.Timout();
                }
            }
            catch (YoutubeExplode.Exceptions.VideoUnplayableException ex)
            {
                Console.WriteLine("&Parse error: " + ex.Message);
                MainActivity.instance.Unplayable(ErrorCode.SP2, song.Title, ex.Message);
                if (MainActivity.instance != null)
                    MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;

                song.IsParsed = false;
                if (queuePosition != -1)
                    MusicPlayer.RemoveFromQueue(queuePosition); //Remove the song from the queue since it can't be played.

                if (playPosition == queuePosition)
                {
                    playPosition = -2;
                    Queue.instance.RefreshCurrent();
                    Player.instance?.Ready();
                }
            }
            catch (YoutubeExplode.Exceptions.VideoUnavailableException)
            {
                MainActivity.instance.NotStreamable(song.Title);
                if (MainActivity.instance != null)
                    MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;

                song.IsParsed = false;
                if (queuePosition != -1)
                    MusicPlayer.RemoveFromQueue(queuePosition); //Remove the song from the queue since it can't be played.

                if (playPosition == queuePosition)
                {
                    playPosition = -2;
                    Queue.instance.RefreshCurrent();
                    Player.instance?.Ready();
                }
            }
            catch //We use this because when the network is reseted, an unknow error is thrown. We also don't want the app to crash at this state so it's ok to use a global catch.
            {
                song.IsParsed = false;

                if (MainActivity.instance != null)
                    MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;

                if (playPosition == queuePosition)
                {
                    playPosition = -2;
                    Queue.instance.RefreshCurrent();
                    Player.instance?.Ready();
                    MainActivity.instance.UnknowError(ErrorCode.SP1, null, Snackbar.LengthLong);
                }
            }

            if (playPosition == queuePosition)
                playPosition = -2;
            queuePosition = -1;
            instances.Remove(this);
            return song;
        }

        /// <summary>
        /// Will stop the parsing for a slot of the queue. Use this when the user wants to play something else before the end of a parsing (Want to play something on the same queue slot but not the same song has the one being parsed now).
        /// </summary>
        /// <param name="queuePosition"></param>
        public static void Cancel(int queuePosition)
        {
            instances.Where(x => x.queuePosition == queuePosition).FirstOrDefault()?.Cancel();
        }

        /// <summary>
        /// This method will remove playback calls
        /// </summary>
        private void Cancel()
        {
            instances.Remove(this);
            canceled = true;
        }

        /// <summary>
        /// This will cancel all parse (by calling the cancel method on all instances). Use this when the queue has too many changes to track them.
        /// </summary>
        public static void CancelAll()
        {
            while(instances.Count > 0)
                instances[0].Cancel();
        }
    }
}
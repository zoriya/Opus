using Opus.Api.Services;
using Opus.DataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Opus.Api
{
    public class SongManager
    {
        /// <summary>
        /// Play a song, can be a local one or a youtube one. The class will handle it automatically.
        /// </summary>
        /// <param name="item"></param>
        public static void Play(Song item)
        {
            if (!item.IsYt)
                LocalManager.Play(item.Path);
            else
                YoutubeManager.Play(item);
        }

        /// <summary>
        /// Add a song to the next slot of the queue. Handle both youtube and local files.
        /// </summary>
        /// <param name="item"></param>
        public static void PlayNext(Song item)
        {
            if (!item.IsYt)
                LocalManager.PlayNext(item.Path);
            else
                YoutubeManager.PlayNext(item);
        }

        /// <summary>
        /// Add a song to the end of the queue. Handle both youtube and local files.
        /// </summary>
        /// <param name="item"></param>
        public static void PlayLast(Song item)
        {
            if (!item.IsYt)
                LocalManager.PlayLast(item.Path);
            else
                YoutubeManager.PlayLast(item);
        }

        /// <summary>
        /// Play a list of song in it's default order
        /// </summary>
        /// <param name="items"></param>
        public async static void PlayInOrder(List<Song> items)
        {
            Play(items[0]);
            items.RemoveAt(0);

            await Task.Delay(1000);

            while (MusicPlayer.instance == null)
                await Task.Delay(10);

            MusicPlayer.instance.AddToQueue(items);
        }

        /// <summary>
        /// Play a list of song in a random order
        /// </summary>
        /// <param name="items"></param>
        public async static void Shuffle(List<Song> items)
        {
            Random r = new Random();
            items = items.OrderBy(x => r.Next()).ToList();

            Play(items[0]);
            items.RemoveAt(0);

            await Task.Delay(1000);

            while (MusicPlayer.instance == null)
                await Task.Delay(10);

            MusicPlayer.instance.AddToQueue(items);
        }

        /// <summary>
        /// Add a list of songs in the queue
        /// </summary>
        /// <param name="items"></param>
        public static void AddToQueue(List<Song> items)
        {
            if(MusicPlayer.instance == null || MusicPlayer.queue == null || MusicPlayer.queue.Count == 0)
            {
                PlayInOrder(items);
                return;
            }

            MusicPlayer.instance.AddToQueue(items);
        }
    }
}
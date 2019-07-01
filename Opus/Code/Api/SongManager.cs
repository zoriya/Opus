using Opus.Api.Services;
using Opus.DataStructure;
using Opus.Fragments;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Opus.Api
{
    public class SongManager
    {
        #region Simple Playback
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
        #endregion

        #region Multi-Song Playback
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
        #endregion

        #region Favorites
        /// <summary>
        /// Check if a song is present in the favorite list.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public async static Task<bool> IsFavorite(Song song)
        {
            return await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Favorites.sqlite"));
                db.CreateTable<Song>();

                if(db.Table<Song>().Where(x => (x.IsYt && x.YoutubeID == song.YoutubeID) || (!x.IsYt && x.LocalID == song.LocalID)).Count() > 0)
                    return true;
                else
                    return false;
            });
        }

        /// <summary>
        /// Add a song to the favorite playlist.
        /// </summary>
        /// <param name="song"></param>
        public async static void Fav(Song song)
        {
            Console.WriteLine("&Fav " + song.Title);
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Favorites.sqlite"));
                db.CreateTable<Song>();

                db.Insert(song);
            });
            Home.instance?.RefreshFavs();
        }

        /// <summary>
        /// Remove a song from the favorites.
        /// </summary>
        /// <param name="song"></param>
        public async static void UnFav(Song song)
        {
            Console.WriteLine("&UnFav " + song.Title);
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Favorites.sqlite"));
                db.CreateTable<Song>();

                if(song.IsYt)
                    db.Table<Song>().Delete(x => x.IsYt && x.YoutubeID == song.YoutubeID);
                else
                    db.Table<Song>().Delete(x => !x.IsYt && x.LocalID == song.LocalID);
            });
            Home.instance?.RefreshFavs();
        }

        /// <summary>
        /// Return the complete list of favorites.
        /// </summary>
        /// <returns></returns>
        public async static Task<List<Song>> GetFavorites()
        {
            return await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Favorites.sqlite"));
                db.CreateTable<Song>();

                return db.Table<Song>().ToList();
            });
        }
        #endregion
    }
}
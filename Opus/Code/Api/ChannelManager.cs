using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Opus.Fragments;
using System.Collections.Generic;
using System.Threading.Tasks;
using Channel = Opus.DataStructure.Channel;

namespace Opus.Api
{
    public class ChannelManager
    {
        /// <summary>
        /// Return a complete Channel object from the youtube id of the channel
        /// </summary>
        /// <param name="channelID"></param>
        /// <returns></returns>
        public async static Task<Channel> GetChannel(string channelID)
        {
            try
            {
                ChannelsResource.ListRequest request = YoutubeManager.YoutubeService.Channels.List("snippet");
                request.Id = channelID;

                ChannelListResponse response = await request.ExecuteAsync();

                if (response.Items.Count > 0)
                {
                    var result = response.Items[0];
                    return new Channel(result.Snippet.Title, channelID, result.Snippet.Thumbnails.High.Url);
                }
                else
                    return null;
            }
            catch
            {
                MainActivity.instance.UnknowError(ErrorCode.CG1);
                return null;
            }
        }

        /// <summary>
        /// Return a list of complete Channel objects from the youtube ids of the channels
        /// </summary>
        /// <param name="channelID"></param>
        /// <returns></returns>
        public async static Task<IEnumerable<Channel>> GetChannels(IEnumerable<string> channelIDs)
        {
            ChannelsResource.ListRequest request = YoutubeManager.YoutubeService.Channels.List("snippet");
            request.Id = string.Join(";", channelIDs);

            ChannelListResponse response = await request.ExecuteAsync();

            if (response.Items.Count > 0)
            {
                List<Channel> channels = new List<Channel>();
                foreach (var result in response.Items)
                    channels.Add(new Channel(result.Snippet.Title, result.Id, result.Snippet.Thumbnails.High.Url));

                return channels;
            }
            else
                return null;
        }

        /// <summary>
        /// Open the channel details tab for a channel and add the current view to the background.
        /// </summary>
        /// <param name="channelID"></param>
        public async static void OpenChannelTab(string channelID)
        {
            Channel channel = await GetChannel(channelID);
            if(channel != null)
                OpenChannelTab(channel);
        }
        public static void OpenChannelTab(Channel channel)
        {
            MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, ChannelDetails.NewInstance(channel)).AddToBackStack("Channel Details").Commit();
        }
    }
}
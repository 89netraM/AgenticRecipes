using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;

namespace AgenticRecipes.Web;

public sealed class MusicTool(ILogger<MusicTool> logger, SpotifyClient spotifyClient)
{
    [Description("Resumes the music. Returns the success status of resuming.")]
    public async Task<bool> ResumeMusic()
    {
        try
        {
            var success = await spotifyClient.Player.ResumePlayback();
            if (success)
            {
                logger.LogDebug("Resumed music");
            }
            else
            {
                logger.LogWarning("Failed to resume the music");
            }
            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resume the music");
            return false;
        }
    }

    [Description("Pause the music. Returns the success status of pausing.")]
    public async Task<bool> PauseMusic()
    {
        try
        {
            var success = await spotifyClient.Player.PausePlayback();
            if (success)
            {
                logger.LogDebug("Paused music");
            }
            else
            {
                logger.LogWarning("Failed to pause the music");
            }
            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resume the music");
            return false;
        }
    }
}

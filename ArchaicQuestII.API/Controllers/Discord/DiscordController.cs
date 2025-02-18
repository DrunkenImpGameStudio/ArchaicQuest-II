using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchaicQuestII.API.Controllers.Discord;

public class DiscordBotData
{
    public string Channel { get; set; }
    public string Message { get; set; }
    public string Username { get; set; }
}

public class DiscordController : Controller
{
    //private  IHubContext<GameHub> _gameHubContext;
    [HttpPost]
    [AllowAnonymous]
    [Route("api/discord/updateChannel")]
    public Task<IActionResult> Post([FromBody] DiscordBotData data)
    {
        if (ModelState.IsValid)
        {
            PostToNewbieChannel(data);
        }

        return Task.FromResult<IActionResult>(Ok());
    }

    public void PostToNewbieChannel(DiscordBotData data)
    {
        var message =
            $"<p class='newbie'>[<span>Newbie</span>] {data.Username}: {data.Message}</p>";

        foreach (
            var pc in GameLogic.Core.Services.Instance.Cache
                .GetAllPlayers()
                .Where(x => x.Config.NewbieChannel)
        )
        {
            GameLogic.Core.Services.Instance.Writer.WriteLine(message, pc);
            GameLogic.Core.Services.Instance.UpdateClient.UpdateCommunication(
                pc,
                message,
                data.Channel
            );
        }
    }
}

using Message.Domain.Dtos;
using Message.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Message.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController(
        ILogger<MessagesController> logger,
        IMessageService messageService): ControllerBase
{
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("Send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto message)
    {
        try
        {
            var res = await messageService.SendMessage(message);
            return res.Succeeded 
                ? Accepted()
                : BadRequest(res.Error);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            return StatusCode(500, e.Message);
        }
    }
}
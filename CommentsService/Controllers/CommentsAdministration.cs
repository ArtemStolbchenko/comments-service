using CommentsService.Communication;
using CommentsService.Database;
using CommentsService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace CommentsService.Controllerss
{
    [ApiController]
    [Route("/Comments/Admin/")]
    public class CommentsAdministration : ControllerBase
    {
        private readonly DatabaseHelper _dataHelper;
        private readonly RabbitMQManager _rabbitmqManager;
        public CommentsAdministration()
        {
            _dataHelper = new DatabaseHelper();
            _rabbitmqManager = new RabbitMQManager();
        }
        [HttpGet("test")]
        [Authorize("admin:comments")]
        public ActionResult Test()
        {
            return Ok();
        }
        [HttpPut("create")]
        [Authorize("admin:comments")]
        public ActionResult<Comment> UpdateComment([FromBody] Comment comment)
        {
            _dataHelper.Update(comment);
            _rabbitmqManager.Send(comment, CommentAction.Update);

            return Ok(comment);
        }
        [HttpDelete("RemoveById")]
        [Authorize("admin:comments")]
        public ActionResult Delete(long id)
        {
            if (_dataHelper.Delete(id))
            {
                _rabbitmqManager.SendDelete(id);
                return Ok();
            }
            else return BadRequest();
        }
    }
}

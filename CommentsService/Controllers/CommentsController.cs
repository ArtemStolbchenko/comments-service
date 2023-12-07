using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CommentsService.Models;
using CommentsService.Database;
using CommentsService.Communication;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Xml.Linq;

namespace CommentsService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly IDatabaseHelper _dataHelper;
        private readonly IRabbitMQManager _rabbitmqManager;
        public CommentsController()
        {
            _dataHelper = new DatabaseHelper();
            _rabbitmqManager = new RabbitMQManager();
        }
        [HttpGet("test")]
        [Authorize]
        public ActionResult Test()
        {
            string userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
            return Ok($"Yay, authorized! {userId}");
        }
        [HttpGet("all")]
        public ActionResult<List<Comment>> GetAll()
        {
            _rabbitmqManager.Send(new Comment(), CommentAction.Read);
            return Ok(_dataHelper.GetAll());
        }
        private ActionResult<List<Comment>> Synchronize()
        {
            var task = _rabbitmqManager.RequestUpdate();
            task.Wait();
            string response = task.Result;
            List<CommentDTO> commentDTOs = JsonConvert.DeserializeObject<List<CommentDTO>>(response);
            List<Comment> comments = commentDTOs.Select(c => new Comment(c)).ToList();
            if (comments != null)
                _dataHelper.SaveAll(comments);
            else return BadRequest(response);
            return Ok(_dataHelper.GetAll());
        }
        [HttpPost("create")]
        [Authorize]
        public ActionResult<Comment> PostComment([FromBody] Comment comment)
        {
            string userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
            comment.AuthorId = userId;

            _dataHelper.Save(comment);
            _rabbitmqManager.Send(comment, CommentAction.Create);

            return Ok(Synchronize().Result);
        }
        [HttpPut("create")]
        [Authorize]
        public ActionResult<Comment> UpdateComment([FromBody] Comment comment)
        {
            string userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;

            if (!_dataHelper.VerifyOwnership(comment.Id, userId))
                return Unauthorized();

            _dataHelper.Update(comment);
            _rabbitmqManager.Send(comment, CommentAction.Update);

            return Ok(comment);
        }
        [HttpDelete("RemoveById")]
        [Authorize]
        public ActionResult Delete(long id)
        {
            string userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;

            if (!_dataHelper.VerifyOwnership(id, userId))
            {
                Console.WriteLine("Failed to verify ownership");
                return Unauthorized();
            }

            if (_dataHelper.Delete(id))
            {
                _rabbitmqManager.SendDelete(id);
                return Ok();
            }
            else return BadRequest();
        }
    }
}

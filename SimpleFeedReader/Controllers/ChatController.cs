using Microsoft.AspNetCore.Mvc;
using SimpleFeedReader.Services;
using SimpleFeedReader.ViewModels;
using System.Threading.Tasks;

namespace SimpleFeedReader.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        public IActionResult Index()
        {
            var model = new ChatViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("Message cannot be empty");
            }

            try
            {
                var response = await _chatService.SendMessageAsync(message);
                return Json(new { success = true, response = response });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
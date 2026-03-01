using TenderSystem.Models;
using TenderSystem.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace FYPBidNetra.Controllers
{
    public class ChatController : Controller
    {

        private readonly TenderSystemContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IDataProtector _protector;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(TenderSystemContext context, IWebHostEnvironment env, DataSecurityProvider key, IDataProtectionProvider provider, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _env = env;
            _protector = provider.CreateProtector(key.Key);
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> UserMessage(short receiverId)
        {
            var currentUserId = Convert.ToInt16(User.Identity!.Name);

            // Fetch messages between the current user and the receiver
            var messages = await _context.Chats
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == receiverId) ||
                             (m.SenderId == receiverId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.CreatedDate)
                .ToListAsync();



            var receiver = await _context.UserLists
                .Where(u => u.UserId == receiverId)
                .Select(u => new
                {
                    FullName = (u.FirstName + " " + (u.MiddleName ?? "") + " " + u.LastName).Trim(),
                    UserPhoto = u.UserPhoto
                })
                .FirstOrDefaultAsync();

            ViewBag.ReceiverName = receiver?.FullName;
            ViewBag.ProfilePic = receiver?.UserPhoto;
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.ReceiverId = receiverId;

            // Return the messages to the view
            return View(messages);
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
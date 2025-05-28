using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UoNMarketPlace.DataContext;
using UoNMarketPlace.Model;
using UoNMarketPlace.ViewModel;

namespace UoNMarketPlace.Controllers
{
    public class ChatController : Controller
    {
        private readonly UoNDB _context;

        public ChatController(UoNDB context)
        {
            _context = context;
        }

        // View messages for a specific product between the seller and a buyer
        [HttpGet]
        public IActionResult ViewMessages(int productId, string buyerId)
        {
            var sellerId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sellerId == null) return Unauthorized();

            // Fetch all messages for the product between the seller and the buyer
            var messages = _context.Messages
                .Where(m => m.ProductId == productId &&
                            (m.SenderId == buyerId || m.ReceiverId == buyerId))
                .OrderBy(m => m.SentAt)
                .ToList();

            // If no messages are found, return not found
            if (!messages.Any()) return NotFound();

            var chatViewModel = new ChatViewModel
            {
                Messages = messages,
                ProductId = productId,
                SellerId = sellerId
            };

            return View(chatViewModel);
        }

        // Send a message from the seller to the buyer or vice versa
        [HttpPost]
        public IActionResult SendMessage(int productId, string receiverId, string message)
        {
            var senderId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (senderId == null) return Unauthorized();

            // Create a new message object
            var newMessage = new Message
            {
                ProductId = productId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = message,
                SentAt = DateTime.Now
            };

            // Save the message to the database
            _context.Messages.Add(newMessage);
            _context.SaveChanges();

            // Redirect back to the chat view
            return RedirectToAction("ViewMessages", new { productId, buyerId = receiverId });
        }
    }
}

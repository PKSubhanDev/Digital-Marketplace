using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UoNMarketPlace.DataContext;
using UoNMarketPlace.Model;

namespace UoNMarketPlace.Controllers
{
    
    public class AluminiController : Controller
    {
        private readonly UoNDB _context;
        private readonly UserManager<IdentityUser> _userManager;
        public AluminiController(UoNDB context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult AluminiLandingPage()
        {
            return View();
        }
        public IActionResult AlumniDashboard()
        {
            var alumniId = _userManager.GetUserId(User);
            var posts = _context.AlumniPosts.Where(p => p.AlumniId == alumniId).Include(p => p.Comments).Include(p => p.Likes).ToList();
            return View(posts);
        }
        // Display alumni posts for students in the forum
        public IActionResult Forum()
        {
            var posts = _context.AlumniPosts
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User) // Include the user for replies
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User) // Include the user for comments
                .Include(p => p.Likes)
                .ToList();

            return View(posts);
        }
        // For alumni to add a new post
        [HttpGet]
        public IActionResult CreatePost()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreatePost(AlumniPost model, IFormFile? image)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                model.AlumniId = userId;
                model.DatePosted = DateTime.Now;

                if (image != null)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    model.ImagePath = "/images/" + fileName;
                }
                else
                {
                    model.ImagePath = null; // No image provided, handle it gracefully
                }

                _context.AlumniPosts.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("AlumniDashboard");
            }

            return View(model);
        }
        // For alumni to delete their post
        [HttpPost]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var post = await _context.AlumniPosts.FindAsync(postId);
            if (post != null)
            {
                _context.AlumniPosts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("AlumniDashboard");
        }
        // Add a comment to a post
        [HttpPost]
        public async Task<IActionResult> AddComment(int postId, string commentText)
        {
            var comment = new Comment
            {
                PostId = postId,
                Text = commentText,
                DateCommented = DateTime.Now,
                UserId = _userManager.GetUserId(User)
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Forum");
        }
        // Like a post
        [HttpPost]
        public async Task<IActionResult> LikePost(int postId)
        {
            var userId = _userManager.GetUserId(User);
            var like = new Like { PostId = postId, UserId = userId };

            if (!_context.Likes.Any(l => l.PostId == postId && l.UserId == userId))
            {
                _context.Likes.Add(like);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Forum");
        }
        // Add a reply to a comment
        [HttpPost]
        public async Task<IActionResult> AddReply(int commentId, string replyText)
        {
            var reply = new Reply
            {
                CommentId = commentId,
                Text = replyText,
                DateReplied = DateTime.Now,
                UserId = _userManager.GetUserId(User)
            };
            _context.Replies.Add(reply);
            await _context.SaveChangesAsync();

            return RedirectToAction("Forum");
        }
    }
}

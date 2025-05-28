using Microsoft.AspNetCore.Mvc;
using UoNMarketPlace.DataContext;
using UoNMarketPlace.ViewModel;
using UoNMarketPlace.Model;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace UoNMarketPlace.Controllers
{
    public class ProductController : Controller
    {
        private readonly UoNDB _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public ProductController(UoNDB context, UserManager<IdentityUser> userManager , RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        #region Landing Page
        public async Task<IActionResult> LandingPage()
        {
            // Await the user retrieval task
            var user = await _userManager.GetUserAsync(User);

            // Now you can use the user object to get roles
            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.Roles = roles; // Pass roles to the view

            return View();
        }
        #endregion

        #region Sell
        [HttpGet]
        public IActionResult Sell()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Sell(sellViewModel model)
        {
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var sellerIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

                if (sellerIdClaim == null)
                {
                    return Unauthorized(); // If the user is not authenticated
                }

                var sellerId = sellerIdClaim.Value;
                var imagePaths = await SaveImages(model.ProductImages); // Modify to save multiple images
                var product = new sellProduct
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    Category = model.Category,
                    ImagePath = string.Join(",", imagePaths),// Join the image paths as a comma-separated string
                    SellerId = sellerId,
                    DateUploaded = DateTime.Now,
                    IsApproved = true // New products are approved by default
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction("SellerDashboard");
            }

            return View(model);
        }
        #endregion

        #region Buy
        public IActionResult Buy(string search = "", string category = "") 
        {
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch all products from the database
            var products = _context.Products.Where(p => p.IsApproved && !p.IsSold && !p.IsFlagged  && p.SellerId != userId).AsQueryable();

            // Apply search by product name or description
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
            }

            // Apply price range filter
            //products = products.Where(p => p.Price >= minPrice && p.Price <= maxPrice);

            // Return the filtered products to the view
            return View(products.ToList());
        }
        public IActionResult BuyNow(int Id)
        {
            var product = _context.Products
                .Include(p => p.Messages) // Include messages
                .FirstOrDefault(p => p.Id == Id);

            if (product == null || product.IsSold) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Retrieve all messages related to the product involving the current user
            var messages = product.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId || m.ReceiverId == product.SellerId)
                .OrderBy(m => m.SentAt)
                .ToList();

            var chatViewModel = new ChatViewModel
            {
                Messages = messages,
                ProductId = product.Id,
                SellerId = product.SellerId
            };

            return View(chatViewModel);
        }

        #endregion

        #region Send Message
        [HttpPost]
        public IActionResult SendMessage(int productId, string receiverId, string message)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var newMessage = new Message
            {
                ProductId = productId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = message,
                SentAt = DateTime.Now
            };

            _context.Messages.Add(newMessage);
            _context.SaveChanges();

            return RedirectToAction("BuyNow", new { Id = productId });
        }


        #endregion

        #region Mark as sold 
        #region Mark as Sold
        [HttpPost]
        public async Task<IActionResult> MarkAsSold(int productId)
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var product = await _context.Products
                .Include(p => p.Messages) // Load messages to find the buyer
                .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);

            if (product == null) return NotFound();
            if (product.IsSold) return BadRequest("This product is already marked as sold.");

            // Find the most recent valid buyer (message sender who is not the seller)
            var latestBuyer = product.Messages
                .Where(m => m.SenderId != sellerId) // Exclude seller's messages
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefault()?.SenderId;

            if (string.IsNullOrEmpty(latestBuyer))
            {
                TempData["Notification"] = "No valid buyer found to notify.";
                return RedirectToAction("SellerDashboard");
            }

            product.IsSold = true;
            product.BuyerId = latestBuyer; // Assign the valid buyer to the product

            // Create notifications for both buyer and seller
            var buyerNotification = new Notification
            {
                UserId = latestBuyer,
                Message = $"Congratulations! You have successfully purchased the product '{product.Name}'. Please leave a review.",
                CreatedAt = DateTime.Now,
                IsRead = false,
                SellerId = product.SellerId // Include seller ID
            };
            _context.Notifications.Add(buyerNotification);

            var sellerNotification = new Notification
            {
                UserId = product.SellerId,
                Message = $"Your product '{product.Name}' has been sold to a buyer.",
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(sellerNotification);

            await _context.SaveChangesAsync();

            TempData["Notification"] = "Product marked as sold successfully!";
            return RedirectToAction("SellerDashboard");
        }
        #endregion


        #endregion

        #region Product details
        public IActionResult ProductDetails(int id)
        {
            // Fetch the product by ID along with reviews
            // Fetch the product by ID, even if flagged, but exclude it from reviews display
            var product = _context.Products
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Check if the product is flagged and show an appropriate message
            if (product.IsFlagged)
            {
                ViewBag.Message = "This product is pending review for being flagged as inappropriate.";
            }

            // For unapproved or flagged products, don't display reviews
            if (!product.IsApproved || product.IsFlagged)
            {
                return View("FlaggedProductDetails", product); // Create a separate view for flagged products
            }

            // Load reviews for approved and non-flagged products only
            product = _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefault(p => p.Id == id && p.IsApproved && !p.IsFlagged);


            // If the current user is not the seller, increment views
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var sellerIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (sellerIdClaim == null || sellerIdClaim.Value != product.SellerId)
            {
                product.Views++;
                _context.SaveChanges();
            }

            // Calculate average rating if there are reviews
            if (product.Reviews.Any())
            {
                product.Rating = product.Reviews.Average(r => r.Rating);
            }

            return View(product);
        }

        public IActionResult ProductDetailsSeller(int id)
        {
            // Fetch the product by ID along with reviews
            // Fetch the product by ID, even if flagged, but exclude it from reviews display
            var product = _context.Products
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Check if the product is flagged and show an appropriate message
            if (product.IsFlagged)
            {
                ViewBag.Message = "This product is pending review for being flagged as inappropriate.";
            }

            // For unapproved or flagged products, don't display reviews
            if (!product.IsApproved || product.IsFlagged)
            {
                return View("FlaggedProductDetails", product); // Create a separate view for flagged products
            }

            // Load reviews for approved and non-flagged products only
            product = _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefault(p => p.Id == id && p.IsApproved && !p.IsFlagged);


            // If the current user is not the seller, increment views
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var sellerIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (sellerIdClaim == null || sellerIdClaim.Value != product.SellerId)
            {
                product.Views++;
                _context.SaveChanges();
            }

            // Calculate average rating if there are reviews
            if (product.Reviews.Any())
            {
                product.Rating = product.Reviews.Average(r => r.Rating);
            }

            return View(product);
        }

        #endregion

        #region Seller Dashboard
        public IActionResult SellerDashboard(string sortBy = "dateUploaded", string filterBy = "")
        {
            var sellerId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sellerId == null) return Unauthorized(); // Ensure the user is authenticated

            // Retrieve all products for the current seller, including related messages
            var products = _context.Products
                .Where(p => p.SellerId == sellerId)
                .Include(p => p.Messages) // Include related messages
                .ToList();

            // Retrieve all reviews for the seller
            var sellerReviews = _context.ProductReviews
                .Where(r => r.SellerId == sellerId)
                .Include(r => r.Product) // Include product details
                .ToList();

            // Calculate the average rating for the seller
            double averageRating = sellerReviews.Any()
                ? sellerReviews.Average(r => r.Rating)
                : 0;

            ViewBag.AverageRating = averageRating; // Pass average rating to the view
            ViewBag.TotalReviews = sellerReviews.Count; // Total number of reviews

            var buyerIds = products.Select(p => p.BuyerId).Where(id => id != null).Distinct();

            // Fetch usernames for unique sender IDs from the messages
            var senderIds = products.SelectMany(p => p.Messages.Select(m => m.SenderId)).Distinct();
            var senders = _context.Users
                .Where(u => senderIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.UserName); // Map user IDs to usernames

            var buyers = _context.Users
               .Where(u => buyerIds.Contains(u.Id))
               .ToDictionary(u => u.Id, u => u.UserName); // Map buyers

            ViewBag.Buyers = buyers; // Store buyer usernames for easy access in the view
            ViewBag.Senders = senders; // Store sender usernames for easy access

            // Apply filtering if provided
            if (!string.IsNullOrEmpty(filterBy))
            {
                products = products.Where(p =>
                    p.Name.Contains(filterBy, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(filterBy, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(filterBy, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Apply sorting based on the sortBy parameter
            products = sortBy switch
            {
                "views" => products.OrderByDescending(p => p.Views).ToList(),
                _ => products.OrderByDescending(p => p.DateUploaded).ToList()
            };

            ViewBag.SortBy = sortBy;
            ViewBag.FilterBy = filterBy;

            ViewBag.Reviews = sellerReviews; // Pass reviews to the view

            return View(products);
        }

        #endregion 

        #region Delete
        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            // You should also delete it from the buy list or any other related tables.
            return RedirectToAction("SellerDashboard");
        }
        #endregion

        #region Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var sellerIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            // Ensure that the product belongs to the current seller
            if (sellerIdClaim == null || sellerIdClaim.Value != product.SellerId)
            {
                return Unauthorized(); // Only the seller can edit their product
            }

            var model = new EditProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                ExistingImages = product.ImagePath.Split(',').ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var product = await _context.Products.FindAsync(model.Id);

            if (product == null)
            {
                return NotFound();
            }

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var sellerIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            // Ensure that the product belongs to the current seller
            if (sellerIdClaim == null || sellerIdClaim.Value != product.SellerId)
            {
                return Unauthorized();
            }

            // Update product details
            product.Name = model.Name;
            product.Description = model.Description;
            product.Price = model.Price;
            product.Category = model.Category;

            // Handle new image uploads
            if (model.ProductImages != null && model.ProductImages.Any())
            {
                var newImagePaths = await SaveImages(model.ProductImages);
                product.ImagePath = string.Join(",", newImagePaths);
            }

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("SellerDashboard");
        }
        #endregion

        #region Review
        [HttpPost]
        public async Task<IActionResult> SubmitReview(string sellerId, int rating, string reviewText)
        {
            // Fetch the buyer's ID from the logged-in user (no need to pass it)
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var buyerName = User.Identity.Name;

            var product = await _context.Products
            .Where(p => p.SellerId == sellerId && p.BuyerId == buyerId && p.IsSold)
            .OrderByDescending(p => p.DateUploaded) // Use the latest sold product
            .FirstOrDefaultAsync();
            // Ensure the seller exists
            var sellerExists = await _context.Users.AnyAsync(u => u.Id == sellerId);
            if (!sellerExists) return NotFound("Seller not found.");

            // Create the review for the seller (and optionally the product)
            var review = new ProductReview
            {
                SellerId = sellerId,    // The seller being reviewed
                UserId = buyerId,       // The buyer submitting the review
                UserName = buyerName,   // Buyer’s name
                Rating = rating,
                ReviewText = reviewText,
                DateReviewed = DateTime.Now,
                ProductId = product.Id
            };

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Notification"] = "Review submitted successfully!";

            // Redirect to the landing page
            return RedirectToAction("LandingPage");
        }
        #endregion

        #region Flagged Product
        [HttpGet]
        public IActionResult FlagProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null || !product.IsApproved)
            {
                return NotFound();
            }

            return View(product);
        }
        [HttpPost]
        public async Task<IActionResult> FlagProduct(int productId, string reason)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            // Flag the product as inappropriate
            product.IsFlagged = true;
            product.FlagReason = reason;
            product.IsApproved = false; // Pending admin review

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            // Optionally, redirect to a confirmation page or the product details page
            return RedirectToAction("FlagConfirmation");
        }
        // Confirmation view after flagging a product
        public IActionResult FlagConfirmation()
        {
            return View();
        }
        #endregion

        #region Forum
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

        #endregion


        #region Notification 
        public async Task<IActionResult> Notifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch notifications for the current user (buyer or seller)
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Mark all unread notifications as read
            notifications.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();

            // Determine if the user is the buyer for any sold product
            var isBuyer = _context.Products.Any(p => p.BuyerId == userId);

            ViewBag.IsBuyer = isBuyer; // Store flag to differentiate between views

            return View(notifications);
        }
        #endregion

        #region Private functions
        private async Task<List<string>> SaveImages(List<IFormFile> imageFiles)
        {
            var imagePaths = new List<string>();

            // Define the image directory path
            var imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

            // Check if the directory exists, if not, create it
            if (!Directory.Exists(imageDirectory))
            {
                Directory.CreateDirectory(imageDirectory);
            }

            // Loop through the files and save them
            foreach (var imageFile in imageFiles)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(imageDirectory, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    imagePaths.Add("/images/" + fileName); // Store the relative path
                }
            }

            return imagePaths;
        }
        #endregion


    }
}

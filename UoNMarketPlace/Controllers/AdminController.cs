using Microsoft.AspNetCore.Mvc;
using UoNMarketPlace.DataContext;
using UoNMarketPlace.Model;
using Microsoft.EntityFrameworkCore;

namespace UoNMarketPlace.Controllers
{
    public class AdminController : Controller
    {
        private readonly UoNDB _context;

        public AdminController(UoNDB context)
        {
            _context = context;
        }

        public IActionResult AdminLandingPage()
        {
            var flaggedProducts = _context.Products.Where(p => p.IsFlagged && !p.IsApproved).ToList();

            // Summary statistics
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.ProductsListed = _context.Products.Count();
            ViewBag.FlaggedProducts = flaggedProducts.Count;

            return View(flaggedProducts);
        }

        [HttpGet]
        public JsonResult GetProductCategoryData()
        {
            var productCategories = _context.Products
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToList();

            return Json(productCategories);
        }

        [HttpGet]
        public JsonResult GetMonthlyProductData()
        {
            var monthlyData = _context.Products
                .GroupBy(p => p.DateUploaded.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToList();

            return Json(monthlyData);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveProduct(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            product.IsApproved = true;
            product.IsFlagged = false;
            product.FlagReason = null;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("AdminLandingPage");
        }

        [HttpPost]
        public async Task<IActionResult> RejectProduct(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("AdminLandingPage");
        }
    }
}

using MakeUBeauty.Data;
using MakeUBeauty.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MakeUBeauty.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var users = _context.Users.ToList();

            var totalUsers = _context.Users.Count(u => u.Role != "Admin");

            var totalOrders = _context.Orders.Count();

            var totalSales = _context.Orders
                .Where(o => o.Status != "Cancelled")
                .Sum(o => (decimal?)o.TotalAmount) ?? 0;

            var pendingOrders = _context.Orders.Count(o => o.Status == "Pending");

            var recentOrders = _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalSales = totalSales;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.RecentOrders = recentOrders;

            return View(users);
        }

        public async Task<IActionResult> Products()
        {
            var products = await _context.Products.OrderByDescending(p => p.Id).ToListAsync();
            return View(products);
        }

        public IActionResult UserDetails(int id)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Id == id);

            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product model, IFormFile ProductPicture)
        {
            if (ModelState.IsValid)
            {
                if (ProductPicture != null && ProductPicture.Length > 0)
                {
                    string folder = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ProductPicture.FileName);
                    string filePath = Path.Combine(folder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProductPicture.CopyToAsync(fileStream);
                    }

                    model.ImageUrl = uniqueFileName;
                }

                _context.Products.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Products));
            }

            var products = await _context.Products.ToListAsync();
            return View("Products", products);
        }

        [HttpGet]
        public async Task<IActionResult> GetEditPartial(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return PartialView("_EditProductPartial", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product model, IFormFile? ProductPicture)
        {
            var productInDb = await _context.Products.FindAsync(model.Id);
            if (productInDb == null) return NotFound();

            productInDb.Name = model.Name;
            productInDb.Price = model.Price;
            productInDb.OriginalPrice = model.OriginalPrice;
            productInDb.Stock = model.Stock;
            productInDb.Category = model.Category;
            productInDb.Brand = model.Brand;
            productInDb.Description = model.Description;
            productInDb.IsActive = model.IsActive;

            if (ProductPicture != null && ProductPicture.Length > 0)
            {
                if (!string.IsNullOrEmpty(productInDb.ImageUrl))
                {
                    var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, "images/products", productInDb.ImageUrl);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ProductPicture.FileName);
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images/products", fileName);

                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await ProductPicture.CopyToAsync(stream);
                }

                productInDb.ImageUrl = fileName;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images/products", product.ImageUrl);
                if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = !product.IsActive;
                await _context.SaveChangesAsync();
                return Ok(new { success = true, isActive = product.IsActive });
            }
            return BadRequest();
        }

        [HttpPost]
        public IActionResult Checkout()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Home");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Home");
            }

            ViewBag.UserEmail = user.Email;
            ViewBag.UserName = user.Name;
            ViewBag.UserPhone = user.PhoneNumber;

            return View();
        }

        public IActionResult Orders(string status = "All")
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .AsQueryable();

            ViewBag.AllCount = query.Count();

            ViewBag.PendingCount = query
                .Count(o => o.Status == "Pending" || o.Status == "Processing");

            ViewBag.DispatchedCount = query
                .Count(o => o.Status == "Shipped");

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                if (status == "Pending")
                {
                    query = query.Where(o => o.Status == "Pending" || o.Status == "Processing");
                }
                else if (status == "Dispatched")
                {
                    query = query.Where(o => o.Status == "Shipped");
                }
                else
                {
                    query = query.Where(o => o.Status == status);
                }
            }

            ViewBag.CurrentStatus = status;

            return View(query.ToList());
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int orderId, string status)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null)
                return Json(new { success = false });

            // ACCEPT ORDER
            if (status == "Processing")
            {
                foreach (var item in order.OrderItems)
                {
                    var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);

                    if (product == null || product.Stock < item.Quantity)
                    {
                        order.Status = "Cancelled";
                        _context.SaveChanges();

                        return Json(new
                        {
                            success = false,
                            message = "Not enough stock. Order has been cancelled."
                        });
                    }
                }

                // Deduct stock after checking
                foreach (var item in order.OrderItems)
                {
                    var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);

                    if (product != null)
                    {
                        product.Stock -= item.Quantity;
                    }
                }

                order.Status = "Processing";
            }

            // SHIP ORDER
            else if (status == "Shipped")
            {
                order.Status = "Shipped";
            }

            // DELIVER ORDER
            else if (status == "Delivered")
            {
                order.Status = "Delivered";
            }

            else if (status == "Cancelled")
            {
                order.Status = "Cancelled";
            }

            _context.SaveChanges();

            return Json(new { success = true });
        }

        public IActionResult PrintInvoice(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return NotFound();

            return View("Invoice", order);
        }

        [HttpPost]
        public async Task<IActionResult> SendUpdateEmail(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return Json(new { success = false });

            try
            {
                var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("your_email@gmail.com", "your_app_password"),
                    EnableSsl = true
                };

                string body = $@"
        Hello {order.FullName},

        Your order (ORD-{order.Id.ToString("D6")}) has been updated.

        Total: ₱{order.TotalAmount:N2}

        Thank you for shopping with Make U Beauty 💄
        ";

                var message = new MailMessage
                {
                    From = new MailAddress("your_email@gmail.com", "Make U Beauty"),
                    Subject = "Order Update - Make U Beauty",
                    Body = body,
                    IsBodyHtml = false
                };

                message.To.Add(order.Email);

                await smtp.SendMailAsync(message);

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        public IActionResult Customers(string search)
        {
            var users = _context.Users
                .Where(u => u.Role != "Admin");

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u =>
                    u.Name.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.Id.ToString().Contains(search));
            }

            var result = users
                .OrderByDescending(u => u.Id)
                .ToList();

            return View(result);
        }

        public IActionResult ExitAdmin()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
    }
}
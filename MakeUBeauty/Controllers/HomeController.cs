using Microsoft.AspNetCore.Mvc;
using MakeUBeauty.Data;
using MakeUBeauty.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace MakeUBeauty.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        private void LoadCounts()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                ViewBag.CartCount = 0;
                ViewBag.WishlistCount = 0;
                ViewBag.OrderCount = 0;
                return;
            }

            int uid = userId.Value;

            ViewBag.CartCount = _context.Carts
                .Where(c => c.UserId == uid)
                .Sum(c => (int?)c.Quantity) ?? 0;

            ViewBag.WishlistCount = _context.Wishlists
                .Count(w => w.UserId == uid);

            ViewBag.OrderCount = _context.Orders
                .Count(o => o.UserId == uid);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            LoadUserWishlist();
            LoadCounts();

            var categoriesToShow = new[] { "Lipstick", "Blush", "Eyeshadow", "Mascara", "Foundation" };

            var activeCounts = await _context.Products
                .Where(p => p.IsActive)
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Category, x => x.Count);

            ViewBag.CategoryCounts = categoriesToShow.ToDictionary(
                cat => cat,
                cat => activeCounts.ContainsKey(cat) ? activeCounts[cat] : 0
            );

            ViewBag.NewArrivals = await _context.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id)
                .Take(6)
                .ToListAsync();

            var brands = new[] { "Issy", "Maybelline", "Vice Cosmetics" };
            var featured = await _context.Products
                .Where(p => p.IsActive && categoriesToShow.Contains(p.Category) && brands.Contains(p.Brand))
                .Take(15)
                .ToListAsync();

            return View(featured);
        }

        [Route("Shop")]
        [HttpGet]
        public async Task<IActionResult> Shop(string searchTerm, string brand, string category, string sortOrder)
        {
            LoadUserWishlist();
            LoadCounts();

            IQueryable<Product> productsQuery = _context.Products
                .Include(p => p.Reviews)
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                productsQuery = productsQuery.Where(p => p.Name.ToLower().Contains(searchTerm.ToLower()));

            if (!string.IsNullOrWhiteSpace(brand))
                productsQuery = productsQuery.Where(p => p.Brand == brand);

            if (!string.IsNullOrWhiteSpace(category))
                productsQuery = productsQuery.Where(p => p.Category == category);

            productsQuery = sortOrder switch
            {
                "price_asc" => productsQuery.OrderBy(p => p.Price),
                "price_desc" => productsQuery.OrderByDescending(p => p.Price),
                "rating" => productsQuery.OrderByDescending(p => p.Rating),
                _ => productsQuery.OrderByDescending(p => p.Id)
            };

            ViewBag.Categories = new List<string> { "Lipstick", "Blush", "Eyeshadow", "Mascara", "Foundation" };
            ViewBag.Brands = await _context.Products.Where(p => p.IsActive).Select(p => p.Brand).Distinct().ToListAsync();

            return View(await productsQuery.ToListAsync());
        }

        public IActionResult Brands()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                ViewBag.WishlistProductIds = new List<int>();
                return View(_context.Products.ToList());
            }

            int uid = userId.Value;

            var wishlistIds = _context.Wishlists
                .Where(w => w.UserId == uid)
                .Select(w => w.ProductId)
                .ToList();

            ViewBag.WishlistProductIds = wishlistIds;

            return View(_context.Products.ToList());
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            LoadUserWishlist();
            return View(product);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string FullName, string Email, string Password, string ConfirmPassword, bool Terms)
        {
            if (!Terms)
            {
                TempData["ErrorMessage"] = "You must agree to the Terms of Service.";
                return RedirectToAction("Register");
            }

            if (Password != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return RedirectToAction("Register");
            }

            var existingUser = _context.Users.FirstOrDefault(u => u.Email == Email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "This email is already registered.";
                return RedirectToAction("Register");
            }

            var user = new User
            {
                Name = FullName,
                Email = Email,
                Password = Password
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Account created successfully! You can now login.";

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                TempData["ErrorMessage"] = "This email is not registered.";
                return RedirectToAction("ForgotPassword");
            }

            TempData["SuccessMessage"] = "Recovery link has been sent to your email.";

            return RedirectToAction("ForgotPassword");
        }

        [HttpGet, Route("Login")]
        public IActionResult Login() => View();

        [HttpPost, Route("Login"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user != null)
            {

                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserStatus", "LoggedIn");
                HttpContext.Session.SetString("UserRole", user.Role ?? "Customer");
                HttpContext.Session.SetString("UserName", user.Name);

                var wishCount = await _context.Wishlists.CountAsync(w => w.UserId == user.Id);
                var cartCount = await _context.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);

                HttpContext.Session.SetInt32("WishlistCount", wishCount);
                HttpContext.Session.SetInt32("CartCount", cartCount);

                return user.Role == "Admin" ? RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = "The email or password provided is incorrect.";
            return View();
        }

        [HttpGet, Route("Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId.Value);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        [HttpPost]
        public IActionResult UpdateProfile(User model, IFormFile profileImage)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId.Value);

            if (user == null)
                return RedirectToAction("Login");

            user.Name = model.Name;
            user.PhoneNumber = model.PhoneNumber;
            user.Gender = model.Gender;
            user.Bio = model.Bio;

            if (profileImage != null && profileImage.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    profileImage.CopyTo(stream);
                }

                user.ProfilePicture = fileName;
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Profile updated successfully.";

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public IActionResult UpdateCartSelection(int id, bool isSelected)
        {
            var item = _context.Carts.FirstOrDefault(c => c.Id == id);

            if (item == null)
            {
                return Json(new { success = false });
            }

            item.IsSelected = isSelected;

            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateCartQuantity(int id, int amount)
        {
            var item = _context.Carts.FirstOrDefault(c => c.Id == id);

            if (item == null)
                return Json(new { success = false });

            item.Quantity += amount;

            if (item.Quantity <= 0)
            {
                _context.Carts.Remove(item);
            }

            _context.SaveChanges();

            var newCartCount = _context.Carts
                .Where(c => c.UserId == item.UserId)
                .Sum(c => c.Quantity);

            return Json(new
            {
                success = true,
                newQty = item.Quantity > 0 ? item.Quantity : 0,
                newRowTotal = (item.Price * item.Quantity).ToString("N2"),
                newCartCount = newCartCount
            });
        }

        [HttpPost]
        public IActionResult ToggleWishlist(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
                return Json(new { success = false });

            var existing = _context.Wishlists
                .FirstOrDefault(w => w.UserId == userId && w.ProductId == id);

            if (existing != null)
            {
                _context.Wishlists.Remove(existing);
            }
            else
            {
                _context.Wishlists.Add(new Wishlist
                {
                    UserId = userId.Value,
                    ProductId = id
                });
            }

            _context.SaveChanges();

            var count = _context.Wishlists
                .Count(w => w.UserId == userId);

            return Json(new { success = true, newCount = count });
        }

        public IActionResult Wishlist()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var wishlistItems = _context.Wishlists
                .Where(w => w.UserId == userId.Value)
                .ToList();

            var productIds = wishlistItems.Select(w => w.ProductId).ToList();

            var products = _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToList();

            return View(products);
        }

        [HttpPost]
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return Json(new
                {
                    success = false,
                    redirect = Url.Action("Login", "Home")
                });
            }

            int uid = userId.Value;

            var product = _context.Products.FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Product not found."
                });
            }

            // OUT OF STOCK CHECK
            if (product.Stock <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "This product is out of stock."
                });
            }

            var existing = _context.Carts
                .FirstOrDefault(c => c.UserId == uid && c.ProductId == id);

            int totalRequestedQty = quantity;

            if (existing != null)
            {
                totalRequestedQty += existing.Quantity;
            }

            // STOCK LIMIT CHECK
            if (totalRequestedQty > product.Stock)
            {
                return Json(new
                {
                    success = false,
                    message = $"Only {product.Stock} item(s) available in stock."
                });
            }

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                _context.Carts.Add(new Cart
                {
                    UserId = uid,
                    ProductId = id,
                    Quantity = quantity,

                    ProductName = product.Name,
                    ProductImageUrl = product.ImageUrl,
                    BrandName = product.Brand,
                    Price = product.Price,
                    IsSelected = true
                });
            }

            _context.SaveChanges();

            var newCount = _context.Carts
                .Where(c => c.UserId == uid)
                .Sum(c => c.Quantity);

            HttpContext.Session.SetInt32("CartCount", newCount);

            return Json(new
            {
                success = true,
                newCount = newCount
            });
        }

        [HttpGet, Route("Home/Cart")]
        public async Task<IActionResult> Cart()
        {
            LoadCounts();

            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            int uid = userId.Value;

            var items = await _context.Carts
                .Where(c => c.UserId == uid)
                .Include(c => c.Product)
                .ToListAsync();

            Console.WriteLine("USER ID: " + uid);
            Console.WriteLine("ITEM COUNT: " + items.Count);

            return View(items);
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            var item = _context.Carts.Find(id);

            if (item != null)
            {
                _context.Carts.Remove(item);
                _context.SaveChanges();
            }

            int? userId = HttpContext.Session.GetInt32("UserId");

            var newCount = _context.Carts
                .Where(c => c.UserId == userId)
                .Sum(c => c.Quantity);

            return Json(new
            {
                success = true,
                newCount = newCount
            });
        }

        [HttpPost]
        public IActionResult DeleteAllCartItems()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
                return Json(new { success = false });

            var items = _context.Carts.Where(c => c.UserId == userId);

            _context.Carts.RemoveRange(items);
            _context.SaveChanges();

            return Json(new { success = true, newCount = 0 });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedCartItems(List<int> ids)
        {
            var items = _context.Carts.Where(c => ids.Contains(c.Id));

            _context.Carts.RemoveRange(items);
            await _context.SaveChangesAsync();

            int? userId = HttpContext.Session.GetInt32("UserId");

            int newCount = _context.Carts
                .Where(c => c.UserId == userId)
                .Sum(c => c.Quantity);

            return Json(new { success = true, newCount });
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            LoadCounts();

            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            int uid = userId.Value;

            var items = await _context.Carts
                .Where(c => c.UserId == uid && c.IsSelected)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        public IActionResult PlaceOrder(Order model)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user == null)
                {
                    return Json(new { success = false, message = "The email is not yet registered." });
                }

                var cartItems = _context.Carts
                    .Where(c => c.UserId == user.Id && c.IsSelected)
                    .ToList();

                if (!cartItems.Any())
                {
                    return Json(new { success = false, message = "Cart is empty." });
                }

                model.UserId = user.Id;
                model.OrderDate = DateTime.Now;
                model.Status = "Pending";
                model.ShippingMethod ??= "Standard";
                model.PaymentMethod ??= "Cash on Delivery";

                model.TotalAmount = cartItems.Sum(c => c.Price * c.Quantity);

                _context.Orders.Add(model);
                _context.SaveChanges();

                foreach (var item in cartItems)
                {
                    var productExists = _context.Products.Any(p => p.Id == item.ProductId);

                    if (!productExists)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Product '{item.ProductName}' is no longer available."
                        });
                    }

                    var orderItem = new OrderItem
                    {
                        OrderId = model.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        ImageUrl = item.ProductImageUrl
                    };

                    _context.OrderItems.Add(orderItem);
                }

                _context.SaveChanges();

                _context.Carts.RemoveRange(cartItems);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Orders(string status = "All")
        {
            LoadCounts();

            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            int uid = userId.Value;

            var allOrders = _context.Orders
                .Where(o => o.UserId == uid)
                .Include(o => o.OrderItems)
                .ToList();

            ViewBag.TotalCount = allOrders.Count;
            ViewBag.ProcessingCount = allOrders.Count(o => o.Status == "Pending" || o.Status == "Processing");
            ViewBag.ShippedCount = allOrders.Count(o => o.Status == "Shipped");
            ViewBag.DeliveredCount = allOrders.Count(o => o.Status == "Delivered");
            ViewBag.ToReviewCount = allOrders.Count(o => o.Status == "To Review");
            ViewBag.ReturnedCount = allOrders.Count(o => o.Status == "Returned");

            var query = allOrders.AsQueryable();

            if (status != "All")
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

            return View(query.OrderByDescending(o => o.OrderDate).ToList());
        }

        [HttpPost]
        public IActionResult UpdateUserOrderStatus(int orderId, string newStatus)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            order.Status = newStatus;

            _context.SaveChanges();

            return RedirectToAction("Orders");
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
                return RedirectToAction("Login");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId.Value);

            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpGet]
        public IActionResult WriteReview(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview(int OrderId, List<ReviewInputModel> Reviews)
        {
            if (Reviews == null || !Reviews.Any())
            {
                return BadRequest("No reviews submitted.");
            }

            foreach (var r in Reviews)
            {
                var review = new Review
                {
                    ProductId = r.ProductId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    DatePosted = DateTime.Now,

                    UserName = HttpContext.Session.GetString("UserName")
                };

                _context.Reviews.Add(review);
            }

            var order = await _context.Orders.FindAsync(OrderId);
            if (order != null)
            {
                order.Status = "Reviewed";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Orders");
        }

        public IActionResult Terms(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public IActionResult Privacy(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        private void LoadUserWishlist()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            List<int> ids = new();

            if (userId.HasValue)
            {
                ids = _context.Wishlists
                    .Where(w => w.UserId == userId.Value)
                    .Select(w => w.ProductId)
                    .ToList();
            }

            ViewBag.WishlistProductIds = ids;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
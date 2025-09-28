using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Mvc.Controllers
{
    public class UserPermissionController : Controller
    {
        private readonly AppDbContext _db;

        public UserPermissionController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var perms = await _db.UserPermissions
                .Include(p => p.User)
                .Include(p => p.Document)
                .ToListAsync();

            return View(perms);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Users = _db.Users.ToList();
            ViewBag.Documents = _db.Documents.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserPermission model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Users = _db.Users.ToList();
                ViewBag.Documents = _db.Documents.ToList();
                return View(model);
            }

            _db.UserPermissions.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "User Permission berhasil ditambahkan!";
            return RedirectToAction("Index");
        }
    }
}
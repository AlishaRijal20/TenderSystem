using TenderSystem.Models;
using TenderSystem.Security;
using TenderSystem.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace TenderSystem.Controllers
{
    public class BlogController : Controller
    {
        private readonly TenderSystemContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;

        public BlogController(TenderSystemContext context, DataSecurityProvider p,
            IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;

        }
        public IActionResult Index()
        {
            var blogPosts = _context.BlogContents
                .Include(b => b.UploadUser)
                .OrderByDescending(b => b.Postdate)
                .ToList();
            return View(blogPosts);
        }

        [Route("blogpage/{activeTab?}")]
        public IActionResult BlogPage(string activeTab = "BlogList")
        {
            ViewBag.ActiveTab = activeTab;
            return PartialView("_BlogPage");
        }

        public IActionResult BlogList()
        {
            var blog = _context.BlogContents
                .Include(b => b.UploadUser)
                .Select(b => new BlogContentEdit
                {
                    Bid = b.Bid,
                    SectionHeading = b.SectionHeading,
                    SectionDescription = b.SectionDescription,
                    SectionImage = b.SectionImage,
                    Postdate = b.Postdate,
                    UploadUserId = b.UploadUserId,
                    EncId = _protector.Protect(b.Bid.ToString()),
                    FirstName = b.UploadUser.FirstName,
                    MiddleName = b.UploadUser.MiddleName,
                    LastName = b.UploadUser.LastName,
                    UserPhoto = b.UploadUser.UserPhoto
                })
                .ToList();
            return PartialView("_BlogList", blog);
        }

        public IActionResult MyBlog()
        {
            int currentUserId = Convert.ToInt16(User.Identity.Name);

            var blog = _context.BlogContents
                .Include(b => b.UploadUser)
                .Where(b => b.UploadUserId == currentUserId)
                .Select(b => new BlogContentEdit
                {
                    Bid = b.Bid,
                    SectionHeading = b.SectionHeading,
                    SectionDescription = b.SectionDescription,
                    SectionImage = b.SectionImage,
                    Postdate = b.Postdate,
                    UploadUserId = b.UploadUserId,
                    EncId = _protector.Protect(b.Bid.ToString()),
                    FirstName = b.UploadUser.FirstName,
                    MiddleName = b.UploadUser.MiddleName,
                    LastName = b.UploadUser.LastName,
                    UserPhoto = b.UploadUser.UserPhoto
                })
                .ToList();
            return PartialView("_MyBlog", blog);
        }


        public IActionResult PublishBlog()
        {
            return View("_PublishBlog");
        }

        [HttpPost]

        public ActionResult PublishBlog(BlogContentEdit edit)
        {
            //return Json(edit);
            short maxid;
            try
            {
                // any and max are Linq
                // if data is present plus 1.
                if (_context.BlogContents.Any())
                    maxid = Convert.ToInt16(_context.BlogContents.Max(x => x.Bid + 1));
                else
                    maxid = 1;
                edit.Bid = maxid;

                if (edit.BlogFile != null)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(edit.BlogFile.FileName);
                    // webRootPath is for directory 
                    string filePath = Path.Combine(_env.WebRootPath, "BlogImage", fileName);
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        edit.BlogFile.CopyTo(stream);
                    }
                    edit.SectionImage = fileName;
                }

                var currentTime = DateTime.UtcNow.AddMinutes(345);
                var currentDate = DateOnly.FromDateTime(currentTime);
                BlogContent p = new()
                {
                    Bid = edit.Bid,
                    SectionHeading = edit.SectionHeading,
                    SectionDescription = edit.SectionDescription,
                    SectionImage = edit.SectionImage,
                    Postdate = currentDate,
                    UploadUserId = Convert.ToInt16(User.Identity.Name)
                };


                _context.Add(p);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Blog posted successfully!";
                return RedirectToAction("BlogPage", "Blog");
            }
            catch
            {
                return View("_PublishBlog", edit);
            }
        }

        [HttpGet]
        public IActionResult EditBlog(string id)
        {

            int blogid;
            try
            {
                blogid = Convert.ToInt32(_protector.Unprotect(id));
            }
            catch
            {
                return BadRequest("Invalid blog ID.");
            }

            var blog = _context.BlogContents
                .Where(t => t.Bid == blogid)
                .Select(t => new BlogContentEdit
                {
                    Bid = t.Bid,
                    SectionHeading = t.SectionHeading,
                    SectionDescription = t.SectionDescription,
                    SectionImage = t.SectionImage,
                    Postdate = t.Postdate,
                    UploadUserId = t.UploadUserId,
                    EncId = _protector.Protect(t.Bid.ToString())
                })
                .FirstOrDefault();

            if (blog == null)
            {
                return NotFound("Blog not found.");
            }

            //return Json(tender);

            return View(blog);
        }

        [HttpPost]
        public async Task<IActionResult> EditBlog(BlogContentEdit b)
        {

            try
            {
                string? existingFile = _context.BlogContents
                    .Where(td => td.Bid == b.Bid)
                    .Select(td => td.SectionImage)
                    .FirstOrDefault();

                // return Json(existingFile);

                if (b.BlogFile != null)
                {
                    string fileName = "BlogImage" + Guid.NewGuid() + Path.GetExtension(b.BlogFile.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "BlogImage", fileName);

                    if (!Directory.Exists(Path.Combine(_env.WebRootPath, "BlogImage")))
                    {
                        Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "BlogImage"));
                    }

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        await b.BlogFile.CopyToAsync(stream);
                    }

                    b.SectionImage = fileName;
                }
                else
                {
                    // Retain existing document if no new file is uploaded
                    b.SectionImage = existingFile;
                }

                var blog = _context.BlogContents.FirstOrDefault(td => td.Bid == b.Bid);
                if (blog == null)
                {
                    return NotFound("Blog not found.");
                }
                // return Json(blog);
                // Updating existing blog details

                var currentTime = DateTime.UtcNow.AddMinutes(345);
                var currentDate = DateOnly.FromDateTime(currentTime);

                blog.SectionHeading = b.SectionHeading;
                blog.SectionDescription = b.SectionDescription;
                blog.SectionImage = b.SectionImage;
                blog.Postdate = currentDate;
                blog.UploadUserId = Convert.ToInt16(User.Identity!.Name);

                //return Json(blog);
                _context.Update(blog);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Blog updated successfully!";
                return RedirectToAction("BlogPage", "Blog");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while updating the blog. Please try again.");
                return View(b);
            }
        }


        [HttpGet]
        public ActionResult DeleteBlog(string id)
        {
            int blogId = Convert.ToInt32(_protector.Unprotect(id));


            var blog = _context.BlogContents
                .Where(t => t.Bid == blogId)
                .Select(t => new BlogContentEdit
                {
                    Bid = t.Bid,
                    SectionHeading = t.SectionHeading,
                    SectionDescription = t.SectionDescription,
                    SectionImage = t.SectionImage,
                    Postdate = t.Postdate,
                    UploadUserId = t.UploadUserId,
                    EncId = _protector.Protect(t.Bid.ToString())
                })
                .FirstOrDefault();

            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlogConfirmed(string id)
        {
            short blogId = Convert.ToInt16(_protector.Unprotect(id));

            var blog = await _context.BlogContents.FindAsync(blogId);
            if (blog != null)
            {
                _context.BlogContents.Remove(blog);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Blog deleted successfully!";
                return RedirectToAction("BlogPage", "Blog");
            }

            TempData["ErrorMessage"] = "Blog not found!";
            return RedirectToAction("BlogPage", "Blog");
        }
    }
}
using TenderSystem.Models;
using TenderSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;



namespace TenderSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly TenderSystemContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;


        public HomeController(TenderSystemContext context, DataSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
        }


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult TermsAndConditions()
        {
            return View();
        }

        //partial view
        public IActionResult ProfileImage()
        {
            var p = _context.UserLists.Where(u => u.UserId.Equals(Convert.ToInt16(User.Identity!.Name))).FirstOrDefault();
            ViewData["img"] = p.UserPhoto;
            return PartialView("_Profile");
        }

        [HttpGet]
        public IActionResult ProfilePage()
        {
            // Get the current user ID from the logged-in user's identity
            int currentUserID = Convert.ToInt16(User.Identity!.Name);

            // Retrieve the user's details from the UserList table
            var user = _context.UserLists
                .Where(u => u.UserId == currentUserID)
                .Select(user => new UserListEdit
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName,
                    MiddleName = user.MiddleName,
                    LastName = user.LastName,
                    Province = user.Province,
                    District = user.District,
                    City = user.City,
                    EmailAddress = user.EmailAddress,
                    Phone = user.Phone,
                    UserPhoto = user.UserPhoto,
                    Gender = user.Gender,
                    UserRole = user.UserRole,
                    UserPassword = user.UserPassword
                })
                .FirstOrDefault();

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Retrieve the associated company details, if available
            var company = _context.Companies
                .Where(c => c.UserbidId == currentUserID)
                .Select(company => new CompanyEdit
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName,
                    FullAddress = company.FullAddress,
                    OfficeEmail = company.OfficeEmail,
                    CompanyWebsiteUrl = company.CompanyWebsiteUrl,
                    RegistrationNumber = company.RegistrationNumber,
                    RegistrationDocument = company.RegistrationDocument,
                    PanNumber = company.PanNumber,
                    PanDocument = company.PanDocument,
                    CompanyType = company.CompanyType
                })
                .FirstOrDefault();

            // Retrieve the associated bank details, if available
            var bank = _context.Banks
                .Where(b => b.UserbankId == currentUserID)
                .Select(bank => new BankEdit
                {
                    BankId = bank.BankId,
                    BankName = bank.BankName,
                    AccountNumber = bank.AccountNumber
                })
                .FirstOrDefault();

            // Combine the results in a view model
            var profileViewModel = new ProfileViewModel
            {
                User = user,
                Company = company,
                Bank = bank
            };

            return View(profileViewModel);
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            // Get the current user ID from the logged-in user's identity
            int currentUserID = Convert.ToInt16(User.Identity!.Name);

            // Retrieve the user's details from the UserList table
            var user = _context.UserLists
                .Where(u => u.UserId == currentUserID)
                .Select(user => new UserListEdit
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName,
                    MiddleName = user.MiddleName,
                    LastName = user.LastName,
                    Province = user.Province,
                    District = user.District,
                    City = user.City,
                    Gender = user.Gender,
                    UserRole = user.UserRole,
                    EmailAddress = user.EmailAddress,
                    Phone = user.Phone,
                    UserPhoto = user.UserPhoto,
                    UserPassword = user.UserPassword
                })
                .FirstOrDefault();

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Retrieve the associated company details, if available
            var company = _context.Companies
                .Where(c => c.UserbidId == currentUserID)
                .Select(company => new CompanyEdit
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName,
                    FullAddress = company.FullAddress,
                    OfficeEmail = company.OfficeEmail,
                    CompanyWebsiteUrl = company.CompanyWebsiteUrl,
                    RegistrationNumber = company.RegistrationNumber,
                    RegistrationDocument = company.RegistrationDocument,
                    PanNumber = company.PanNumber,
                    PanDocument = company.PanDocument,
                    CompanyType = company.CompanyType
                })
                .FirstOrDefault();

            // Retrieve the associated bank details, if available
            var bank = _context.Banks
                .Where(b => b.UserbankId == currentUserID)
                .Select(bank => new BankEdit
                {
                    BankId = bank.BankId,
                    BankName = bank.BankName,
                    AccountNumber = bank.AccountNumber
                })
                .FirstOrDefault();
            // Combine the results in a view model for editing
            var editProfileViewModel = new UserListEdit
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Province = user.Province,
                District = user.District,
                City = user.City,
                Gender = user.Gender,
                UserRole = user.UserRole,
                Phone = user.Phone,
                EmailAddress = user.EmailAddress,
                UserPhoto = user.UserPhoto,
                CompanyName = company?.CompanyName,
                Position = company?.Position,
                BankName = bank?.BankName,
                AccountNumber = bank?.AccountNumber
            };

            return View(editProfileViewModel);
        }


        [HttpPost]
        public async Task<IActionResult> EditProfile(ProfileViewModel model)
        {
            //return Json(model);
            // Get the current user ID from the logged-in user's identity
            int currentUserID = Convert.ToInt16(User.Identity!.Name);


            if (model.User.UserFile != null)
            {
                string fileName = "UserImage" + Guid.NewGuid() + Path.GetExtension(model.User.UserFile.FileName);
                string filePath = Path.Combine(_env.WebRootPath, "UserImage", fileName);

                // Ensure the directory exists
                if (!Directory.Exists(Path.Combine(_env.WebRootPath, "UserImage")))
                {
                    Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "UserImage"));
                }

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.User.UserFile.CopyToAsync(stream);
                }

                model.User.UserPhoto = fileName;
            }

            UserList users = new()
            {
                // Update user details
                UserId = (short)currentUserID,
                FirstName = model.User.FirstName,
                MiddleName = model.User.MiddleName,
                LastName = model.User.LastName,
                Province = model.User.Province,
                District = model.User.District,
                City = model.User.City,
                Phone = model.User.Phone,
                Gender = model.User.Gender,
                EmailAddress = model.User.EmailAddress,
                UserPhoto = model.User.UserPhoto,
                UserRole = model.User.UserRole,
                UserPassword = model.User.UserPassword
            };


            // Save changes to the UserList table
            _context.Update(users);
            //return Json(users);



            // Save all changes to the database
            await _context.SaveChangesAsync();

            // Redirect to the profile page or any confirmation page
            return RedirectToAction("ProfilePage");

        }




    }
}
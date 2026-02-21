using TenderSystem.Models;
using TenderSystem.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text;

// register login with email verification, otp sabai kaam sakiyo
namespace TenderSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly TenderSystemContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;


        public AccountController(TenderSystemContext context, DataSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
        }


        // Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(UserListEdit u)
        {
            //return Json(u);
            try
            {
                if (!ModelState.IsValid)
                    return View(u);

                var existingUser = _context.UserLists.FirstOrDefault(x => x.EmailAddress == u.EmailAddress);
                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = "User already exists with this email!";
                    return View(u);
                }

                // Save files to temp location
                if (u.UserFile != null)
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), $"user_{Guid.NewGuid()}");
                    using (var stream = System.IO.File.Create(tempPath))
                    {
                        await u.UserFile.CopyToAsync(stream);
                    }
                    HttpContext.Session.SetString("UserTempPath", tempPath);
                    HttpContext.Session.SetString("UserFileName", u.UserFile.FileName);
                }

                // Store user data in session
                var userData = new
                {
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    u.Phone,
                    u.EmailAddress,
                    u.Province,
                    u.District,
                    u.City,
                    u.Gender,
                    u.UserPassword,
                    u.UserRole,
                };

                //return Json(userData);

                HttpContext.Session.SetString("UserData", JsonSerializer.Serialize(userData));

                // Generate and send OTP
                var otp = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("RegisterOTP", otp);

                // Send OTP via email
                SmtpClient s = new()
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    Credentials = new NetworkCredential("rijalalisha20@gmail.com", "kcnl yedt kxiz gikk"),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                MailMessage m = new()
                {
                    From = new MailAddress("rijalalisha20@gmail.com"),
                    Subject = "Email Verification for Registration",
                    Body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Verification</title>
    <style>
        /* Base styles */
        body, html {{
            margin: 0;
            padding: 0;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 4px 10px rgba(0, 0, 0, 0.05);
        }}
        .header {{
            background: linear-gradient(135deg, #1e40af 0%, #1e3a8a 100%);
            padding: 30px 20px;
            text-align: center;
            color: white;
        }}
        .content {{
            padding: 40px 30px;
            background-color: #ffffff;
        }}
        .footer {{
            background-color: #f8f8f8;
            padding: 15px;
            text-align: center;
            font-size: 12px;
            color: #666;
            border-top: 1px solid #ddd;
        }}
        .token-box {{
            background-color: #f1f5f9;
            padding: 15px;
            border-radius: 6px;
            font-family: monospace;
            font-size: 24px;
            font-weight: bold;
            text-align: center;
            margin: 20px 0;
            color: #1e40af;
        }}
        .security-note {{
            background-color: #f8fafc;
            border-left: 4px solid #3b82f6;
            padding: 15px;
            margin: 20px 0;
            font-size: 14px;
            color: #64748b;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin:0;'>Email Verification</h1>
        </div>
        <div class='content'>
            <h2 style='color:#0056b3;'>Welcome to Tender System</h2>
            <p>Thank you for registering with  Tender System. Please use the verification code below to complete your registration.</p>
            
            <div class='token-box'>
                {otp}
            </div>
            
            <div class='security-note'>
                <strong>Security Tip:</strong> This code will expire after use. 
                Do not share this code with anyone. Tender System representatives will never ask for this code.
            </div>
            
            <p>If you did not request this registration, please ignore this email or contact our support team.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from Tender System. Please do not reply to this email.</p>
            <p>&copy; {DateTime.Now.Year}  Tender System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>",
                    IsBodyHtml = true,
                };

                m.To.Add(u.EmailAddress);
                s.Send(m);

                return RedirectToAction("VerifyRegistration", new { email = u.EmailAddress });
            }
            catch (Exception ex)
            {
                CleanTempFiles();
                ModelState.AddModelError("", "Registration failed. Please try again.");
                return View(u);
            }
        }



        [HttpGet]
        public IActionResult VerifyRegistration(string email)
        {
            return View(new UserListEdit { EmailAddress = email });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyRegistration(UserListEdit model)
        {
            try
            {
                var storedOTP = HttpContext.Session.GetString("RegisterOTP");
                var userDataJson = HttpContext.Session.GetString("UserData");

                if (storedOTP != model.EmailToken || string.IsNullOrEmpty(userDataJson))
                {
                    ModelState.AddModelError("", "Invalid verification code");
                    return View(model);
                }

                var userData = JsonSerializer.Deserialize<UserListEdit>(userDataJson);

                // Generate user ID
                short userId = _context.UserLists.Any()
                    ? (short)(_context.UserLists.Max(x => x.UserId) + 1)
                    : (short)1;

                // Handle user photo
                string userPhotoPath = null;
                if (HttpContext.Session.TryGetValue("UserTempPath", out var userTempPath))
                {
                    var tempPath = Encoding.UTF8.GetString(userTempPath);
                    var fileName = $"user_{userId}{Path.GetExtension(HttpContext.Session.GetString("UserFileName"))}";
                    userPhotoPath = await SaveFileToPermanentLocation(tempPath, "UserImage", fileName);
                }

                // Create and save user
                var user = new UserList
                {
                    UserId = userId,
                    FirstName = userData.FirstName,
                    MiddleName = userData.MiddleName,
                    LastName = userData.LastName,
                    Phone = userData.Phone,
                    EmailAddress = userData.EmailAddress,
                    Province = userData.Province,
                    District = userData.District,
                    City = userData.City,
                    Gender = userData.Gender,
                    UserPhoto = userPhotoPath,
                    UserPassword = _protector.Protect(userData.UserPassword),
                    UserRole = userData.UserRole
                };

                //return Json(user);
                _context.UserLists.Add(user);
                await _context.SaveChangesAsync();


                CleanTempFiles();
                ClearSessionData();

                TempData["SuccessMessage"] = "Registration successful! You can now log in.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                CleanTempFiles();
                ModelState.AddModelError("", "Registration verification failed. Please try again.");
                return View(model);
            }
        }


        private async Task<string> SaveFileToPermanentLocation(string tempPath, string folder, string fileName)
        {
            var permPath = Path.Combine(_env.WebRootPath, folder, fileName);

            if (!Directory.Exists(Path.Combine(_env.WebRootPath, folder)))
            {
                Directory.CreateDirectory(Path.Combine(_env.WebRootPath, folder));
            }

            using (var sourceStream = System.IO.File.OpenRead(tempPath))
            using (var destinationStream = System.IO.File.Create(permPath))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }

            System.IO.File.Delete(tempPath);
            return fileName;
        }

        private void CleanTempFiles()
        {
            var keys = new[] { "UserTempPath" };
            foreach (var key in keys)
            {
                if (HttpContext.Session.TryGetValue(key, out var pathBytes))
                {
                    var path = Encoding.UTF8.GetString(pathBytes);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }
            }
        }

        private void ClearSessionData()
        {
            var keys = new[] { "RegisterOTP", "UserData", "UserFileName" };
            foreach (var key in keys)
            {
                HttpContext.Session.Remove(key);
            }
        }

        //login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserListEdit uEdit)
        {
            var users = _context.UserLists.ToList();
            if (users != null)
            {
                var u = users.Where(x => x.EmailAddress.ToUpper().Equals(uEdit.EmailAddress.ToUpper()) && _protector.Unprotect(x.UserPassword).Equals(uEdit.UserPassword)).FirstOrDefault();

                if (u != null)
                {
                    List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, u.UserId.ToString()),
                new Claim(ClaimTypes.Role, u.UserRole),

                new Claim("Role", u.UserRole),
                new Claim("FullName", u.FirstName),
                new Claim("image", u.UserPhoto),
                new Claim("email", u.EmailAddress),
                // new Claim("address",u.CurrentAddress),
            };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));

                    return RedirectToAction("Dashboard");
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid email or password.";
                }
            }
            else
            {
                ModelState.AddModelError("", "Invalid User");

            }
            return View(uEdit);
        }




        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (User.IsInRole("Publisher"))
            {
                return RedirectToAction("Index", "PublisherTender");
            }
            else
            {
                return RedirectToAction("Index", "BidTender");
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public IActionResult ChangePassword(ChangePassword c)
        {
            var u = _context.UserLists.Where(e => e.UserId == Convert.ToInt16(User.Identity!.Name)).First();
            if (_protector.Unprotect(u.UserPassword) != c.CurrentPassword)
            {
                ModelState.AddModelError("", "Check your current password");
            }
            else
            {
                if (c.NewPassword == c.ConfirmPassword)
                {
                    u.UserPassword = _protector.Protect(c.NewPassword);
                    _context.Update(u);
                    _context.SaveChanges();

                    // Add a success message to TempData
                    TempData["Success"] = "Your password has been changed successfully!";
                    return View();
                }
                else
                {
                    ModelState.AddModelError("", "Confirm Password does not match");
                    return View(c);
                }
            }

            TempData["Error"] = "Password change failed. Please try again!";
            return View();
        }


        [HttpGet]

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(UserListEdit edit)
        {

            if (edit.EmailAddress != null)
            {
                Random r = new Random();
                HttpContext.Session.SetString("token", r.Next(9999).ToString());
                var token = HttpContext.Session.GetString("token");
                var user = _context.UserLists.Where(u => u.EmailAddress == edit.EmailAddress).FirstOrDefault();
                if (user != null)
                {
                    SmtpClient s = new()
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        Credentials = new NetworkCredential("rijalalisha20@gmail.com", "kcnl yedt kxiz gikk"),
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network
                    };

                    MailMessage m = new()
                    {
                        From = new MailAddress("rijalalisha20@gmail.com"),
                        Subject = "Reset Your Tender System Password",
                        Body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset Request</title>
    <style>
        /* Base styles */
        body, html {{
            margin: 0;
            padding: 0;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 4px 10px rgba(0, 0, 0, 0.05);
        }}
        .header {{
            background: linear-gradient(135deg, #1e40af 0%, #1e3a8a 100%);
            padding: 30px 20px;
            text-align: center;
            color: white;
        }}
        .content {{
            padding: 40px 30px;
            background-color: #ffffff;
        }}
        .footer {{
            background-color: #f8f8f8;
            padding: 15px;
            text-align: center;
            font-size: 12px;
            color: #666;
            border-top: 1px solid #ddd;
        }}
        .button {{
            display: inline-block;
            background-color: #0056b3;
            color: white;
            padding: 10px 20px;
            text-decoration: none;
            border-radius: 4px;
            margin: 15px 0;
        }}
        .info-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 15px 0;
        }}
        .info-table td {{
            padding: 8px;
            border-bottom: 1px solid #eee;
        }}
        .info-table td:first-child {{
            font-weight: bold;
            width: 140px;
        }}
        .security-note {{
            background-color: #f8fafc;
            border-left: 4px solid #3b82f6;
            padding: 15px;
            margin: 20px 0;
            font-size: 14px;
            color: #64748b;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin:0;'>Password Reset Request</h1>
        </div>
        <div class='content'>
            <h2 style='color:#0056b3;'>Reset Your Password</h2>
            <p>We received a request to reset your password for your Tender System account.</p>
            
            <table class='info-table'>
                <tr>
                    <td>Account Email:</td>
                    <td>{user.EmailAddress}</td>
                </tr>
                <tr>
                    <td>Request Time:</td>
                    <td>{DateTime.Now.ToString("dd MMM yyyy, HH:mm")}</td>
                </tr>
            </table>
           
            
            <div style='text-align: center;'>
                <strong>Your verification code:</strong> {token}
            </div>
            
           <div class='security-note'>
                <strong>Security Tip:</strong> For your protection, please never share this email 
                or your password reset link with anyone. Tender System representatives will never ask 
                for your password or this reset token.
                <br><br>
                
            </div>
            
            <p>If you did not request a password reset, please ignore this email or contact our support team if you have concerns about your account security.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from Tender System. Please do not reply to this email.</p>
            <p>&copy; {DateTime.Now.Year} Tender System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>",
                        IsBodyHtml = true,
                    };


                    m.To.Add(user.EmailAddress);
                    s.Send(m);
                    // return Json("success");
                    return RedirectToAction("VerifyToken", new { email = user.EmailAddress });

                }
                else
                {
                    ModelState.AddModelError("", "This Email is not registered Email.");
                    return View(edit);
                }
            }
            return Json("Failed");
        }

        [HttpGet]
        public IActionResult VerifyToken(string email)
        {
            return View(new UserListEdit { EmailAddress = email });
        }

        [HttpPost]
        public IActionResult VerifyToken(UserListEdit e)
        {
            var token = HttpContext.Session.GetString("token");
            if (token == e.EmailToken)
            {
                var et = _protector.Protect(e.EmailToken!);
                return RedirectToAction("ResetPassword", new UserListEdit { EmailAddress = e.EmailAddress, EmailToken = et });
            }
            else
            {
                ModelState.AddModelError("", "Invalid verification code");
                return View(e);
            }
        }


        [HttpGet]
        public IActionResult ResetPassword(UserListEdit e)
        {
            try
            {
                // return Json(e);
                var token = HttpContext.Session.GetString("token");
                var eToken = _protector.Unprotect(e.EmailToken);
                if (token == eToken)
                {
                    return View(new ChangePassword { EmailAddress = e.EmailAddress });
                }
                else
                {
                    return RedirectToAction("ForgotPassword");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("ForgotPassword");
            }
        }


        [HttpPost]
        public IActionResult ResetPassword(ChangePassword model)
        {


            if (model.NewPassword == model.ConfirmPassword)
            {
                var user = _context.UserLists.FirstOrDefault(u => u.EmailAddress == model.EmailAddress);
                if (user != null)
                {
                    user.UserPassword = _protector.Protect(model.NewPassword);
                    _context.Update(user);
                    _context.SaveChanges();
                    return RedirectToAction("Login");
                }
            }
            else
            {
                ModelState.AddModelError("", "Passwords do not match");
                return View(model);
            }


            // return RedirectToAction("ForgotPassword");
            return Json("error");
        }
    }

}
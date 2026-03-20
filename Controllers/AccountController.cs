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
                            <style>
                                body {{ font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 0; background: #f4f6f9; color: #333; }}
                                .wrapper {{ max-width: 620px; margin: 30px auto; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(11,31,58,.15); }}
                                .header {{ background: #0B1F3A; padding: 32px 28px; text-align: center; }}
                                .header-badge {{ display: inline-block; background: rgba(200,150,12,.15); border: 1px solid rgba(200,150,12,.3); color: #E8B84B; font-size: 11px; font-weight: 600; letter-spacing: .12em; text-transform: uppercase; padding: 4px 14px; border-radius: 999px; margin-bottom: 12px; }}
                                .header h1 {{ margin: 0; font-size: 22px; font-weight: 700; color: #ffffff; }}
                                .header p {{ margin: 8px 0 0; font-size: 13px; color: #8A9BB5; }}
                                .gold-line {{ height: 3px; background: linear-gradient(90deg, transparent, #C8960C 30%, #E8B84B 50%, #C8960C 70%, transparent); }}
                                .content {{ background: #ffffff; padding: 32px 28px; }}
                                .content h2 {{ font-size: 17px; font-weight: 700; color: #0B1F3A; margin: 0 0 10px; }}
                                .content p {{ font-size: 14px; line-height: 1.7; color: #5a6a80; margin: 0 0 16px; }}
                                .otp-box {{ background: #0B1F3A; border-radius: 12px; padding: 24px 20px; text-align: center; margin: 24px 0; }}
                                .otp-label {{ font-size: 11px; font-weight: 600; letter-spacing: .15em; text-transform: uppercase; color: #8A9BB5; margin-bottom: 10px; }}
                                .otp-code {{ font-family: 'Courier New', monospace; font-size: 36px; font-weight: 700; color: #E8B84B; letter-spacing: .25em; }}
                                .security-note {{ background: #F7F3EC; border-left: 3px solid #C8960C; border-radius: 0 8px 8px 0; padding: 13px 16px; font-size: 13.5px; color: #5a6a80; line-height: 1.6; margin: 20px 0; }}
                                .security-note strong {{ color: #0B1F3A; }}
                                .footer {{ background: #0B1F3A; padding: 18px 28px; text-align: center; }}
                                .footer p {{ margin: 4px 0; font-size: 12px; color: #8A9BB5; }}
                                .footer .brand {{ font-size: 13px; font-weight: 600; color: #C8960C; letter-spacing: .05em; }}
                            </style>
                            </head>
                            <body>
                            <div class='wrapper'>

                                <div class='header'>
                                    <div class='header-badge'>Account Verification</div>
                                    <h1>Email Verification</h1>
                                    <p>Nepal Public Procurement Portal</p>
                                </div>
                                <div class='gold-line'></div>

                                <div class='content'>
                                    <h2>Welcome to Nepal Public Procurement Portal</h2>
                                    <p>Thank you for registering. Please use the verification code below to complete your registration. Enter this code on the verification page to activate your account.</p>

                                    <div class='otp-box'>
                                        <div class='otp-label'>Your Verification Code</div>
                                        <div class='otp-code'>{otp}</div>
                                    </div>

                                    <div class='security-note'>
                                        <strong>&#128274; Security Notice:</strong> This code expires after use. Never share it with anyone — Nepal Public Procurement Portal representatives will never ask for this code.
                                    </div>

                                    <p>If you did not request this registration, please ignore this email or contact our support team immediately.</p>
                                </div>

                                <div class='footer'>
                                    <p class='brand'>Nepal Public Procurement Portal</p>
                                    <p>This is an automated message. Please do not reply to this email.</p>
                                    <p>&copy; {DateTime.Now.Year} Nepal Public Procurement Portal. All rights reserved.</p>
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
                            <style>
                                body {{ font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 0; background: #f4f6f9; color: #333; }}
                                .wrapper {{ max-width: 620px; margin: 30px auto; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(11,31,58,.15); }}
                                .header {{ background: #0B1F3A; padding: 32px 28px; text-align: center; }}
                                .header-badge {{ display: inline-block; background: rgba(200,150,12,.15); border: 1px solid rgba(200,150,12,.3); color: #E8B84B; font-size: 11px; font-weight: 600; letter-spacing: .12em; text-transform: uppercase; padding: 4px 14px; border-radius: 999px; margin-bottom: 12px; }}
                                .header h1 {{ margin: 0; font-size: 22px; font-weight: 700; color: #ffffff; }}
                                .header p {{ margin: 8px 0 0; font-size: 13px; color: #8A9BB5; }}
                                .gold-line {{ height: 3px; background: linear-gradient(90deg, transparent, #C8960C 30%, #E8B84B 50%, #C8960C 70%, transparent); }}
                                .content {{ background: #ffffff; padding: 32px 28px; }}
                                .content h2 {{ font-size: 17px; font-weight: 700; color: #0B1F3A; margin: 0 0 10px; }}
                                .content p {{ font-size: 14px; line-height: 1.7; color: #5a6a80; margin: 0 0 16px; }}
                                .info-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; border-radius: 10px; overflow: hidden; border: 1px solid rgba(11,31,58,.07); }}
                                .info-table tr:nth-child(odd) td {{ background: #F7F3EC; }}
                                .info-table tr:nth-child(even) td {{ background: #ffffff; }}
                                .info-table td {{ padding: 11px 14px; font-size: 13.5px; border-bottom: 1px solid rgba(11,31,58,.05); color: #333; }}
                                .info-table td:first-child {{ font-weight: 700; color: #0B1F3A; width: 150px; }}
                                .token-box {{ background: #0B1F3A; border-radius: 12px; padding: 24px 20px; text-align: center; margin: 24px 0; }}
                                .token-label {{ font-size: 11px; font-weight: 600; letter-spacing: .15em; text-transform: uppercase; color: #8A9BB5; margin-bottom: 10px; }}
                                .token-code {{ font-family: 'Courier New', monospace; font-size: 36px; font-weight: 700; color: #E8B84B; letter-spacing: .25em; }}
                                .security-note {{ background: #F7F3EC; border-left: 3px solid #C8960C; border-radius: 0 8px 8px 0; padding: 13px 16px; font-size: 13.5px; color: #5a6a80; line-height: 1.6; margin: 20px 0; }}
                                .security-note strong {{ color: #0B1F3A; }}
                                .footer {{ background: #0B1F3A; padding: 18px 28px; text-align: center; }}
                                .footer p {{ margin: 4px 0; font-size: 12px; color: #8A9BB5; }}
                                .footer .brand {{ font-size: 13px; font-weight: 600; color: #C8960C; letter-spacing: .05em; }}
                            </style>
                            </head>
                            <body>
                            <div class='wrapper'>

                                <div class='header'>
                                    <div class='header-badge'>Account Security</div>
                                    <h1>Password Reset Request</h1>
                                    <p>Nepal Public Procurement Portal</p>
                                </div>
                                <div class='gold-line'></div>

                                <div class='content'>
                                    <h2>Reset Your Password</h2>
                                    <p>We received a request to reset the password for your account. Use the verification code below to proceed. If you did not make this request, you can safely ignore this email.</p>

                                    <table class='info-table'>
                                        <tr>
                                            <td>&#128231; Account Email</td>
                                            <td>{user.EmailAddress}</td>
                                        </tr>
                                        <tr>
                                            <td>&#128336; Request Time</td>
                                            <td>{DateTime.Now.ToString("dd MMM yyyy, HH:mm")}</td>
                                        </tr>
                                    </table>

                                    <div class='token-box'>
                                        <div class='token-label'>Your Reset Code</div>
                                        <div class='token-code'>{token}</div>
                                    </div>

                                    <div class='security-note'>
                                        <strong>&#128274; Security Notice:</strong> Never share this code with anyone — Nepal Public Procurement Portal representatives will never ask for your password or this reset token. This code expires after use.
                                    </div>

                                    <p>If you did not request a password reset, please ignore this email or contact our support team immediately if you have concerns about your account security.</p>
                                </div>

                                <div class='footer'>
                                    <p class='brand'>Nepal Public Procurement Portal</p>
                                    <p>This is an automated message. Please do not reply to this email.</p>
                                    <p>&copy; {DateTime.Now.Year} Nepal Public Procurement Portal. All rights reserved.</p>
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
using TenderSystem.Models;
using TenderSystem.Security;
using TenderSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TenderSystem.Controllers
{
    [Authorize(Roles = "Bidder")]
    public class KycController : Controller
    {
        private readonly TenderSystemContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;


        public KycController(TenderSystemContext context, DataSecurityProvider p,
             IDataProtectionProvider provider, IWebHostEnvironment env,
             EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
            _emailService = emailService;
            _configuration = configuration;
        }
        public IActionResult RegisterCompany()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RegisterCompany(UserListEdit u)
        {
            //return Json(u);  
            try
            {
                var existingCompanyEmail = _context.Companies.FirstOrDefault(x => x.OfficeEmail == u.OfficeEmail);
                if (existingCompanyEmail != null)
                {
                    TempData["ErrorMessage"] = "Company already exists with this email!";
                    return View(u);
                }

                var existingRegisterNumber = _context.Companies.FirstOrDefault(x => x.RegistrationNumber == u.RegistrationNumber);
                if (existingRegisterNumber != null)
                {
                    TempData["ErrorMessage"] = "Company already exists with this registration number!";
                    return View(u);
                }

                var existingPanNumber = _context.Companies.FirstOrDefault(x => x.PanNumber == u.PanNumber);
                if (existingPanNumber != null)
                {
                    TempData["ErrorMessage"] = "Company already exists with this pan number!";
                    return View(u);
                }

                var existingAccountNumber = _context.Banks.FirstOrDefault(x => x.AccountNumber == u.AccountNumber);
                if (existingAccountNumber != null)
                {
                    TempData["ErrorMessage"] = "Bank Account already exists with this account number!";
                    return View(u);
                }

                //return Json(u);
                short bidderid;
                if (_context.Companies.Any())
                    bidderid = Convert.ToInt16(_context.Companies.Max(x => x.CompanyId) + 1);
                else
                    bidderid = 1;
                u.CompanyId = bidderid;

                short bankid;
                if (_context.Companies.Any())
                    bankid = Convert.ToInt16(_context.Banks.Max(x => x.BankId) + 1);
                else
                    bankid = 1;
                u.BankId = bankid;

                if (u.RegisterFile != null)
                {
                    string fileName = "RegisterImage" + Guid.NewGuid() + Path.GetExtension(u.RegisterFile.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "RegisterImage", fileName);
                    // Ensure the EmpImage directory exists
                    if (!Directory.Exists(Path.Combine(_env.WebRootPath, "RegisterImage")))
                    {
                        Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "RegisterImage"));
                    }
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        u.RegisterFile.CopyTo(stream);
                    }
                    u.RegistrationDocument = fileName;
                }

                if (u.PanFile != null)
                {
                    string fileName = "PanImage" + Guid.NewGuid() + Path.GetExtension(u.PanFile.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "PanImage", fileName);
                    // Ensure the EmpImage directory exists
                    if (!Directory.Exists(Path.Combine(_env.WebRootPath, "PanImage")))
                    {
                        Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "PanImage"));
                    }
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        u.PanFile.CopyTo(stream);
                    }
                    u.PanDocument = fileName;
                }


                var userId = Convert.ToInt16(User.Identity.Name);

                Company company = new()
                {
                    CompanyId = u.CompanyId,
                    UserbidId = userId,
                    CompanyName = u.CompanyName,
                    FullAddress = u.FullAddress,
                    OfficeEmail = u.OfficeEmail,
                    CompanyWebsiteUrl = u.CompanyWebsiteUrl,
                    CompanyType = u.CompanyType,
                    RegistrationDocument = u.RegistrationDocument,
                    PanDocument = u.PanDocument,
                    RegistrationNumber = u.RegistrationNumber,
                    PanNumber = u.PanNumber,
                    Rating = u.Rating,
                    Position = u.Position,
                    IsVerified = false,
                };
                //return Json(company);
                Bank bank = new()
                {
                    BankId = u.BankId,
                    BankName = u.BankName,
                    AccountNumber = u.AccountNumber,
                    AccountHolderName = u.AccountHolderName,
                    AccountType = u.AccountType,
                    UserbankId = userId,
                    IsVerified = false,
                };

                //return Json(bank);
                _context.Companies.Add(company);
                _context.Banks.Add(bank);
                await _context.SaveChangesAsync();
                try
                {
                    var adminEmail = _configuration.GetValue<string>("EmailSettings:AdminEmail");
                    if (string.IsNullOrEmpty(adminEmail))
                    {
                        throw new Exception("Admin email is not configured in appsettings.json");
                    }

                    var subject = "New KYC Registration Requires Verification";
                    var body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>KYC Verification Required</title>
                    <style>
                        body {{
                            font-family: 'Segoe UI', Arial, sans-serif;
                            line-height: 1.6;
                            color: #333;
                            margin: 0;
                            padding: 0;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 0 auto;
                            padding: 20px;
                        }}
                        .header {{
                            background-color: #0056b3;
                            padding: 20px;
                            text-align: center;
                            color: white;
                            border-top-left-radius: 5px;
                            border-top-right-radius: 5px;
                        }}
                        .content {{
                            background-color: #ffffff;
                            padding: 20px;
                            border-left: 1px solid #ddd;
                            border-right: 1px solid #ddd;
                        }}
                        .footer {{
                            background-color: #f8f8f8;
                            padding: 15px;
                            text-align: center;
                            font-size: 12px;
                            color: #666;
                            border-bottom-left-radius: 5px;
                            border-bottom-right-radius: 5px;
                            border: 1px solid #ddd;
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
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1 style='margin:0;'>KYC Verification</h1>
                        </div>
                        <div class='content'>
                            <h2 style='color:#0056b3;'>New KYC Registration Requires Verification</h2>
                            <p>A new company has registered in the system and requires your verification before they can participate in tenders.</p>
        
                            <table class='info-table'>
                                <tr>
                                    <td>Company ID:</td>
                                    <td>{company.CompanyId}</td>
                                </tr>
                                <tr>
                                    <td>Company Name:</td>
                                    <td>{company.CompanyName}</td>
                                </tr>
                                <tr>
                                    <td>Company Type:</td>
                                    <td>{company.CompanyType}</td>
                                </tr>
                                <tr>
                                    <td>Registration Number:</td>
                                    <td>{company.RegistrationNumber}</td>
                                </tr>
                                <tr>
                                    <td>Date Registered:</td>
                                    <td>{DateTime.Now.ToString("dd MMM yyyy, HH:mm")}</td>
                                </tr>
                            </table>
        
                            <p>Please review this KYC registration for accuracy and compliance with organizational guidelines.</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message from the BidNetra. Please do not reply to this email.</p>
                            <p>© 2025 BidNetra. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

                    await _emailService.SendEmailAsync(adminEmail, subject, body);
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"Email sending failed: {emailEx.Message}");
                }

                TempData["SuccessMessage"] = "Registration successful! Your KYC is pending verification.";
                return RedirectToAction("Index", "BidTender");
            }
            catch
            {
                ModelState.AddModelError("", " Registration Failed. Please try again");
                return View(u);
            }
        }


        [Authorize(Roles = "Bidder")]

        public IActionResult KycDetails()
        {
            int currentUserId = Convert.ToInt16(User.Identity!.Name);

            var kycDetails = _context.UserLists
                .Where(u => u.UserId == currentUserId)
                .Select(u => new UserListEdit
                {
                    // User details
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    Province = u.Province,
                    District = u.District,
                    City = u.City,
                    Gender = u.Gender,
                    Phone = u.Phone,
                    EmailAddress = u.EmailAddress,
                    UserPhoto = u.UserPhoto,
                    UserRole = u.UserRole,

                    // Company details
                    CompanyName = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().CompanyName : null,
                    FullAddress = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().FullAddress : null,
                    OfficeEmail = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().OfficeEmail : null,
                    CompanyWebsiteUrl = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().CompanyWebsiteUrl : null,
                    RegistrationNumber = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().RegistrationNumber : null,
                    RegistrationDocument = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().RegistrationDocument : null,
                    PanNumber = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().PanNumber : null,
                    PanDocument = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().PanDocument : null,
                    CompanyType = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().CompanyType : null,
                    Position = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().Position : null,
                    Rating = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().Rating : null,
                    IsVerified = u.Companies.FirstOrDefault() != null ? u.Companies.FirstOrDefault().IsVerified : false,

                    // Bank details
                    BankName = u.Banks.FirstOrDefault() != null ? u.Banks.FirstOrDefault().BankName : null,
                    AccountNumber = u.Banks.FirstOrDefault() != null ? u.Banks.FirstOrDefault().AccountNumber : null,
                    AccountType = u.Banks.FirstOrDefault() != null ? u.Banks.FirstOrDefault().AccountType : null,
                    AccountHolderName = u.Banks.FirstOrDefault() != null ? u.Banks.FirstOrDefault().AccountHolderName : null
                })
                .FirstOrDefault();

            if (kycDetails == null || kycDetails.CompanyName == null || kycDetails.BankName == null)
            {
                return RedirectToAction("RegisterCompany");
            }

            return View(kycDetails);
        }


        [Authorize(Roles = "Bidder")]
        [HttpGet]
        public IActionResult UpdateKyc()
        {
            var userId = Convert.ToInt16(User.Identity.Name);

            // Get existing company and bank details
            var company = _context.Companies
                .FirstOrDefault(c => c.UserbidId == userId);
            var bank = _context.Banks
                .FirstOrDefault(b => b.UserbankId == userId);

            if (company == null || bank == null)
            {
                return RedirectToAction("RegisterCompany");
            }

            var model = new UserListEdit
            {
                // Company details
                CompanyId = company.CompanyId,
                CompanyName = company.CompanyName,
                FullAddress = company.FullAddress,
                OfficeEmail = company.OfficeEmail,
                CompanyWebsiteUrl = company.CompanyWebsiteUrl,
                CompanyType = company.CompanyType,
                RegistrationNumber = company.RegistrationNumber,
                PanNumber = company.PanNumber,
                Position = company.Position,
                Rating = company.Rating,

                // Bank details
                BankId = bank.BankId,
                BankName = bank.BankName,
                AccountNumber = bank.AccountNumber,
                AccountHolderName = bank.AccountHolderName,
                AccountType = bank.AccountType
            };

            return View(model);
        }

        [Authorize(Roles = "Bidder")]
        [HttpPost]
        public async Task<IActionResult> UpdateKyc(UserListEdit u)
        {
            try
            {
                var userId = Convert.ToInt16(User.Identity.Name);

                // Get existing company and bank
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserbidId == userId);
                var bank = await _context.Banks
                    .FirstOrDefaultAsync(b => b.UserbankId == userId);

                if (company == null || bank == null)
                {
                    TempData["ErrorMessage"] = "KYC details not found!";
                    return RedirectToAction("KycDetails", "Kyc");
                }

                // Update company details
                company.CompanyName = u.CompanyName;
                company.FullAddress = u.FullAddress;
                company.OfficeEmail = u.OfficeEmail;
                company.CompanyWebsiteUrl = u.CompanyWebsiteUrl;
                company.CompanyType = u.CompanyType;
                company.Position = u.Position;
                company.Rating = u.Rating;
                company.IsVerified = false;

                // Update bank details
                bank.BankName = u.BankName;
                bank.AccountNumber = u.AccountNumber;
                bank.AccountHolderName = u.AccountHolderName;
                bank.AccountType = u.AccountType;
                bank.IsVerified = false;

                // Handle file uploads if provided
                if (u.RegisterFile != null)
                {
                    string fileName = "RegisterImage" + Guid.NewGuid() + Path.GetExtension(u.RegisterFile.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "RegisterImage", fileName);

                    if (!Directory.Exists(Path.Combine(_env.WebRootPath, "RegisterImage")))
                    {
                        Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "RegisterImage"));
                    }

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        await u.RegisterFile.CopyToAsync(stream);
                    }
                    company.RegistrationDocument = fileName;
                }

                if (u.PanFile != null)
                {
                    string fileName = "PanImage" + Guid.NewGuid() + Path.GetExtension(u.PanFile.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "PanImage", fileName);

                    if (!Directory.Exists(Path.Combine(_env.WebRootPath, "PanImage")))
                    {
                        Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "PanImage"));
                    }

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        await u.PanFile.CopyToAsync(stream);
                    }
                    company.PanDocument = fileName;
                }

                _context.Update(company);
                _context.Update(bank);
                await _context.SaveChangesAsync();

                try
                {
                    var adminEmail = _configuration.GetValue<string>("EmailSettings:AdminEmail");
                    if (string.IsNullOrEmpty(adminEmail))
                    {
                        throw new Exception("Admin email is not configured in appsettings.json");
                    }

                    var subject = "New KYC Registration Requires Verification";
                    var body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>KYC Verification Required</title>
                    <style>
                        body {{
                            font-family: 'Segoe UI', Arial, sans-serif;
                            line-height: 1.6;
                            color: #333;
                            margin: 0;
                            padding: 0;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 0 auto;
                            padding: 20px;
                        }}
                        .header {{
                            background-color: #0056b3;
                            padding: 20px;
                            text-align: center;
                            color: white;
                            border-top-left-radius: 5px;
                            border-top-right-radius: 5px;
                        }}
                        .content {{
                            background-color: #ffffff;
                            padding: 20px;
                            border-left: 1px solid #ddd;
                            border-right: 1px solid #ddd;
                        }}
                        .footer {{
                            background-color: #f8f8f8;
                            padding: 15px;
                            text-align: center;
                            font-size: 12px;
                            color: #666;
                            border-bottom-left-radius: 5px;
                            border-bottom-right-radius: 5px;
                            border: 1px solid #ddd;
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
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1 style='margin:0;'>KYC Verification</h1>
                        </div>
                        <div class='content'>
                            <h2 style='color:#0056b3;'>Updated KYC Registration Requires Verification</h2>
                            <p>A existing company has updated their details in the system and requires your verification before they can participate in tenders.</p>
        
                            <table class='info-table'>
                                <tr>
                                    <td>Company ID:</td>
                                    <td>{company.CompanyId}</td>
                                </tr>
                                <tr>
                                    <td>Company Name:</td>
                                    <td>{company.CompanyName}</td>
                                </tr>
                                <tr>
                                    <td>Company Type:</td>
                                    <td>{company.CompanyType}</td>
                                </tr>
                                <tr>
                                    <td>Registration Number:</td>
                                    <td>{company.RegistrationNumber}</td>
                                </tr>
                                <tr>
                                    <td>Date Registered:</td>
                                    <td>{DateTime.Now.ToString("dd MMM yyyy, HH:mm")}</td>
                                </tr>
                            </table>
        
                            <p>Please review this KYC registration for accuracy and compliance with organizational guidelines.</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message from the BidNetra. Please do not reply to this email.</p>
                            <p>© 2025 BidNetra. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

                    await _emailService.SendEmailAsync(adminEmail, subject, body);
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"Email sending failed: {emailEx.Message}");
                }


                TempData["SuccessMessage"] = "KYC details updated successfully!";
                return RedirectToAction("KycDetails", "kyc");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to update KYC details. Please try again.";
                return View(u);
            }
        }
    }
}
using TenderSystem.Models;
using TenderSystem.Security;
using TenderSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TenderSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly TenderSystemContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IDataProtector _protector;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public AdminController(TenderSystemContext context, IWebHostEnvironment env,
            DataSecurityProvider key, IDataProtectionProvider provider,
            EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _protector = provider.CreateProtector(key.Key);
            _emailService = emailService;
            _configuration = configuration;
        }

        private void UpdateTenderStatuses()
        {
            var currentDate = DateTime.UtcNow.AddMinutes(345); // Current date-time in Nepal time
            var tenders = _context.TenderDetails.ToList();

            foreach (var tender in tenders)
            {
                var openingDate = tender.OpeningDate.ToDateTime(TimeOnly.MinValue);
                var closingDate = tender.ClosingDate.ToDateTime(TimeOnly.MaxValue);

                if (currentDate >= openingDate && currentDate < closingDate)
                {
                    tender.TenderStatus = "Open";
                }
                else if (currentDate >= closingDate)
                {
                    tender.TenderStatus = "Closed";
                }
                else
                {
                    tender.TenderStatus = "Pending";
                }
            }

            _context.SaveChanges();
        }

        // home page
        public IActionResult Index()
        {
            // Fetch statistics for the dashboard
            var totalUsers = _context.UserLists.Count();
            var totalTenders = _context.TenderDetails.Count();

            var totalPending = _context.Companies.Count(k => !k.IsVerified);



            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalTenders = totalTenders;

            ViewBag.TotalCompanies = totalPending;

            return View();
        }

        public IActionResult UserList()
        {
            //var users = _context.UserLists.ToList();
            var users = _context.UserLists
                .Select(u => new UserListEdit
                {
                    UserId = u.UserId,
                    UserEncId = _protector.Protect(u.UserId.ToString()),
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
                    UserRole = u.UserRole
                })
                .ToList();
            var totalAdmins = _context.UserLists.Count(u => u.UserRole == "Admin");
            var totalPublishers = _context.UserLists.Count(u => u.UserRole == "Publisher");
            var totalBidders = _context.UserLists.Count(u => u.UserRole == "Bidders");

            ViewBag.TotalAdmins = totalAdmins;
            ViewBag.TotalBidders = totalBidders;
            ViewBag.TotalPublishers = totalPublishers;

            return View(users);
        }


        public IActionResult UserDetails(string id)
        {
            int userId = Convert.ToInt32(_protector.Unprotect(id));

            var user = _context.UserLists
                .Where(u => u.UserId == userId)
                .Select(u => new UserListEdit
                {
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
                    UserRole = u.UserRole
                })
                .FirstOrDefault();

            if (user == null)
            {
                return NotFound();
            }

            // Get company details if they exist
            var company = _context.Companies
                .Where(c => c.UserbidId == userId)
                .Select(c => new CompanyEdit
                {
                    CompanyId = c.CompanyId,
                    CompanyName = c.CompanyName,
                    FullAddress = c.FullAddress,
                    OfficeEmail = c.OfficeEmail,
                    CompanyWebsiteUrl = c.CompanyWebsiteUrl,
                    RegistrationNumber = c.RegistrationNumber,
                    RegistrationDocument = c.RegistrationDocument,
                    PanNumber = c.PanNumber,
                    PanDocument = c.PanDocument,
                    CompanyType = c.CompanyType,
                    Position = c.Position,
                    Rating = (decimal)c.Rating
                })
                .FirstOrDefault();

            // Get bank details if they exist
            var bank = _context.Banks
                .Where(b => b.UserbankId == userId)
                .Select(b => new BankEdit
                {
                    BankId = b.BankId,
                    BankName = b.BankName,
                    AccountNumber = b.AccountNumber,
                    AccountType = b.AccountType,
                    AccountHolderName = b.AccountHolderName
                })
                .FirstOrDefault();

            var viewModel = new UserDetailsViewModel
            {
                User = user,
                Company = company,
                Bank = bank
            };

            return View(viewModel);
        }

        // ####################################### Tender ################################
        public IActionResult TenderList()
        {
            UpdateTenderStatuses();
            var tenders = _context.TenderDetails
               .OrderByDescending(t => t.IssuedDate).Select(t => new TenderEdit
               {
                   TenderId = t.TenderId,
                   Title = t.Title,
                   IssuedBy = t.IssuedBy,
                   IssuedDate = t.IssuedDate,
                   TenderType = t.TenderType,
                   TenderStatus = t.TenderStatus,
                   IsVerified = t.IsVerified,
                   OpeningDate = t.OpeningDate,
                   ClosingDate = t.ClosingDate,
                   BudgetEstimation = t.BudgetEstimation,
                   EncId = _protector.Protect(t.TenderId.ToString())
               })
                 .ToList();
            var totalTenders = _context.TenderDetails.Count();
            var totalPendingTenders = _context.TenderDetails.Count(u => u.IsVerified == "Pending");
            var totalVerifiedTenders = _context.TenderDetails.Count(u => u.IsVerified == "Verified");
            var totalNotVerifiedTenders = _context.TenderDetails.Count(u => u.IsVerified == "Not Verified");

            ViewBag.TotalTenders = totalTenders;
            ViewBag.PendingTenders = totalPendingTenders;
            ViewBag.VerifiedTenders = totalVerifiedTenders;
            ViewBag.NotVerifiedTenders = totalNotVerifiedTenders;

            return View(tenders);
        }



        [HttpPost]
        public async Task<IActionResult> UpdateVerifiedStatus(long TenderId, string IsVerified)
        {
            try
            {
                var tender = await _context.TenderDetails
                    .Include(t => t.PublishedByUser) // Include publisher details
                    .FirstOrDefaultAsync(d => d.TenderId == TenderId);

                if (tender != null)
                {
                    tender.IsVerified = IsVerified;
                    await _context.SaveChangesAsync();

                    try
                    {
                        if (IsVerified == "Verified")
                        {
                            // Get publisher details
                            var publisher = await _context.UserLists
                                .FirstOrDefaultAsync(u => u.UserId == tender.PublishedByUserId);

                            if (publisher != null)
                            {
                                // Send email to publisher
                                await _emailService.SendEmailAsync(
                                    publisher.EmailAddress,
                                    "Your Tender Has Been Verified",
                                    GeneratePublisherEmailBody(tender));

                                // Get all bidders
                                var bidders = await _context.UserLists
                                    .Where(u => u.UserRole == "Bidder")
                                    .ToListAsync();

                                // Send emails to bidders
                                foreach (var bidder in bidders)
                                {
                                    await _emailService.SendEmailAsync(
                                        bidder.EmailAddress,
                                        "New Tender Available",
                                        GenerateBidderEmailBody(tender));
                                }
                            }
                        }
                        else if (IsVerified == "Not Verified")
                        {
                            var publisher = await _context.UserLists
                                .FirstOrDefaultAsync(u => u.UserId == tender.PublishedByUserId);

                            if (publisher != null)
                            {
                                await _emailService.SendEmailAsync(
                                    publisher.EmailAddress,
                                    "Tender Verification Status Update",
                                    GenerateRejectionEmailBody(tender));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the email error but continue with the response
                        Console.WriteLine($"Email sending failed: {ex.Message}");
                        return Json(new { success = true, warning = "Status updated but email notification failed." });
                    }

                    return Json(new { success = true });
                }



                return Json(new { success = false, error = "Tender not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Helper methods to generate email bodies
        private string GeneratePublisherEmailBody(TenderDetail tender)
        {
            return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ 
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
                    line-height: 1.6; 
                    color: #333;
                    margin: 0;
                    padding: 0;
                    background-color: #f5f7fa;
                }}
                .email-container {{
                    max-width: 600px;
                    margin: 20px auto;
                    background: white;
                    border-radius: 8px;
                    box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                    overflow: hidden;
                }}
                .email-header {{
                    background: linear-gradient(135deg, #1e40af, #1e3a8a);
                    color: white;
                    padding: 25px;
                    text-align: center;
                }}
                .email-content {{
                    padding: 30px;
                }}
                .status-badge {{
                    display: inline-block;
                    padding: 5px 10px;
                    border-radius: 20px;
                    font-weight: bold;
                    margin-left: 10px;
                }}
                .verified {{
                    background-color: #dcfce7;
                    color: #166534;
                }}
                .rejected {{
                    background-color: #fee2e2;
                    color: #991b1b;
                }}
                .info-table {{
                    width: 100%;
                    border-collapse: collapse;
                    margin: 20px 0;
                }}
                .info-table td {{
                    padding: 10px;
                    border-bottom: 1px solid #e5e7eb;
                }}
                .info-table td:first-child {{
                    font-weight: bold;
                    color: #4b5563;
                    width: 35%;
                }}
                .action-button {{
                    display: inline-block;
                    background: linear-gradient(135deg, #1e40af, #1e3a8a);
                    color: white !important;
                    text-decoration: none;
                    padding: 12px 24px;
                    border-radius: 6px;
                    margin: 20px 0;
                }}
                .email-footer {{
                    background-color: #f9fafb;
                    padding: 15px;
                    text-align: center;
                    font-size: 14px;
                    color: #6b7280;
                    border-top: 1px solid #e5e7eb;
                }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='email-header'>
                    <h2>Tender Verification Update</h2>
                </div>
                <div class='email-content'>
                    <p>Dear Publisher,</p>
                    <p>Your tender has been reviewed by the Tender System admin team:</p>
            
                    <table class='info-table'>
                        <tr>
                            <td>Tender ID:</td>
                            <td>{tender.TenderId}</td>
                        </tr>
                        <tr>
                            <td>Title:</td>
                            <td>{tender.Title}</td>
                        </tr>
                        <tr>
                            <td>Status:</td>
                            <td>
                                <span class='status-badge verified'>Verified</span>
                            </td>
                        </tr>
                        <tr>
                            <td>Verification Date:</td>
                            <td>{DateTime.Now.ToString("dd MMM yyyy")}</td>
                        </tr>
                    </table>
            
                    <p>Your tender is now visible to all registered bidders on our platform.</p>
            
                   
            
                    <p>If you have any questions, please contact our support team.</p>
                </div>
                <div class='email-footer'>
                    <p>This is an automated message from Tender System. Please do not reply to this email.</p>
                    <p>&copy; {DateTime.Now.Year} Tender System. All rights reserved.</p>
                </div>
            </div>
        </body>
        </html>";
        }

        private string GenerateBidderEmailBody(TenderDetail tender)
        {
            return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ 
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
                    line-height: 1.6; 
                    color: #333;
                    margin: 0;
                    padding: 0;
                    background-color: #f5f7fa;
                }}
                .email-container {{
                    max-width: 600px;
                    margin: 20px auto;
                    background: white;
                    border-radius: 8px;
                    box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                    overflow: hidden;
                }}
                .email-header {{
                    background: linear-gradient(135deg, #1e40af, #1e3a8a);
                    color: white;
                    padding: 25px;
                    text-align: center;
                }}
                .email-content {{
                    padding: 30px;
                }}
                .status-badge {{
                    display: inline-block;
                    padding: 5px 10px;
                    border-radius: 20px;
                    font-weight: bold;
                    margin-left: 10px;
                }}
                .verified {{
                    background-color: #dcfce7;
                    color: #166534;
                }}
                .rejected {{
                    background-color: #fee2e2;
                    color: #991b1b;
                }}
                .info-table {{
                    width: 100%;
                    border-collapse: collapse;
                    margin: 20px 0;
                }}
                .info-table td {{
                    padding: 10px;
                    border-bottom: 1px solid #e5e7eb;
                }}
                .info-table td:first-child {{
                    font-weight: bold;
                    color: #4b5563;
                    width: 35%;
                }}
                .action-button {{
                    display: inline-block;
                    background: linear-gradient(135deg, #1e40af, #1e3a8a);
                    color: white !important;
                    text-decoration: none;
                    padding: 12px 24px;
                    border-radius: 6px;
                    margin: 20px 0;
                }}
                .email-footer {{
                    background-color: #f9fafb;
                    padding: 15px;
                    text-align: center;
                    font-size: 14px;
                    color: #6b7280;
                    border-top: 1px solid #e5e7eb;
                }}
            </style>
        </head>
            <body>
                <div class='email-container'>
                    <div class='email-header'>
                        <h2>New Tender Available</h2>
                    </div>
                    <div class='email-content'>
                        <p>Dear Bidder,</p>
                        <p>A new tender matching your profile has been published:</p>
            
                        <table class='info-table'>
                            <tr>
                                <td>Tender Title:</td>
                                <td>{tender.Title}</td>
                            </tr>
                            <tr>
                                <td>Tender Type:</td>
                                <td>{tender.TenderType}</td>
                            </tr>
                            <tr>
                                <td>Budget Estimate:</td>
                                <td>Rs.{tender.BudgetEstimation}</td>
                            </tr>
                            <tr>
                                <td>Opening Date:</td>
                                <td>{tender.OpeningDate:d}</td>
                            </tr>
                            <tr>
                                <td>Closing Date:</td>
                                <td>{tender.ClosingDate:d}</td>
                            </tr>
                        </table>
            
                        <p>This tender is now open for bidding. Don't miss this opportunity!</p>
            
            
            
                        <p>Submit your bid before the closing date to be considered.</p>
                    </div>
                    <div class='email-footer'>
                        <p>This is an automated message from Tender System. Please do not reply to this email.</p>
                        <p>&copy; {DateTime.Now.Year} Tender System All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string GenerateRejectionEmailBody(TenderDetail tender)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                body {{ 
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
                    line-height: 1.6; 
                    color: #333;
                    margin: 0;
                    padding: 0;
                    background-color: #f5f7fa;
                }}
                .email-container {{
                    max-width: 600px;
                    margin: 20px auto;
                    background: white;
                    border-radius: 8px;
                    box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                    overflow: hidden;
                }}
                .email-header {{
                    background: linear-gradient(135deg, #1e40af, #1e3a8a);
                    color: white;
                    padding: 25px;
                    text-align: center;
                }}
                .email-content {{
                    padding: 30px;
                }}
                .status-badge {{
                    display: inline-block;
                    padding: 5px 10px;
                    border-radius: 20px;
                    font-weight: bold;
                    margin-left: 10px;
                }}
                .verified {{
                    background-color: #dcfce7;
                    color: #166534;
                }}
                .rejected {{
                    background-color: #fee2e2;
                    color: #991b1b;
                }}
                .info-table {{
                    width: 100%;
                    border-collapse: collapse;
                    margin: 20px 0;
                }}
                .info-table td {{
                    padding: 10px;
                    border-bottom: 1px solid #e5e7eb;
                }}
                .info-table td:first-child {{
                    font-weight: bold;
                    color: #4b5563;
                    width: 35%;
                }}
                .action-button {{
                    display: inline-block;
                    background: linear-gradient(135deg, #1e40af, #1e3a8a);
                    color: white !important;
                    text-decoration: none;
                    padding: 12px 24px;
                    border-radius: 6px;
                    margin: 20px 0;
                }}
                .email-footer {{
                    background-color: #f9fafb;
                    padding: 15px;
                    text-align: center;
                    font-size: 14px;
                    color: #6b7280;
                    border-top: 1px solid #e5e7eb;
                }}
            </style>
            </head>
            <body>
                <div class='email-container'>
                    <div class='email-header' style='background: linear-gradient(135deg, #dc2626, #b91c1c);'>
                        <h2>Tender Verification Update</h2>
                    </div>
                    <div class='email-content'>
                        <p>Dear Publisher,</p>
                        <p>We regret to inform you that your tender could not be verified:</p>
            
                        <table class='info-table'>
                            <tr>
                                <td>Tender ID:</td>
                                <td>{tender.TenderId}</td>
                            </tr>
                            <tr>
                                <td>Title:</td>
                                <td>{tender.Title}</td>
                            </tr>
                            <tr>
                                <td>Status:</td>
                                <td>
                                    <span class='status-badge rejected'>Not Verified</span>
                                </td>
                            </tr>
                            <tr>
                                <td>Review Date:</td>
                                <td>{DateTime.Now.ToString("dd MMM yyyy")}</td>
                            </tr>
                        </table>
            
                        <p>Possible reasons for rejection:</p>
                        <ul>
                            <li>Incomplete documentation</li>
                            <li>Non-compliance with our terms</li>
                            <li>Missing required information</li>
                        </ul>
            
                        <p>Please review your submission and contact our support team for assistance.</p>
            
                        
                    </div>
                    <div class='email-footer'>
                        <p>This is an automated message from Tender System. Please do not reply to this email.</p>
                        <p>&copy; {DateTime.Now.Year} Tender System. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
        }


        public IActionResult AdminTenderDetails(string id)
        {
            UpdateTenderStatuses();
            int tenderid = Convert.ToInt32(_protector.Unprotect(id));

            var tender = _context.TenderDetails
                .Where(t => t.TenderId == tenderid)
                .Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    IssuedBy = t.IssuedBy,
                    TenderType = t.TenderType,
                    ProjectDuration = t.ProjectDuration,
                    BudgetEstimation = t.BudgetEstimation,
                    TenderStatus = t.TenderStatus,
                    IsVerified = t.IsVerified,
                    IssuedDate = t.IssuedDate,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    AwardDate = t.AwardDate,
                    AwardCompanyId = t.AwardCompanyId,
                    TenderDescription = t.TenderDescription,
                    PublishedByUserId = t.PublishedByUserId,
                    TenderDocument = t.TenderDocument
                })
                .FirstOrDefault();

            if (tender == null)
            {
                return NotFound();
            }

            var user = _context.UserLists
                .Where(u => u.UserId == tender.PublishedByUserId)
                .Select(u => new UserListEdit
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    Province = u.Province,
                    District = u.District,
                    City = u.City,
                    EmailAddress = u.EmailAddress,
                    Phone = u.Phone,
                    UserRole = u.UserRole,
                    UserPhoto = u.UserPhoto,
                })
                .FirstOrDefault();

            var viewModel = new TenderDetailsViewModel
            {
                Tender = tender,
                User = user
            };

            return View(viewModel);
        }


        /// ########################### Kyc ##############################

        public IActionResult KycList()
        {
            var kycList = _context.UserLists
                .Include(u => u.Companies)
             .Where(u => u.UserRole == "Bidder")
             .Select(u => new KycViewModel
             {
                 UserId = u.UserId,
                 UserEncId = _protector.Protect(u.UserId.ToString()),
                 FirstName = u.FirstName,
                 LastName = u.LastName,
                 EmailAddress = u.EmailAddress,
                 Phone = u.Phone,

                 CompanyId = _context.Companies
                     .Where(c => c.UserbidId == u.UserId)
                     .Select(c => c.CompanyId)
                     .FirstOrDefault(),
                 BankId = _context.Banks
                     .Where(b => b.UserbankId == u.UserId)
                     .Select(b => b.BankId)
                     .FirstOrDefault(),
                 CompanyName = _context.Companies
                     .Where(c => c.UserbidId == u.UserId)
                     .Select(c => c.CompanyName)
                     .FirstOrDefault(),
                 RegistrationNumber = _context.Companies
                     .Where(c => c.UserbidId == u.UserId)
                     .Select(c => c.RegistrationNumber)
                     .FirstOrDefault(),
                 PanNumber = _context.Companies
                     .Where(c => c.UserbidId == u.UserId)
                     .Select(c => c.PanNumber)
                     .FirstOrDefault(),
                 IsVerified = _context.Companies
                     .Where(c => c.UserbidId == u.UserId)
                     .Select(c => c.IsVerified)
                     .FirstOrDefault(),
                 HasCompany = _context.Companies.Any(c => c.UserbidId == u.UserId),
                 HasBank = _context.Banks.Any(b => b.UserbankId == u.UserId)
             })
             .Where(k => k.HasCompany && k.HasBank && !k.IsVerified)
             .ToList();

            var totalPending = kycList.Count(k => !k.IsVerified);
            var totalVerified = kycList.Count(k => k.IsVerified);

            ViewBag.TotalPending = totalPending;
            ViewBag.TotalVerified = totalVerified;

            return View(kycList);
        }

        public IActionResult KycDetails(string id)
        {
            try
            {
                // Decrypt the user ID
                int userId = Convert.ToInt32(_protector.Unprotect(id));

                // Get user details
                var user = _context.UserLists
                    .Where(u => u.UserId == userId)
                    .Select(u => new UserListEdit
                    {
                        UserId = u.UserId,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        EmailAddress = u.EmailAddress,
                        Phone = u.Phone,
                        Province = u.Province,
                        District = u.District,
                        City = u.City,
                        UserPhoto = u.UserPhoto
                    })
                    .FirstOrDefault();

                if (user == null)
                {
                    return NotFound();
                }

                // Get company details
                var company = _context.Companies
                    .Where(c => c.UserbidId == userId)
                    .Select(c => new CompanyEdit
                    {
                        CompanyId = c.CompanyId,
                        CompanyName = c.CompanyName,
                        FullAddress = c.FullAddress,
                        OfficeEmail = c.OfficeEmail,
                        RegistrationNumber = c.RegistrationNumber,
                        RegistrationDocument = c.RegistrationDocument,
                        PanNumber = c.PanNumber,
                        PanDocument = c.PanDocument,
                        CompanyType = c.CompanyType,
                        IsVerified = c.IsVerified
                    })
                    .FirstOrDefault();

                // Get bank details
                var bank = _context.Banks
                    .Where(b => b.UserbankId == userId)
                    .Select(b => new BankEdit
                    {
                        BankId = b.BankId,
                        BankName = b.BankName,
                        AccountNumber = b.AccountNumber,
                        AccountType = b.AccountType,
                        AccountHolderName = b.AccountHolderName,
                        IsVerified = b.IsVerified
                    })
                    .FirstOrDefault();

                // Create view model
                var viewModel = new KycDetailsViewModel
                {
                    User = user,
                    Company = company,
                    Bank = bank
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log error and return error view
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateKycStatus(int userId, int companyId, int bankId, bool isVerified)
        {
            try
            {
                // Verify company
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.UserbidId == userId);
                if (company == null)
                {
                    return Json(new { success = false, error = "Company not found" });
                }

                // Verify bank
                var bank = await _context.Banks
                    .FirstOrDefaultAsync(b => b.BankId == bankId && b.UserbankId == userId);
                if (bank == null)
                {
                    return Json(new { success = false, error = "Bank details not found" });
                }

                // Update both records
                company.IsVerified = isVerified;
                bank.IsVerified = isVerified;
                await _context.SaveChangesAsync();

                // Get user details for email
                var user = await _context.UserLists
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user != null)
                {
                    try
                    {
                        var emailSubject = isVerified ?
                            "Your KYC Has Been Verified" :
                            "KYC Verification Update";

                        var emailBody = isVerified ?
                            GenerateKycApprovedEmailBody(company, user) :
                            GenerateKycRejectedEmailBody(company, user);

                        await _emailService.SendEmailAsync(
                            user.EmailAddress,
                            emailSubject,
                            emailBody);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email sending failed: {ex.Message}");
                        return Json(new { success = true, warning = "Status updated but email notification failed." });
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }



        private string GenerateKycApprovedEmailBody(Company company, UserList user)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                body {{ 
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
                    line-height: 1.6; 
                    color: #333;
                    margin: 0;
                    padding: 0;
                    background-color: #f5f7fa;
                }}
                .email-container {{
                    max-width: 600px;
                    margin: 20px auto;
                    background: white;
                    border-radius: 8px;
                    box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                    overflow: hidden;
                }}
                .email-header {{
                    background: linear-gradient(135deg, #1e40af, #1e3a8a);
                    color: white;
                    padding: 25px;
                    text-align: center;
                }}
                .email-content {{
                    padding: 30px;
                }}
                .status-badge {{
                    display: inline-block;
                    padding: 5px 10px;
                    border-radius: 20px;
                    font-weight: bold;
                    margin-left: 10px;
                }}
                .verified {{
                    background-color: #dcfce7;
                    color: #166534;
                }}
                .rejected {{
                    background-color: #fee2e2;
                    color: #991b1b;
                }}
                .info-table {{
                    width: 100%;
                    border-collapse: collapse;
                    margin: 20px 0;
                }}
                .info-table td {{
                    padding: 10px;
                    border-bottom: 1px solid #e5e7eb;
                }}
                .info-table td:first-child {{
                    font-weight: bold;
                    color: #4b5563;
                    width: 35%;
                }}
                .action-button {{
                    display: inline-block;
                    background: linear-gradient(135deg, #1e40af, #1e3a8a);
                    color: white !important;
                    text-decoration: none;
                    padding: 12px 24px;
                    border-radius: 6px;
                    margin: 20px 0;
                }}
                .email-footer {{
                    background-color: #f9fafb;
                    padding: 15px;
                    text-align: center;
                    font-size: 14px;
                    color: #6b7280;
                    border-top: 1px solid #e5e7eb;
                }}
            </style>
                </head>
                <body>
                    <div class='email-container'>
                        <div class='email-header' style='background: linear-gradient(135deg, #166534, #14532d);'>
                            <h2>KYC Verification Approved</h2>
                        </div>
                        <div class='email-content'>
                            <p>Dear {user.FirstName} {user.LastName},</p>
                            <p>We are pleased to inform you that your KYC verification has been successfully completed:</p>
            
                            <table class='info-table'>
                                <tr>
                                    <td>Company Name:</td>
                                    <td>{company.CompanyName}</td>
                                </tr>
                                <tr>
                                    <td>Registration Number:</td>
                                    <td>{company.RegistrationNumber}</td>
                                </tr>
                                <tr>
                                    <td>Status:</td>
                                    <td>
                                        <span class='status-badge verified'>Verified</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td>Verification Date:</td>
                                    <td>{DateTime.Now.ToString("dd MMM yyyy")}</td>
                                </tr>
                            </table>
            
                            <p>Your account now has full access to all platform features including:</p>
                            <ul>
                                <li>Participating in tenders</li>
                                <li>Bidding in auctions</li>
                                <li>Accessing premium features</li>
                            </ul>
            
                            
                        </div>
                        <div class='email-footer'>
                            <p>This is an automated message from Tender System. Please do not reply to this email.</p>
                            <p>&copy; {DateTime.Now.Year}Tender System. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerateKycRejectedEmailBody(Company company, UserList user)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        line-height: 1.6;
                        color: #333;
                        margin: 0;
                        padding: 0;
                        background-color: #f5f7fa;
                    }}
                    .email-container {{
                        max-width: 600px;
                        margin: 20px auto;
                        background: white;
                        border-radius: 8px;
                        box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                        overflow: hidden;
                    }}
                    .email-header {{
                        background: linear-gradient(135deg, #dc2626, #b91c1c);
                        color: white;
                        padding: 25px;
                        text-align: center;
                    }}
                    .email-content {{
                        padding: 30px;
                    }}
                    .status-badge {{
                        display: inline-block;
                        padding: 5px 10px;
                        border-radius: 20px;
                        font-weight: bold;
                        margin-left: 10px;
                    }}
                    .rejected {{
                        background-color: #fee2e2;
                        color: #991b1b;
                    }}
                    .info-table {{
                        width: 100%;
                        border-collapse: collapse;
                        margin: 20px 0;
                    }}
                    .info-table td {{
                        padding: 10px;
                        border-bottom: 1px solid #e5e7eb;
                    }}
                    .info-table td:first-child {{
                        font-weight: bold;
                        color: #4b5563;
                        width: 35%;
                    }}
                    .action-button {{
                        display: inline-block;
                        background: linear-gradient(135deg, #dc2626, #b91c1c);
                        color: white !important;
                        text-decoration: none;
                        padding: 12px 24px;
                        border-radius: 6px;
                        margin: 20px 0;
                    }}
                    .email-footer {{
                        background-color: #f9fafb;
                        padding: 15px;
                        text-align: center;
                        font-size: 14px;
                        color: #6b7280;
                        border-top: 1px solid #e5e7eb;
                    }}
                </style>
            </head>
            <body>
                <div class='email-container'>
                    <div class='email-header'>
                        <h2>KYC Verification Update</h2>
                    </div>
                    <div class='email-content'>
                        <p>Dear {user.FirstName} {user.LastName},</p>
                        <p>We regret to inform you that your KYC verification could not be completed:</p>
            
                        <table class='info-table'>
                            <tr>
                                <td>Company Name:</td>
                                <td>{company.CompanyName}</td>
                            </tr>
                            <tr>
                                <td>Registration Number:</td>
                                <td>{company.RegistrationNumber}</td>
                            </tr>
                            <tr>
                                <td>Status:</td>
                                <td>
                                    <span class='status-badge rejected'>Not Verified</span>
                                </td>
                            </tr>
                            <tr>
                                <td>Review Date:</td>
                                <td>{DateTime.Now.ToString("dd MMM yyyy")}</td>
                            </tr>
                        </table>
            
                        <p>Possible reasons for rejection:</p>
                        <ul>
                            <li>Document verification failed</li>
                            <li>Information mismatch in submitted documents</li>
                            <li>Incomplete documentation</li>
                            <li>Expired documents</li>
                        </ul>
            
                        <p>Please review your submission and contact our support team for assistance.</p>
            
                       
            
                        <p>You may resubmit your KYC documents after addressing the issues mentioned above.</p>
                    </div>
                    <div class='email-footer'>
                        <p>This is an automated message fromTender System. Please do not reply to this email.</p>
                        <p>&copy; {DateTime.Now.Year} Tender System. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}
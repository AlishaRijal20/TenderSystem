using TenderSystem.Models;
using TenderSystem.Security;
using TenderSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;

namespace TenderSystem.Controllers
{

    public class PublisherTenderController : Controller
    {

        private readonly TenderSystemContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public PublisherTenderController(TenderSystemContext context, DataSecurityProvider p,
            IDataProtectionProvider provider, IWebHostEnvironment env,
            EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
            _emailService = emailService;
            _configuration = configuration;
        }


        public async Task<IActionResult> Index()
        {
            // Get the current user's ID
            int userId = Convert.ToInt16(User.Identity!.Name);

            // Create view model for dashboard
            var dashboardViewModel = new PublisherDashboardViewModel
            {
                // Active Tenders
                ActiveTenders = await _context.TenderDetails
                    .CountAsync(t => t.PublishedByUserId == userId && t.TenderStatus == "Open"),


                // Total Bidders (unique bidders for this publisher's auctions and tenders)
                TotalBidders = await GetTotalBiddersAsync(userId),

                // Completion Rate (percentage of completed tenders and auctions)
                CompletionRate = await CalculateCompletionRateAsync(userId),

                // Get Recent Activities
                RecentActivities = await GetRecentActivitiesAsync(userId)
            };

            return View(dashboardViewModel);
        }

        private async Task<int> GetTotalBiddersAsync(int publisherId)
        {


            // Get unique bidders from tender applications
            var tenderBidders = await _context.TenderApplications
                .Where(t => _context.TenderDetails
                    .Any(td => td.TenderId == t.TenderAppllyId && td.PublishedByUserId == publisherId))
                .Select(t => t.CompanyApplyId)
                .Distinct()
                .ToListAsync();

            // Count all companies that have applied to tenders
            var companyUserIds = await _context.Companies
                .Where(c => tenderBidders.Contains(c.CompanyId))
                .Select(c => c.UserbidId)
                .Distinct()
                .ToListAsync();

            // Combine tenders and company
            var allBidders = tenderBidders
                .Select(b => (int)b)  // Convert short to int
                .Union(companyUserIds.Select(id => (int)id))  // Convert short to int
                .Distinct()
                .Count();

            return allBidders;
        }

        private async Task<int> CalculateCompletionRateAsync(int publisherId)
        {
            // Get total number of tenders and auctions created by this publisher
            var totalTenders = await _context.TenderDetails
                .CountAsync(t => t.PublishedByUserId == publisherId);


            // Get number of completed tenders and auctions
            var completedTenders = await _context.TenderDetails
                .CountAsync(t => t.PublishedByUserId == publisherId &&
                           (t.TenderStatus == "Closed" && t.AwardStatus == "Awarded"));


            // Calculate completion rate
            var total = totalTenders;
            var completed = completedTenders;

            return total > 0 ? (int)Math.Round((double)completed / total * 100) : 0;
        }

        private async Task<List<ActivityViewModel>> GetRecentActivitiesAsync(int publisherId)
        {
            var activities = new List<ActivityViewModel>();

            // Get recent tender applications
            var tenderApplications = await _context.TenderApplications
                .Where(ta => _context.TenderDetails
                    .Any(t => t.TenderId == ta.TenderAppllyId && t.PublishedByUserId == publisherId))
                .OrderByDescending(ta => ta.ApplicationId) // Assuming newer applications have higher IDs
                .Take(3)
                .Select(ta => new
                {
                    Application = ta,
                    Tender = _context.TenderDetails.FirstOrDefault(t => t.TenderId == ta.TenderAppllyId),
                    Company = _context.Companies.FirstOrDefault(c => c.CompanyId == ta.CompanyApplyId)
                })
                .ToListAsync();

            foreach (var app in tenderApplications)
            {
                activities.Add(new ActivityViewModel
                {
                    Type = "Tender",
                    Title = "New tender application received",
                    Description = $"{app.Tender?.Title} #{app.Tender?.TenderId}",
                    Time = DateTime.Now.AddDays(-activities.Count),
                    Status = app.Application.ApplicationStatus,
                    IconClass = "fas fa-file-contract"
                });
            }





            // Get the most recent 5 activities, ordered by time
            return activities.OrderByDescending(a => a.Time).Take(5).ToList();
        }


        // tender related methods
        [Route("tenderpage/{activeTab?}")]
        public IActionResult TenderPage(string activeTab = "TenderList")
        {
            ViewBag.ActiveTab = activeTab; // Set the active tab in ViewBag
            return PartialView("_TenderPage");
        }


        private void UpdateTenderStatuses()
        {
            var currentDate = DateTime.UtcNow.AddMinutes(345); // Current date-time in Nepal time
            var tenders = _context.TenderDetails.ToList();

            foreach (var tender in tenders)
            {

                // Skip tenders that are awarded, as their status should not be changed
                if (tender.TenderStatus == "Awarded")
                {
                    continue;
                }

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




        public IActionResult TenderList()
        {
            UpdateTenderStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var tenders = _context.TenderDetails
                .Where(t => t.PublishedByUserId == currentUserID)
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

            return PartialView("_TenderList", tenders); // Returns the Tender List partial view
        }

        public IActionResult TenderAward()
        {
            return PartialView("_TenderAward"); // Returns the Tender Award partial view
        }

        public IActionResult OpenTender()
        {
            UpdateTenderStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var tenders = _context.TenderDetails
                .Where(t => t.PublishedByUserId == currentUserID &&
                            t.TenderStatus == "Open" &&
                            t.IsVerified == "Verified")
                .OrderByDescending(t => t.IssuedDate).Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    IssuedBy = t.IssuedBy,
                    IssuedDate = t.IssuedDate,
                    TenderType = t.TenderType,
                    TenderStatus = t.TenderStatus,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    IsVerified = t.IsVerified,
                    EncId = _protector.Protect(t.TenderId.ToString())
                })
                  .ToList();
            //return Json(tenders);
            return PartialView("_OpenTender", tenders);
        }

        public IActionResult CloseTender()
        {
            UpdateTenderStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var tenders = _context.TenderDetails

                .Where(t => t.PublishedByUserId == currentUserID &&
                            t.TenderStatus == "Closed" &&
                            t.IsVerified == "Verified" &&
                            t.AwardStatus == "Pending")
                .OrderByDescending(t => t.IssuedDate).Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    IssuedBy = t.IssuedBy,
                    IssuedDate = t.IssuedDate,
                    TenderType = t.TenderType,
                    TenderStatus = t.TenderStatus,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    IsVerified = t.IsVerified,
                    EncId = _protector.Protect(t.TenderId.ToString())
                })
                  .ToList();
            //return Json(tenders);
            return PartialView("_CloseTender", tenders);
        }

        public IActionResult AwardedTender()
        {
            UpdateTenderStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var tenders = _context.TenderDetails
                .Where(t => t.PublishedByUserId == currentUserID &&
                            t.TenderStatus == "Closed" &&
                            t.IsVerified == "Verified" &&
                            t.AwardStatus == "Awarded")
                .OrderByDescending(t => t.IssuedDate).Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    IssuedBy = t.IssuedBy,
                    IssuedDate = t.IssuedDate,
                    TenderType = t.TenderType,
                    TenderStatus = t.TenderStatus,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    IsVerified = t.IsVerified,
                    AwardDate = t.AwardDate,
                    AwardStatus = t.AwardStatus,
                    EncId = _protector.Protect(t.TenderId.ToString()),
                    AwardedCompany = t.AwardCompanyId != null ? new CompanyEdit
                    {
                        CompanyId = t.AwardCompany.CompanyId,
                        CompanyName = t.AwardCompany.CompanyName,
                        FullAddress = t.AwardCompany.FullAddress,
                        OfficeEmail = t.AwardCompany.OfficeEmail,
                        CompanyWebsiteUrl = t.AwardCompany.CompanyWebsiteUrl,
                        CompanyType = t.AwardCompany.CompanyType,
                        Position = t.AwardCompany.Position,
                        Rating = t.AwardCompany.Rating,
                        UserbidId = t.AwardCompany.UserbidId,

                    } : null,

                    // Add payment status
                    PaymentStatus = _context.Payments
                        .Where(p => p.PayTenderId == t.TenderId &&
                                   p.PayByUser == currentUserID &&
                                   p.PaymentMethod == "Deposit")
                        .OrderByDescending(p => p.PaymentDate)
                        .Select(p => p.PaymentStatus)
                        .FirstOrDefault() ?? "Not Paid",
                    PaymentId = _context.Payments
                        .Where(p => p.PayTenderId == t.TenderId &&
                                   p.PayByUser == currentUserID &&
                                   p.PaymentMethod == "Deposit")
                        .Select(p => p.PaymentId)
                        .FirstOrDefault()
                })
                      .ToList();
            //return Json(tenders);
            return PartialView("_AwardedTender", tenders);
        }



        public IActionResult PublishTender()
        {
            return View("_PublishTender");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishTender(TenderEdit t)
        {
            //return Json(t);
            try
            {
                // Validate closing date is not before opening date
                if (t.ClosingDate <= t.OpeningDate)
                {
                    ModelState.AddModelError("ClosingDate", "Closing date must be after the opening date.");
                    return View("_PublishTender", t);
                }

                // Validate budget estimation
                if (t.BudgetEstimation <= 0)
                {
                    ModelState.AddModelError("BudgetEstimation", "Budget estimation must be greater than 0.");
                    return View("_PublishTender", t);
                }

                if (t.TenderDescription == null)
                {
                    ModelState.AddModelError("TenderDescription", "Please enter the description.");
                    return View("_PublishTender", t);
                }

                // Validate file upload
                if (t.TenderFile == null)
                {
                    ModelState.AddModelError("TenderFile", "Please upload a tender document.");
                    return View("_PublishTender", t);
                }

                // Validate file type
                string fileExtension = Path.GetExtension(t.TenderFile.FileName).ToLower();
                if (fileExtension != ".pdf")
                {
                    ModelState.AddModelError("TenderFile", "Only PDF files are allowed.");
                    return View("_PublishTender", t);
                }

                // Generate Tender ID
                short maxid;
                if (_context.TenderDetails.Any())
                    maxid = Convert.ToInt16(_context.TenderDetails.Max(x => x.TenderId) + 1);
                else
                    maxid = 1;
                t.TenderId = maxid;

                // Validate and save the file
                if (t.TenderFile != null)
                {
                    if (Path.GetExtension(t.TenderFile.FileName).ToLower() != ".pdf")
                    {
                        ModelState.AddModelError("TenderFile", "Only PDF files are allowed.");
                        return View(t); // Return the view with the validation error
                    }

                    string fileName = "TenderDocument" + Guid.NewGuid() + Path.GetExtension(t.TenderFile.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "TenderDocument", fileName);

                    // Ensure the directory exists
                    if (!Directory.Exists(Path.Combine(_env.WebRootPath, "TenderDocument")))
                    {
                        Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "TenderDocument"));
                    }

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        t.TenderFile.CopyTo(stream);
                    }
                    t.TenderDocument = fileName;
                }

                // Get the current Nepal time
                var currentTime = DateTime.UtcNow.AddMinutes(345);
                var currentDate = DateOnly.FromDateTime(currentTime);

                // Create a new TenderDetail object
                TenderDetail tenderList = new()
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    TenderDescription = t.TenderDescription,
                    TenderType = t.TenderType,
                    BudgetEstimation = t.BudgetEstimation,
                    IssuedBy = t.IssuedBy,
                    IssuedDate = currentDate,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    ProjectDuration = t.ProjectDuration,
                    TenderDocument = t.TenderDocument,
                    PublishedByUserId = Convert.ToInt16(User.Identity!.Name),
                    TenderStatus = "Pending",
                    IsVerified = "Pending",
                    AwardStatus = "Pending",

                };

                //return Json(tenderList);
                // Save the tender to the database
                _context.Add(tenderList);
                await _context.SaveChangesAsync();

                try
                {
                    var adminEmail = _configuration.GetValue<string>("EmailSettings:AdminEmail");
                    if (string.IsNullOrEmpty(adminEmail))
                    {
                        throw new Exception("Admin email is not configured in appsettings.json");
                    }

                    // Send email notification to admin
                    var subject = "New Tender Verification Required";
                    var body = $@"
                        <!DOCTYPE html>
                        <html lang='en'>
                        <head>
                            <meta charset='UTF-8'>
                            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                            <title>Tender Verification Required</title>
                            <style>
                                body {{ font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 0; background: #f4f6f9; color: #333; }}
                                .wrapper {{ max-width: 620px; margin: 30px auto; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(11,31,58,.15); }}

                                /* Header */
                                .header {{
                                    background: #0B1F3A;
                                    padding: 32px 28px;
                                    text-align: center;
                                    position: relative;
                                }}
                                .header-badge {{
                                    display: inline-block;
                                    background: rgba(200,150,12,.15);
                                    border: 1px solid rgba(200,150,12,.3);
                                    color: #E8B84B;
                                    font-size: 11px;
                                    font-weight: 600;
                                    letter-spacing: .12em;
                                    text-transform: uppercase;
                                    padding: 4px 14px;
                                    border-radius: 999px;
                                    margin-bottom: 12px;
                                }}
                                .header h1 {{
                                    margin: 0;
                                    font-size: 22px;
                                    font-weight: 700;
                                    color: #ffffff;
                                    letter-spacing: .01em;
                                }}
                                .header p {{
                                    margin: 8px 0 0;
                                    font-size: 13px;
                                    color: #8A9BB5;
                                }}

                                /* Gold divider line */
                                .gold-line {{
                                    height: 3px;
                                    background: linear-gradient(90deg, transparent, #C8960C 30%, #E8B84B 50%, #C8960C 70%, transparent);
                                }}

                                /* Body */
                                .content {{
                                    background: #ffffff;
                                    padding: 32px 28px;
                                }}
                                .content h2 {{
                                    font-size: 17px;
                                    font-weight: 700;
                                    color: #0B1F3A;
                                    margin: 0 0 10px;
                                }}
                                .content p {{
                                    font-size: 14px;
                                    line-height: 1.7;
                                    color: #5a6a80;
                                    margin: 0 0 20px;
                                }}

                                /* Info table */
                                .info-table {{
                                    width: 100%;
                                    border-collapse: collapse;
                                    margin: 20px 0;
                                    border-radius: 10px;
                                    overflow: hidden;
                                    border: 1px solid rgba(11,31,58,.07);
                                }}
                                .info-table tr:nth-child(odd) td {{ background: #F7F3EC; }}
                                .info-table tr:nth-child(even) td {{ background: #ffffff; }}
                                .info-table td {{
                                    padding: 11px 14px;
                                    font-size: 13.5px;
                                    border-bottom: 1px solid rgba(11,31,58,.05);
                                    color: #333;
                                }}
                                .info-table td:first-child {{
                                    font-weight: 700;
                                    color: #0B1F3A;
                                    width: 150px;
                                    white-space: nowrap;
                                }}
                                .info-table td:last-child {{ color: #5a6a80; }}
                                .info-table .icon {{ color: #C8960C; margin-right: 6px; }}

                                /* Notice box */
                                .notice {{
                                    background: #F7F3EC;
                                    border-left: 3px solid #C8960C;
                                    border-radius: 0 8px 8px 0;
                                    padding: 13px 16px;
                                    font-size: 13.5px;
                                    color: #5a6a80;
                                    line-height: 1.6;
                                    margin-top: 8px;
                                }}

                                /* Footer */
                                .footer {{
                                    background: #0B1F3A;
                                    padding: 18px 28px;
                                    text-align: center;
                                }}
                                .footer p {{
                                    margin: 4px 0;
                                    font-size: 12px;
                                    color: #8A9BB5;
                                }}
                                .footer .brand {{
                                    font-size: 13px;
                                    font-weight: 600;
                                    color: #C8960C;
                                    letter-spacing: .05em;
                                }}
                            </style>
                        </head>
                        <body>
                            <div class='wrapper'>

                                <!-- Header -->
                                <div class='header'>
                                    <div class='header-badge'>Admin Action Required</div>
                                    <h1>Tender Verification</h1>
                                    <p>Nepal Public Procurement Portal</p>
                                </div>
                                <div class='gold-line'></div>

                                <!-- Content -->
                                <div class='content'>
                                    <h2>New Tender Requires Verification</h2>
                                    <p>A new tender has been published and is awaiting your review. Please verify the details below before it becomes publicly available to bidders.</p>

                                    <table class='info-table'>
                                        <tr>
                                            <td><span class='icon'>&#35;</span> Tender ID</td>
                                            <td>{t.TenderId}</td>
                                        </tr>
                                        <tr>
                                            <td><span class='icon'>&#128196;</span> Title</td>
                                            <td>{t.Title}</td>
                                        </tr>
                                        <tr>
                                            <td><span class='icon'>&#127970;</span> Published By</td>
                                            <td>{t.IssuedBy}</td>
                                        </tr>
                                        <tr>
                                            <td><span class='icon'>&#127991;</span> Type</td>
                                            <td>{t.TenderType}</td>
                                        </tr>
                                        <tr>
                                            <td><span class='icon'>&#128197;</span> Date Published</td>
                                            <td>{DateTime.Now.ToString("dd MMM yyyy, HH:mm")}</td>
                                        </tr>
                                    </table>

                                    <div class='notice'>
                                        Please review this tender for accuracy and compliance with organizational guidelines before approving or rejecting it.
                                    </div>
                                </div>

                                <!-- Footer -->
                                <div class='footer'>
                                    <p class='brand'>Nepal Public Procurement Portal</p>
                                    <p>This is an automated message. Please do not reply to this email.</p>
                                    <p>&copy; {DateTime.Now.Year} Nepal Public Procurement Portal. All rights reserved.</p>
                                </div>

                            </div>
                        </body>
                        </html>";

                    await _emailService.SendEmailAsync(adminEmail, subject, body);
                }
                catch (Exception emailEx)
                {
                    // Log the email error but don't stop the tender creation process
                    // The tender is saved, but email failed
                    Console.WriteLine($"Email sending failed: {emailEx.Message}");
                    TempData["WarningMessage"] = "Tender saved successfully, but notification email could not be sent.";
                    return RedirectToAction("TenderPage", "PublisherTender");
                }

                TempData["SuccessMessage"] = "Tender published successfully!";
                return RedirectToAction("TenderPage", "PublisherTender");
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                ModelState.AddModelError("", "An error occurred while publishing the tender. Please try again.");
                return View("_PublishTender", t); // Return the view with the error message
            }
        }



        public IActionResult TenderDetails(string id)
        {
            int tenderid = Convert.ToInt32(_protector.Unprotect(id));
            //return Json(tenderid);
            var tender = _context.TenderDetails
                .Include(t => t.AwardCompany)
                    .ThenInclude(c => c.Userbid)
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
                    TenderDocument = t.TenderDocument,
                    AwardStatus = t.AwardStatus,
                    EncId = _protector.Protect(t.TenderId.ToString()),
                    // Add awarded company details
                    AwardedCompany = t.AwardCompanyId != null ? new CompanyEdit
                    {
                        CompanyId = t.AwardCompany.CompanyId,
                        CompanyName = t.AwardCompany.CompanyName,
                        FullAddress = t.AwardCompany.FullAddress,
                        OfficeEmail = t.AwardCompany.OfficeEmail,
                        CompanyWebsiteUrl = t.AwardCompany.CompanyWebsiteUrl,
                        CompanyType = t.AwardCompany.CompanyType,
                        Position = t.AwardCompany.Position,
                        Rating = t.AwardCompany.Rating,
                        UserbidId = t.AwardCompany.UserbidId,
                        EncId = _protector.Protect(t.AwardCompany.CompanyId.ToString())
                    } : null
                })
                .FirstOrDefault();

            return View(tender);
        }

        public async Task<IActionResult> MonitorTender(string id)
        {
            UpdateTenderStatuses();
            int tenderid = Convert.ToInt32(_protector.Unprotect(id));

            // Fetch Tender Details
            var tender = _context.TenderDetails
                .Where(t => t.TenderId == tenderid)
                .Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    IssuedBy = t.IssuedBy,
                    IssuedDate = t.IssuedDate,
                    TenderType = t.TenderType,
                    TenderStatus = t.TenderStatus,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    ProjectDuration = t.ProjectDuration,
                    BudgetEstimation = t.BudgetEstimation,
                    TenderDescription = t.TenderDescription,
                    IsVerified = t.IsVerified,
                    PublishedByUserId = t.PublishedByUserId,
                    EncId = _protector.Protect(t.TenderId.ToString())
                })
                .FirstOrDefault();

            if (tender == null)
            {
                return NotFound("Tender not found.");
            }

            /*// Call the recommendation API asynchronously
            using (var client = new HttpClient())
            {
                try
                {
                    var apiUrl = "http://127.0.0.1:5000/api/recommend";
                    var requestData = new { tender_id = tenderid.ToString() };

                    var response = await client.PostAsJsonAsync(apiUrl, requestData);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<RecommendationResponse>();
                        if (result?.recommended_companies != null)
                        {
                            tender.RecommendedCompanies = result.recommended_companies;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"API Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calling recommendation API: {ex.Message}");
                }
            }*/

            // Fetch Applications for the Tender
            var applications = _context.TenderApplications
                .Where(ta => ta.TenderAppllyId == tenderid)
                .Select(ta => new
                {
                    Application = new TenderApplicationEdit
                    {
                        ApplicationId = ta.ApplicationId,
                        ApplicationDocument = ta.ApplicationDocument,
                        ProposedBudget = ta.ProposedBudget,
                        ApplicationStatus = ta.ApplicationStatus,
                        ProposedDuration = ta.ProposedDuration,
                        EncId = _protector.Protect(ta.ApplicationId.ToString())
                    },
                    CompanyApplyId = ta.CompanyApplyId
                })
                .ToList();

            // Fetch Company and Bidder Details
            var companyDetails = _context.Companies
                .Where(c => applications.Select(a => a.CompanyApplyId).Contains(c.CompanyId))
                .Select(c => new
                {
                    Company = new CompanyEdit
                    {
                        CompanyId = c.CompanyId,
                        CompanyName = c.CompanyName,
                        FullAddress = c.FullAddress,
                        OfficeEmail = c.OfficeEmail,
                        CompanyWebsiteUrl = c.CompanyWebsiteUrl,
                        RegistrationNumber = c.RegistrationNumber,
                        CompanyType = c.CompanyType,
                        Position = c.Position,
                    },
                    UserbidId = c.UserbidId
                })
                .ToList();

            var bidderDetails = _context.UserLists
                .Where(u => companyDetails.Select(c => c.UserbidId).Contains(u.UserId))
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
                .ToList();

            // Creating ViewModel
            var viewModel = new MonitorTenderViewModel
            {
                Tender = tender,
                Applications = applications.Select(a => new TenderApplicationViewModel
                {
                    Application = a.Application,
                    Company = companyDetails.FirstOrDefault(c => c.Company.CompanyId == a.CompanyApplyId)?.Company,
                    User = bidderDetails.FirstOrDefault(b => b.UserId == companyDetails.FirstOrDefault(c => c.Company.CompanyId == a.CompanyApplyId)?.UserbidId)
                }).ToList()
            };

            return View(viewModel);
        }




        public IActionResult ApplicationDetails(string id)
        {
            // Decrypt the application ID
            int appId = Convert.ToInt32(_protector.Unprotect(id));

            // Fetch the Application Details
            var application = _context.TenderApplications
                .Where(ta => ta.ApplicationId == appId)
                .Select(ta => new TenderApplicationEdit
                {
                    ApplicationId = ta.ApplicationId,
                    ApplicationDocument = ta.ApplicationDocument,
                    ProposedBudget = ta.ProposedBudget,
                    ApplicationStatus = ta.ApplicationStatus,
                    ProposedDuration = ta.ProposedDuration,
                    TenderAppllyId = ta.TenderAppllyId,
                    CompanyApplyId = ta.CompanyApplyId,

                    EncId = _protector.Protect(ta.ApplicationId.ToString())

                })
                .FirstOrDefault();

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Fetch Tender Details based on TenderId
            var tender = _context.TenderDetails
                .Where(t => t.TenderId == application.TenderAppllyId)
                .Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    IssuedBy = t.IssuedBy,
                    IssuedDate = t.IssuedDate,
                    TenderType = t.TenderType,
                    TenderStatus = t.TenderStatus,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    ProjectDuration = t.ProjectDuration,
                    BudgetEstimation = t.BudgetEstimation,
                    TenderDescription = t.TenderDescription,
                    IsVerified = t.IsVerified,
                    PublishedByUserId = t.PublishedByUserId,
                    EncId = _protector.Protect(t.TenderId.ToString())
                })
                .FirstOrDefault();

            // Fetch Company Details that applied for the tender
            var company = _context.Companies
                .Where(c => c.CompanyId == application.CompanyApplyId)
                .Select(c => new CompanyEdit
                {
                    CompanyId = c.CompanyId,
                    CompanyName = c.CompanyName,
                    FullAddress = c.FullAddress,
                    OfficeEmail = c.OfficeEmail,
                    CompanyWebsiteUrl = c.CompanyWebsiteUrl,
                    RegistrationNumber = c.RegistrationNumber,
                    CompanyType = c.CompanyType,
                    Position = c.Position,
                    Rating = c.Rating,
                    PanDocument = c.PanDocument,
                    PanNumber = c.PanNumber,
                    RegistrationDocument = c.RegistrationDocument,
                    UserbidId = c.UserbidId,
                    EncId = _protector.Protect(c.CompanyId.ToString())

                })
                .FirstOrDefault();

            //return Json(company);

            // Fetch Bidder (User) Details
            var bidder = _context.UserLists
                .Where(u => u.UserId == company.UserbidId)
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

            // return Json(bidder);

            // fetch bidder bank details
            var bank = _context.Banks
                .Where(b => b.UserbankId == bidder.UserId)
                .Select(b => new BankEdit
                {
                    BankId = b.BankId,
                    BankName = b.BankName,
                    AccountNumber = b.AccountNumber,
                    AccountType = b.AccountType,
                    AccountHolderName = b.AccountHolderName,
                    UserbankId = b.UserbankId

                })
                .FirstOrDefault();

            // Create ViewModel for passing to the view
            var viewModel = new TenderApplicationViewModel
            {
                Application = application,
                Tender = tender,
                Company = company,
                User = bidder,
                Bank = bank
            };

            return View(viewModel);
        }




        /*[HttpPost]
        public async Task<IActionResult> AwardTender(long ApplicationId, string ApplicationStatus)
        {
            try
            {
                var application = await _context.TenderApplications
                    .Include(a => a.CompanyApply)
                        .ThenInclude(c => c.Userbid)
                    .FirstOrDefaultAsync(a => a.ApplicationId == ApplicationId);

                if (application == null)
                {
                    return Json(new { success = false, message = "Application not found." });
                }

                if (application.ApplicationStatus != "Pending")
                {
                    return Json(new { success = false, message = "Application is not in a pending state." });
                }

                application.ApplicationStatus = ApplicationStatus;

                if (ApplicationStatus == "Won")
                {
                    var tender = await _context.TenderDetails
                        .Include(t => t.PublishedByUser)
                        .FirstOrDefaultAsync(t => t.TenderId == application.TenderAppllyId);

                    if (tender == null)
                    {
                        return Json(new { success = false, message = "Tender details not found." });
                    }

                    var currentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(345));

                    tender.AwardStatus = "Awarded";
                    tender.AwardCompanyId = application.CompanyApplyId;
                    tender.AwardDate = currentDate;

                    _context.Update(tender);

                    var otherApplications = _context.TenderApplications
                        .Where(a => a.TenderAppllyId == application.TenderAppllyId && a.ApplicationId != ApplicationId)
                        .ToList();

                    foreach (var app in otherApplications)
                    {
                        app.ApplicationStatus = "Lost";
                    }


                }

                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = "Tender status updated successfully.",
                    redirectUrl = Url.Action("MonitorTender", "PublisherTender",
                        new { id = _protector.Protect(application.TenderAppllyId.ToString()) })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }*/


        [HttpPost]
        public async Task<IActionResult> AwardTender(long ApplicationId, string ApplicationStatus)
        {
            try
            {
                var application = await _context.TenderApplications
                    .Include(a => a.CompanyApply)
                        .ThenInclude(c => c.Userbid)
                    .FirstOrDefaultAsync(a => a.ApplicationId == ApplicationId);

                if (application == null)
                    return Json(new { success = false, message = "Application not found." });

                if (application.ApplicationStatus != "Pending")
                    return Json(new { success = false, message = "Application is not in a pending state." });

                application.ApplicationStatus = ApplicationStatus;

                if (ApplicationStatus == "Won")
                {
                    var tender = await _context.TenderDetails
                        .Include(t => t.PublishedByUser)
                        .FirstOrDefaultAsync(t => t.TenderId == application.TenderAppllyId);

                    if (tender == null)
                        return Json(new { success = false, message = "Tender details not found." });

                    var currentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(345));
                    tender.AwardStatus = "Awarded";
                    tender.AwardCompanyId = application.CompanyApplyId;
                    tender.AwardDate = currentDate;
                    _context.Update(tender);

                    // Get winner details
                    var winnerCompany = await _context.Companies
                        .Include(c => c.Userbid)
                        .FirstOrDefaultAsync(c => c.CompanyId == application.CompanyApplyId);

                    string winnerName = winnerCompany?.Userbid != null
                        ? $"{winnerCompany.Userbid.FirstName} {winnerCompany.Userbid.LastName}"
                        : "N/A";
                    string winnerCompanyName = winnerCompany?.CompanyName ?? "N/A";
                    string winnerEmail = winnerCompany?.Userbid?.EmailAddress ?? "";

                    // Mark other applications as Lost and send sorry emails
                    var otherApplications = await _context.TenderApplications
                        .Where(a => a.TenderAppllyId == application.TenderAppllyId && a.ApplicationId != ApplicationId)
                        .Include(a => a.CompanyApply)
                            .ThenInclude(c => c.Userbid)
                        .ToListAsync();

                    foreach (var app in otherApplications)
                    {
                        app.ApplicationStatus = "Lost";

                        // Send "better luck" email to each loser
                        if (!string.IsNullOrEmpty(app.CompanyApply?.Userbid?.EmailAddress))
                        {
                            string loserEmail = app.CompanyApply.Userbid.EmailAddress;
                            string loserName = $"{app.CompanyApply.Userbid.FirstName} {app.CompanyApply.Userbid.LastName}";
                            string loserCompany = app.CompanyApply.CompanyName ?? "Your Company";

                            string lostSubject = $"Tender Result — {tender.Title}";
                            string lostBody = $@"
                    <!DOCTYPE html>
                    <html lang='en'>
                    <head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; background: #f4f6f9; margin: 0; padding: 0; color: #333; }}
                        .container {{ max-width: 600px; margin: 30px auto; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,.1); }}
                        .header {{ background: #0B1F3A; padding: 28px 24px; text-align: center; }}
                        .header h1 {{ color: #fff; margin: 0; font-size: 22px; font-family: Georgia, serif; }}
                        .header p {{ color: #8A9BB5; font-size: 13px; margin: 6px 0 0; }}
                        .body {{ padding: 30px 28px; }}
                        .body h2 {{ font-family: Georgia, serif; color: #0B1F3A; font-size: 18px; margin-top: 0; }}
                        .body p {{ font-size: 14px; line-height: 1.7; color: #555; }}
                        .winner-box {{ background: #F7F3EC; border-left: 4px solid #C8960C; border-radius: 6px; padding: 14px 18px; margin: 20px 0; }}
                        .winner-box p {{ margin: 0; font-size: 13px; color: #555; }}
                        .winner-box strong {{ color: #0B1F3A; }}
                        .tender-box {{ background: #f8f9fa; border-radius: 6px; padding: 14px 18px; margin: 20px 0; }}
                        .tender-box p {{ margin: 4px 0; font-size: 13px; color: #555; }}
                        .tender-box strong {{ color: #0B1F3A; }}
                        .footer {{ background: #f8f8f8; padding: 16px; text-align: center; font-size: 12px; color: #999; border-top: 1px solid #eee; }}
                    </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>Tender Result Notification</h1>
                                <p>Nepal Public Procurement Portal</p>
                            </div>
                            <div class='body'>
                                <h2>Dear {loserName},</h2>
                                <p>Thank you for submitting your proposal for the following tender on behalf of <strong>{loserCompany}</strong>. After careful evaluation, we regret to inform you that your application was not selected this time.</p>

                                <div class='tender-box'>
                                    <p><strong>Tender:</strong> {tender.Title}</p>
                                    <p><strong>Tender ID:</strong> #{tender.TenderId}</p>
                                    <p><strong>Issued By:</strong> {tender.IssuedBy}</p>
                                    <p><strong>Award Date:</strong> {currentDate:MMMM dd, yyyy}</p>
                                </div>

                                <div class='winner-box'>
                                    <p><strong>Awarded To:</strong> {winnerCompanyName}</p>
                                    <p><strong>Representative:</strong> {winnerName}</p>
                                </div>

                                <p>We truly appreciate the effort and time you invested in preparing your proposal. We encourage you to continue participating in future tenders — your experience and dedication are valuable.</p>
                                <p>Better luck next time!</p>
                                <p style='margin-top:24px;'>Warm regards,<br><strong>Nepal Public Procurement Portal</strong></p>
                            </div>
                            <div class='footer'>
                                <p>This is an automated message. Please do not reply to this email.</p>
                                <p>© {DateTime.Now.Year} Nepal Public Procurement Portal. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                            try { await _emailService.SendEmailAsync(loserEmail, lostSubject, lostBody); }
                            catch (Exception emailEx) { Console.WriteLine($"Lost email failed for {loserEmail}: {emailEx.Message}"); }
                        }
                    }

                    // Send congratulations email to winner
                    if (!string.IsNullOrEmpty(winnerEmail))
                    {
                        string wonSubject = $"Congratulations! You've Won — {tender.Title}";
                        string wonBody = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{ font-family: 'Segoe UI', Arial, sans-serif; background: #f4f6f9; margin: 0; padding: 0; color: #333; }}
                    .container {{ max-width: 600px; margin: 30px auto; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,.1); }}
                    .header {{ background: linear-gradient(135deg, #0B1F3A, #122848); padding: 28px 24px; text-align: center; }}
                    .header h1 {{ color: #fff; margin: 0; font-size: 22px; font-family: Georgia, serif; }}
                    .header p {{ color: #C8960C; font-size: 13px; margin: 6px 0 0; letter-spacing: .05em; text-transform: uppercase; }}
                    .trophy {{ font-size: 48px; text-align: center; padding: 20px 0 0; }}
                    .body {{ padding: 10px 28px 30px; }}
                    .body h2 {{ font-family: Georgia, serif; color: #0B1F3A; font-size: 18px; }}
                    .body p {{ font-size: 14px; line-height: 1.7; color: #555; }}
                    .award-box {{ background: linear-gradient(135deg, rgba(200,150,12,.08), rgba(200,150,12,.03)); border: 1px solid rgba(200,150,12,.25); border-radius: 8px; padding: 18px 20px; margin: 20px 0; }}
                    .award-box p {{ margin: 5px 0; font-size: 13px; color: #555; }}
                    .award-box strong {{ color: #0B1F3A; }}
                    .award-box .amount {{ font-family: Georgia, serif; font-size: 20px; font-weight: 700; color: #C8960C; }}
                    .footer {{ background: #f8f8f8; padding: 16px; text-align: center; font-size: 12px; color: #999; border-top: 1px solid #eee; }}
                </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Congratulations!</h1>
                            <p>Tender Award Notification</p>
                        </div>
                        <div class='trophy'>🏆</div>
                        <div class='body'>
                            <h2>Dear {winnerName},</h2>
                            <p>We are delighted to inform you that your proposal submitted on behalf of <strong>{winnerCompanyName}</strong> has been selected as the winning bid for the following tender.</p>

                            <div class='award-box'>
                                <p><strong>Tender:</strong> {tender.Title}</p>
                                <p><strong>Tender ID:</strong> #{tender.TenderId}</p>
                                <p><strong>Issued By:</strong> {tender.IssuedBy}</p>
                                <p><strong>Award Date:</strong> {currentDate:MMMM dd, yyyy}</p>
                                <p><strong>Budget:</strong> <span class='amount'>₹ {tender.BudgetEstimation:N2}</span></p>
                            </div>

                            <p>Our team will be in touch with you shortly regarding the next steps. Please ensure all required documents and agreements are prepared for the contract process.</p>
                            <p>We look forward to a successful collaboration!</p>
                            <p style='margin-top:24px;'>Warm regards,<br><strong>Nepal Public Procurement Portal</strong></p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                            <p>© {DateTime.Now.Year} Nepal Public Procurement Portal. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

                        try { await _emailService.SendEmailAsync(winnerEmail, wonSubject, wonBody); }
                        catch (Exception emailEx) { Console.WriteLine($"Won email failed for {winnerEmail}: {emailEx.Message}"); }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = "Tender status updated successfully.",
                    redirectUrl = Url.Action("MonitorTender", "PublisherTender",
                        new { id = _protector.Protect(application.TenderAppllyId.ToString()) })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }


        [HttpGet]
        public IActionResult EditTender(string id)
        {
            UpdateTenderStatuses();
            int tenderid;
            try
            {
                tenderid = Convert.ToInt32(_protector.Unprotect(id));
            }
            catch
            {
                return BadRequest("Invalid tender ID.");
            }

            var tender = _context.TenderDetails
                .Where(t => t.TenderId == tenderid)
                .Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    IssuedBy = t.IssuedBy,
                    IssuedDate = t.IssuedDate,
                    TenderType = t.TenderType,
                    TenderStatus = t.TenderStatus,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    ProjectDuration = t.ProjectDuration,
                    BudgetEstimation = t.BudgetEstimation,
                    TenderDescription = t.TenderDescription,
                    IsVerified = t.IsVerified,
                    PublishedByUserId = t.PublishedByUserId,
                    AwardStatus = t.AwardStatus,
                    AwardCompanyId = t.AwardCompanyId,
                    AwardDate = t.AwardDate,
                    TenderDocument = t.TenderDocument,
                    EncId = _protector.Protect(t.TenderId.ToString())
                })
                .FirstOrDefault();

            if (tender == null)
            {
                return NotFound("Tender not found.");
            }

            //return Json(tender);

            return View(tender);
        }

        [HttpPost]
        public async Task<IActionResult> EditTender(TenderEdit t)
        {
            UpdateTenderStatuses();
            try
            {
                string? existingFile = _context.TenderDetails
                    .Where(td => td.TenderId == t.TenderId)
                    .Select(td => td.TenderDocument)
                    .FirstOrDefault();

                // return Json(existingFile);

                if (t.TenderFile != null)
                {
                    string fileName = "TenderDocument" + Guid.NewGuid() + Path.GetExtension(t.TenderFile.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "TenderDocument", fileName);

                    if (!Directory.Exists(Path.Combine(_env.WebRootPath, "TenderDocument")))
                    {
                        Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "TenderDocument"));
                    }

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        await t.TenderFile.CopyToAsync(stream);
                    }

                    t.TenderDocument = fileName;
                }
                else
                {
                    // Retain existing document if no new file is uploaded
                    t.TenderDocument = existingFile;
                }

                var tender = _context.TenderDetails.FirstOrDefault(td => td.TenderId == t.TenderId);
                if (tender == null)
                {
                    return NotFound("Tender not found.");
                }

                var currentTime = DateTime.UtcNow.AddMinutes(345);
                var currentDate = DateOnly.FromDateTime(currentTime);
                // return Json(tender);
                // Updating existing tender details
                tender.Title = t.Title;
                tender.IssuedBy = t.IssuedBy;
                tender.IssuedDate = currentDate;
                tender.TenderType = t.TenderType;
                tender.TenderStatus = t.TenderStatus;
                tender.OpeningDate = t.OpeningDate;
                tender.ClosingDate = t.ClosingDate;
                tender.ProjectDuration = t.ProjectDuration;
                tender.BudgetEstimation = t.BudgetEstimation;
                tender.TenderDescription = t.TenderDescription;
                tender.IsVerified = t.IsVerified;
                tender.PublishedByUserId = Convert.ToInt16(User.Identity!.Name);
                tender.AwardStatus = t.AwardStatus;
                tender.AwardCompanyId = t.AwardCompanyId;
                tender.AwardDate = t.AwardDate;
                tender.TenderDocument = t.TenderDocument;

                //return Json(tender);
                _context.Update(tender);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tender updated successfully!";
                return RedirectToAction("TenderPage", "PublisherTender");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the tender. Please try again.");
                return View(t);
            }
        }


        [HttpGet]
        public ActionResult DeleteTender(string id)
        {
            int tenderId = Convert.ToInt32(_protector.Unprotect(id));

            // Find the tender in the database
            var tender = _context.TenderDetails
                .Where(t => t.TenderId == tenderId)
                .Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    IssuedBy = t.IssuedBy,
                    TenderStatus = t.TenderStatus
                })
                .FirstOrDefault();

            if (tender == null)
            {
                return NotFound();
            }

            return View(tender);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTenderConfirmed(string id)
        {
            short tenderId = Convert.ToInt16(_protector.Unprotect(id));

            var tender = await _context.TenderDetails.FindAsync(tenderId);
            if (tender != null)
            {
                _context.TenderDetails.Remove(tender);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tender deleted successfully!";
                return RedirectToAction("TenderPage", "PublisherTender");
            }

            TempData["ErrorMessage"] = "Tender not found!";
            return RedirectToAction("TenderPage", "PublisherTender");
        }





    }
}


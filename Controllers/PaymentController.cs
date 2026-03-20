using Microsoft.AspNetCore.Mvc;
using payment_gateway_nepal;
using TenderSystem.Models;
using Microsoft.EntityFrameworkCore;

using TenderSystem.Services;
using TenderSystem.Security;
using Microsoft.AspNetCore.DataProtection;
using System.Text;
using Newtonsoft.Json;
using MimeKit;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Reflection;


namespace FYPBidNetra.Controllers
{
    public class PaymentController : Controller
    {
        private readonly TenderSystemContext _context;
        private readonly string eSewa_TestKey = "8gBm/:&EnhH.1/q";
        private readonly bool testMode = true;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IDataProtector _protector;

        public PaymentController(TenderSystemContext context, IWebHostEnvironment env,
           DataSecurityProvider key, IDataProtectionProvider provider, EmailService emailService)
        {
            _context = context;
            _env = env;
            _protector = provider.CreateProtector(key.Key);
            _emailService = emailService;

        }





        [HttpGet]
        public async Task<IActionResult> VerifyPayment()
        {
            try
            {
                // Get payment data from session
                var paymentDataJson = HttpContext.Session.GetString("PendingPayment");
                if (string.IsNullOrEmpty(paymentDataJson))
                {
                    return RedirectToAction("PaymentFailure");
                }

                dynamic paymentData = JsonConvert.DeserializeObject<dynamic>(paymentDataJson);


                // Create payment record
                var paymentRecord = new Payment
                {
                    PaymentId = (short)(_context.Payments.Any()
                        ? _context.Payments.Max(p => p.PaymentId) + 1
                        : 1),

                    PaymentAmount = paymentData.Amount,
                    PaymentDate = DateTime.UtcNow.AddMinutes(345),
                    PaymentMethod = "Esewa",
                    PaymentStatus = "Pending", // Initial status
                    PayToUser = paymentData.PayToUser,
                    PayByUser = paymentData.PayByUser,
                    PayTenderId = paymentData.TenderId,
                    PayCompanyId = paymentData.CompanyId
                };

                _context.Payments.Add(paymentRecord);
                await _context.SaveChangesAsync();

                // Get tender application data from session
                var tenderDataJson = HttpContext.Session.GetString("TenderApplicationData");
                if (string.IsNullOrEmpty(tenderDataJson))
                {
                    paymentRecord.PaymentStatus = "Failed";
                    await _context.SaveChangesAsync();
                    return RedirectToAction("PaymentFailure");
                }

                var tenderData = JsonConvert.DeserializeObject<TenderApplicationEdit>(tenderDataJson);



                // Handle file from temp location
                string applicationDocPath = null;
                if (HttpContext.Session.TryGetValue("TenderTempPath", out var tempPathBytes))
                {
                    var tempPath = Encoding.UTF8.GetString(tempPathBytes);
                    var fileName = $"tender_{tenderData.ApplicationId}{Path.GetExtension(HttpContext.Session.GetString("TenderFileName"))}";
                    applicationDocPath = await SaveFileToPermanentLocation(tempPath, "ProposalTender", fileName);
                }

                // Create and save tender application
                var tenderApplication = new TenderApplication
                {
                    ApplicationId = tenderData.ApplicationId,
                    TenderAppllyId = tenderData.TenderAppllyId,
                    CompanyApplyId = tenderData.CompanyApplyId,
                    ProposedBudget = tenderData.ProposedBudget,
                    ProposedDuration = tenderData.ProposedDuration,
                    ApplicationDocument = applicationDocPath,
                    ApplicationStatus = "Pending",

                };

                _context.TenderApplications.Add(tenderApplication);

                // Update payment status to success
                paymentRecord.PaymentStatus = "Verified";
                await _context.SaveChangesAsync();

                short tenderId = paymentData.TenderId;
                short companyId = paymentData.CompanyId;

                var tender = await _context.TenderDetails
                    .Include(t => t.PublishedByUser)
                    .FirstOrDefaultAsync(t => t.TenderId == tenderId);

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (tender?.PublishedByUser?.EmailAddress != null)
                {
                    // Send email to publisher
                    string emailBody = $@"
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
                            .info-table td:first-child {{ font-weight: 700; color: #0B1F3A; width: 170px; }}
                            .info-table .budget {{ font-weight: 700; color: #C8960C; font-size: 14px; }}
                            .status-badge {{ display: inline-block; background: #fef3c7; color: #92400e; font-weight: 700; font-size: 12px; padding: 4px 12px; border-radius: 999px; letter-spacing: .04em; }}
                            .notice {{ background: #F7F3EC; border-left: 3px solid #C8960C; border-radius: 0 8px 8px 0; padding: 13px 16px; font-size: 13.5px; color: #5a6a80; line-height: 1.6; margin-top: 8px; }}
                            .notice strong {{ color: #0B1F3A; }}
                            .footer {{ background: #0B1F3A; padding: 18px 28px; text-align: center; }}
                            .footer p {{ margin: 4px 0; font-size: 12px; color: #8A9BB5; }}
                            .footer .brand {{ font-size: 13px; font-weight: 600; color: #C8960C; letter-spacing: .05em; }}
                        </style>
                        </head>
                        <body>
                        <div class='wrapper'>

                            <div class='header'>
                                <div class='header-badge'>New Proposal</div>
                                <h1>Tender Proposal Received</h1>
                                <p>Nepal Public Procurement Portal</p>
                            </div>
                            <div class='gold-line'></div>

                            <div class='content'>
                                <h2>A New Proposal Has Been Submitted</h2>
                                <p>Dear Publisher,</p>
                                <p>A new proposal has been submitted for your tender. Please review it at your earliest convenience and take the appropriate action.</p>

                                <table class='info-table'>
                                    <tr>
                                        <td>&#128196; Tender Title</td>
                                        <td>{tender.Title}</td>
                                    </tr>
                                    <tr>
                                        <td>&#127970; Company Name</td>
                                        <td>{company?.CompanyName}</td>
                                    </tr>
                                    <tr>
                                        <td>&#8377; Proposed Budget</td>
                                        <td class='budget'>Rs. {tenderApplication.ProposedBudget:N2}</td>
                                    </tr>
                                    <tr>
                                        <td>&#9200; Proposed Duration</td>
                                        <td>{tenderApplication.ProposedDuration}</td>
                                    </tr>
                                    <tr>
                                        <td>&#128197; Submission Date</td>
                                        <td>{DateTime.Now.ToString("dd MMM yyyy")}</td>
                                    </tr>
                                    <tr>
                                        <td>&#128203; Status</td>
                                        <td><span class='status-badge'>Pending Review</span></td>
                                    </tr>
                                </table>

                                <div class='notice'>
                                    <strong>&#9432; Next Step:</strong> Log in to the portal to review this proposal in detail. You can accept or reject it after reviewing all submitted documents and information.
                                </div>
                            </div>

                            <div class='footer'>
                                <p class='brand'>Nepal Public Procurement Portal</p>
                                <p>This is an automated message. Please do not reply to this email.</p>
                                <p>&copy; {DateTime.Now.Year} Nepal Public Procurement Portal. All rights reserved.</p>
                            </div>

                        </div>
                        </body>
                        </html>";

                    await _emailService.SendEmailAsync(
                        tender.PublishedByUser.EmailAddress,
                        "New Tender Proposal Received",
                        emailBody);
                }


                // Clean up session data
                CleanTempFiles();
                //ClearSessionData();

                return RedirectToAction("PaymentSuccess", new { tenderId = (int)paymentData.TenderId });
            }
            catch (Exception ex)
            {
                // Update payment status if record exists
                var paymentDataJson = HttpContext.Session.GetString("PendingPayment");
                if (!string.IsNullOrEmpty(paymentDataJson))
                {
                    dynamic paymentData = JsonConvert.DeserializeObject<dynamic>(paymentDataJson);

                    var paymentRecord = new Payment
                    {
                        PaymentId = (short)(_context.Payments.Any()
                            ? _context.Payments.Max(p => p.PaymentId) + 1
                            : 1),

                        PaymentAmount = paymentData.Amount,
                        PaymentDate = DateTime.UtcNow.AddMinutes(345),
                        PaymentMethod = "Esewa",
                        PaymentStatus = "Failed",
                        PayToUser = paymentData.PayToUser,
                        PayByUser = paymentData.PayByUser,
                        PayTenderId = paymentData.TenderId,
                        PayCompanyId = paymentData.CompanyId
                    };
                    _context.Payments.Add(paymentRecord);
                    await _context.SaveChangesAsync();
                }

                CleanTempFiles();
                return RedirectToAction("PaymentFailure");
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
            if (HttpContext.Session.TryGetValue("TenderTempPath", out var pathBytes))
            {
                var path = Encoding.UTF8.GetString(pathBytes);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        }

        private void ClearSessionData()
        {
            var keys = new[] { "TenderApplicationData", "TenderTempPath", "TenderFileName", "PendingPayment" };
            foreach (var key in keys)
            {
                HttpContext.Session.Remove(key);
            }
        }

        [HttpGet]
        public IActionResult PaymentSuccess(int tenderId)
        {


            var paymentDetails = _context.TenderDetails
           .Where(t => t.TenderId == tenderId)
           .Select(t => new PaymentEdit
           {
               TenderId = t.TenderId,
               TenderTitle = t.Title,
               PaymentAmount = 10,
               PaymentDate = DateTime.UtcNow.AddMinutes(345), // Nepali time
               PayFromUser = new UserListEdit
               {
                   UserId = (short)Convert.ToInt16(User.Identity!.Name),
                   FirstName = _context.UserLists
                       .Where(u => u.UserId == Convert.ToInt16(User.Identity.Name))
                       .Select(u => u.FirstName + " " + u.LastName)
                       .FirstOrDefault()
               },
               PayToUser = new UserListEdit
               {
                   UserId = t.PublishedByUserId,
                   FirstName = _context.UserLists
                       .Where(u => u.UserId == t.PublishedByUserId)
                       .Select(u => u.FirstName + " " + u.LastName)
                       .FirstOrDefault()
               },
               PayFromCompany = new CompanyEdit
               {
                   CompanyId = _context.Companies
                    .Where(c => c.UserbidId == Convert.ToInt16(User.Identity.Name))
                    .Select(c => c.CompanyId)
                    .FirstOrDefault(),
                   CompanyName = _context.Companies
                    .Where(c => c.UserbidId == Convert.ToInt16(User.Identity.Name))
                    .Select(c => c.CompanyName)
                    .FirstOrDefault(),
                   FullAddress = _context.Companies
                    .Where(c => c.UserbidId == Convert.ToInt16(User.Identity.Name))
                    .Select(c => c.FullAddress)
                    .FirstOrDefault()
               }
           })
           .FirstOrDefault();





            if (paymentDetails == null)
            {
                return NotFound();
            }

            // Clear session on success
            HttpContext.Session.Remove("PendingTenderApplication");
            HttpContext.Session.Remove("TempFilePath");
            HttpContext.Session.Remove("PendingPayment");

            return View(paymentDetails);
        }

        [HttpGet]
        public IActionResult PaymentFailure()
        {
            // Clean up temp files if they exist
            var tempFilePath = HttpContext.Session.GetString("TempFilePath");
            if (!string.IsNullOrEmpty(tempFilePath) && System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }

            // Clear session
            HttpContext.Session.Remove("PendingTenderApplication");
            HttpContext.Session.Remove("TempFilePath");
            HttpContext.Session.Remove("PendingPayment");

            return View();
        }


    }
}
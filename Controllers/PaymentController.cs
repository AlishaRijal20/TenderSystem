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
                                 .status-badge {{
                                     display: inline-block;
                                     padding: 5px 10px;
                                     border-radius: 20px;
                                     font-weight: bold;
                                     background-color: #fef3c7;
                                     color: #92400e;
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
                                     <h2>New Tender Proposal Received</h2>
                                 </div>
                                 <div class='email-content'>
                                     <p>Dear Publisher,</p>
                                     <p>A new proposal has been submitted for your tender:</p>

                                     <table class='info-table'>
                                         <tr>
                                             <td>Tender Title:</td>
                                             <td>{tender.Title}</td>
                                         </tr>
                                         <tr>
                                             <td>Company Name:</td>
                                             <td>{company?.CompanyName}</td>
                                         </tr>
                                         <tr>
                                             <td>Proposed Budget:</td>
                                             <td>Rs.{tenderApplication.ProposedBudget}</td>
                                         </tr>
                                         <tr>
                                             <td>Proposed Duration:</td>
                                             <td>{tenderApplication.ProposedDuration}</td>
                                         </tr>
                                         <tr>
                                             <td>Submission Date:</td>
                                             <td>{DateTime.Now.ToString("dd MMM yyyy")}</td>
                                         </tr>
                                         <tr>
                                             <td>Status:</td>
                                             <td><span class='status-badge'>Pending Review</span></td>
                                         </tr>
                                     </table>

                                     <p>Please review this proposal at your earliest convenience.</p>

                                     <p>You can accept or reject this proposal after reviewing all details.</p>
                                 </div>
                                 <div class='email-footer'>
                                     <p>This is an automated message from BidNetra. Please do not reply to this email.</p>
                                     <p>&copy; {DateTime.Now.Year} BidNetra. All rights reserved.</p>
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
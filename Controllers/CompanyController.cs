using TenderSystem.Models;
using TenderSystem.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TenderSystem.Controllers
{
    public class CompanyController : Controller
    {
        private readonly TenderSystemContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;


        public CompanyController(TenderSystemContext context, DataSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
        }

        public IActionResult Index(string id, int? tenderId)
        {
            //return Json(tenderId);
            try
            {
                int companyId = Convert.ToInt32(_protector.Unprotect(id));

                var company = _context.Companies
                    .Include(c => c.TenderDetails)
                    .Where(c => c.CompanyId == companyId)
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
                        Rating = c.Rating,
                        UserbidId = c.UserbidId,
                        EncId = _protector.Protect(c.CompanyId.ToString()),
                        // Include user details
                        User = new UserListEdit
                        {
                            UserId = c.Userbid.UserId,
                            FirstName = c.Userbid.FirstName,
                            MiddleName = c.Userbid.MiddleName,
                            LastName = c.Userbid.LastName,
                            EmailAddress = c.Userbid.EmailAddress,
                            Phone = c.Userbid.Phone,
                            Province = c.Userbid.Province,
                            District = c.Userbid.District,
                            City = c.Userbid.City,
                            UserRole = c.Userbid.UserRole,
                            UserPhoto = c.Userbid.UserPhoto
                        },
                        Tenders = tenderId.HasValue
                            ? c.TenderDetails
                                .Where(t => t.TenderId == tenderId)
                                .Select(t => new TenderEdit
                                {
                                    TenderId = t.TenderId,
                                    Title = t.Title,
                                    EncId = _protector.Protect(t.TenderId.ToString())
                                }).FirstOrDefault() ?? new TenderEdit()
                            : new TenderEdit()

                    })
                    .FirstOrDefault();



                if (company == null)
                {
                    return NotFound("Company not found.");
                }

                return View(company);
            }
            catch (Exception ex)
            {
                return NotFound("Invalid company ID or company not found.");
            }
        }






        public IActionResult ViewReview(string id, string tenderId)
        {
            try
            {
                int companyId = Convert.ToInt32(_protector.Unprotect(id));
                int decodedTenderId = Convert.ToInt32(_protector.Unprotect(tenderId));
                //return Json(decodedTenderId);
                var company = _context.Companies.FirstOrDefault(c => c.CompanyId == companyId);
                var tender = _context.TenderDetails.FirstOrDefault(t => t.TenderId == decodedTenderId);

                int currentUserId = Convert.ToInt16(User.Identity!.Name);

                var hasReviewed = _context.Ratings.Any(r => r.RatingFor == companyId && r.RatingBy == currentUserId);

                if (company == null || tender == null)
                {
                    return NotFound("Company or tender not found.");
                }

                var model = new CompanyReviewViewModel
                {
                    CompanyId = id,
                    CompanyName = company.CompanyName,
                    CompanyRating = company.Rating,
                    TenderId = decodedTenderId,
                    HasUserReviewed = hasReviewed,
                    EncTenderId = tenderId,
                    NewRating = new RatingEdit(),
                    Reviews = _context.Ratings
                        .Include(r => r.RatingByNavigation)
                        .Where(r => r.RatingFor == companyId)
                        .Select(r => new RatingEdit
                        {
                            RatingId = r.RatingId,
                            Rate = (decimal)r.Rate,
                            RatingDescription = r.RatingDescription,
                            ReviewerName = $"{r.RatingByNavigation.FirstName} {r.RatingByNavigation.LastName}",
                            ReviewerPhoto = r.RatingByNavigation.UserPhoto
                        })
                        .ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                return NotFound("Invalid company ID, tender ID, or reviews not found.");
            }
        }

        [HttpPost]

        public IActionResult AddRating(RatingEdit r, string companyid, string tenderid)
        {
            try
            {
                int decryptedCompanyId = Convert.ToInt32(_protector.Unprotect(r.CompanyId));

                // Add new rating
                short newRatingId = 1;
                if (_context.Ratings.Any())
                {
                    newRatingId = (short)(_context.Ratings.Max(c => c.RatingId) + 1);
                }

                var newRating = new Rating
                {
                    RatingId = newRatingId,
                    RatingDescription = r.RatingDescription,
                    Rate = (decimal)r.Rate,  // Ensure this is decimal in your Rating model
                    RatingBy = Convert.ToInt16(User.Identity!.Name),
                    RatingFor = (short?)decryptedCompanyId
                };

                _context.Ratings.Add(newRating);
                _context.SaveChanges();



                // Calculate new average rating
                var avgrating = _context.Ratings
                    .Where(rt => rt.RatingFor == decryptedCompanyId)
                    .Average(rt => rt.Rate);

                // Find the company and update rating
                var company = _context.Companies.FirstOrDefault(c => c.CompanyId == decryptedCompanyId);
                if (company == null)
                {
                    throw new Exception("Company not found.");
                }

                company.Rating = avgrating;
                _context.Entry(company).State = EntityState.Modified;
                _context.SaveChanges();

                return RedirectToAction("ViewReview", new { id = companyid, tenderId = tenderid });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return RedirectToAction("ViewReview", new { id = r.CompanyId, tenderId = r.EncTenderId });
            }
        }

        [HttpGet]
        public IActionResult ViewCompanyReview(string id, string applicationId)
        {
            try
            {
                int companyId = Convert.ToInt32(_protector.Unprotect(id));
                var company = _context.Companies.FirstOrDefault(c => c.CompanyId == companyId);


                if (company == null)
                {
                    return NotFound("Company not found.");
                }


                var model = new CompanyReviewViewModel
                {
                    CompanyId = id,
                    CompanyName = company.CompanyName,
                    CompanyRating = company.Rating,
                    ApplicationId = applicationId,
                    NewRating = new RatingEdit(),
                    Reviews = _context.Ratings
                        .Include(r => r.RatingByNavigation)
                        .Where(r => r.RatingFor == companyId)
                        .Select(r => new RatingEdit
                        {
                            RatingId = r.RatingId,
                            Rate = (decimal)r.Rate,
                            RatingDescription = r.RatingDescription,
                            ReviewerName = $"{r.RatingByNavigation.FirstName} {r.RatingByNavigation.LastName}",
                            ReviewerPhoto = r.RatingByNavigation.UserPhoto
                        })
                        .ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                return NotFound("Invalid company ID or reviews not found.");
            }
        }

    }
}
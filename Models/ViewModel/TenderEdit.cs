using System.ComponentModel.DataAnnotations;
using TenderSystem.Models;

namespace TenderSystem.Models
{
    public class TenderEdit
    {

        public short TenderId { get; set; }

        public string Title { get; set; } = null!;

        public string? TenderDescription { get; set; }

        public decimal BudgetEstimation { get; set; }

        public string IssuedBy { get; set; } = null!;

        public DateOnly IssuedDate { get; set; }

        public DateOnly OpeningDate { get; set; }

        public DateOnly ClosingDate { get; set; }

        public string TenderType { get; set; } = null!;

        public string? ProjectDuration { get; set; }

        public string TenderDocument { get; set; } = null!;

        public short? AwardCompanyId { get; set; }

        public DateOnly? AwardDate { get; set; }

        public short PublishedByUserId { get; set; }

        public string IsVerified { get; set; } = null!;

        public string TenderStatus { get; set; } = null!;

        public string AwardStatus { get; set; }

        public string EncId { get; set; } = null!;

        [DataType(DataType.Upload)]
        public IFormFile? TenderFile { get; set; } = null!;

        public TenderApplicationEdit Application { get; set; }

        public CompanyEdit? AwardedCompany { get; set; }

        public string? PaymentStatus { get; set; }

        public short? PaymentId { get; set; }


    }
}
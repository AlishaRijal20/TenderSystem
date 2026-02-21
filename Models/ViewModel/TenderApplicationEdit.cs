using System.ComponentModel.DataAnnotations;

namespace TenderSystem.Models
{
    public class TenderApplicationEdit
    {
        [Key]
        public short ApplicationId { get; set; }

        public short TenderAppllyId { get; set; }

        public short CompanyApplyId { get; set; }

        public decimal ProposedBudget { get; set; }

        public string ProposedDuration { get; set; } = null!;

        public string ApplicationDocument { get; set; } = null!;

        public string ApplicationStatus { get; set; } = null!;

        public string EncId { get; set; } = null!;

        [DataType(DataType.Upload)]
        public IFormFile? TenderFile { get; set; } = null!;

    }
}
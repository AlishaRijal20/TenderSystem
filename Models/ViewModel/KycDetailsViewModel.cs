using TenderSystem.Models;

namespace TenderSystem.Models
{
    public class KycDetailsViewModel
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string FullAddress { get; set; }
        public string OfficeEmail { get; set; }
        public string CompanyWebsiteUrl { get; set; }
        public string RegistrationNumber { get; set; }
        public string RegistrationDocument { get; set; }
        public string PanNumber { get; set; }
        public string PanDocument { get; set; }
        public string CompanyType { get; set; }
        public string Position { get; set; }
        public decimal? Rating { get; set; }
        public bool IsVerified { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public List<BankDetailsViewModel> BankDetails { get; set; }

        public UserListEdit User { get; set; }
        public CompanyEdit Company { get; set; }
        public BankEdit Bank { get; set; }
    }
}
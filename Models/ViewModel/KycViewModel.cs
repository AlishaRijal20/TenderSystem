namespace TenderSystem.Models
{
    public class KycViewModel
    {
        public int UserId { get; set; }
        public string UserEncId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string Phone { get; set; }
        public bool HasCompany { get; set; }
        public bool HasBank { get; set; }
        public bool IsVerified { get; set; }

        public int BankId { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string RegistrationNumber { get; set; }
        public string PanNumber { get; set; }

        public UserListEdit User { get; set; }
    }
}
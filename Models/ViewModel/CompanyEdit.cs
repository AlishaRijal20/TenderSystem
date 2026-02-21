namespace TenderSystem.Models
{
    public class CompanyEdit
    {
        public short CompanyId { get; set; }

        public string CompanyName { get; set; } = null!;

        public string FullAddress { get; set; } = null!;

        public string OfficeEmail { get; set; } = null!;

        public string? CompanyWebsiteUrl { get; set; }

        public string RegistrationNumber { get; set; } = null!;

        public string RegistrationDocument { get; set; } = null!;

        public string PanNumber { get; set; } = null!;

        public string PanDocument { get; set; } = null!;

        public string CompanyType { get; set; } = null!;

        public string Position { get; set; } = null!;

        public string EncId { get; set; } = null!;

        public decimal? Rating { get; set; }
        public short? UserbidId { get; set; }

        public UserListEdit User { get; set; }

        public TenderEdit Tenders { get; set; }

        public bool IsVerified { get; set; }


    }
}
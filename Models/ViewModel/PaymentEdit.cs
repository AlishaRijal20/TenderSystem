namespace TenderSystem.Models
{
    public class PaymentEdit
    {
        public short TenderId { get; set; }

        public short AuctionId { get; set; }

        public string AuctionTitle { get; set; }
        public string TenderTitle { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow.AddMinutes(345);

        public UserListEdit PayFromUser { get; set; }

        public UserListEdit PayToUser { get; set; }
        public CompanyEdit PayToCompany { get; set; }

        public CompanyEdit PayFromCompany { get; set; }
        public string EncryptedTenderId { get; set; }

        public TenderEdit PayTenderId { get; set; }



    }
}
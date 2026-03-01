namespace TenderSystem.Models
{
    public class RatingEdit
    {
        public short RatingId { get; set; }

        public string? RatingDescription { get; set; }

        public decimal? Rating1 { get; set; }

        public decimal? Rate { get; set; }
        public short? RatingBy { get; set; }

        public short? RatingFor { get; set; }

        public string ReviewerName { get; set; }
        public string? ReviewerPhoto { get; set; }
        public string CompanyName { get; set; }

        public decimal? CompanyRating { get; set; }

        public string CompanyId { get; set; }

        public string EncTenderId { get; set; }


    }
}
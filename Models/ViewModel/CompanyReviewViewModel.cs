namespace TenderSystem.Models
{
    public class CompanyReviewViewModel
    {
        public int TenderId { get; set; }
        public string EncTenderId { get; set; }
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public decimal? CompanyRating { get; set; }
        public List<RatingEdit> Reviews { get; set; }

        public RatingEdit NewRating { get; set; }

        public string ApplicationId { get; set; }
        public string EncId { get; set; }

        public bool HasUserReviewed { get; set; }
    }
}
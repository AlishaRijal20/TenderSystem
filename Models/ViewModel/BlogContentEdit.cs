using System.ComponentModel.DataAnnotations;

namespace TenderSystem.Models
{
    public class BlogContentEdit
    {
        public short Bid { get; set; }

        public string SectionHeading { get; set; }

        public string SectionImage { get; set; }

        public string SectionDescription { get; set; }

        public DateOnly Postdate { get; set; }

        public short? UploadUserId { get; set; }

        public string EncId { get; set; } = null!;


        [DataType(DataType.Upload)]
        public IFormFile? BlogFile { get; set; } = null!;

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public string UserPhoto { get; set; }
    }
}
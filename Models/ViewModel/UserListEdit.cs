using System.ComponentModel.DataAnnotations;

namespace TenderSystem.Models
{
    public class UserListEdit
    {

        // UserList 
        public short UserId { get; set; }

        public string FirstName { get; set; } = null!;

        public string? MiddleName { get; set; }

        public string LastName { get; set; } = null!;

        public string Province { get; set; } = null!;

        public string District { get; set; } = null!;

        public string City { get; set; } = null!;

        public string Gender { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+977[0-9]{10}$",
        ErrorMessage = "Phone number must start with +977 followed by 10 digits")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z]+\.[a-zA-Z]{2,}$",
        ErrorMessage = "Please enter a valid email address (e.g., user@gmail.com)")]
        public string EmailAddress { get; set; } = null!;
        public string? UserPhoto { get; set; }

        public string UserRole { get; set; } = null!;

        public string UserPassword { get; set; } = null!;

        public string EncId { get; set; } = null!;

        public string UserEncId { get; set; }


        [DataType(DataType.Upload)]
        public IFormFile? UserFile { get; set; } = null!;

        public string EmailToken { get; set; } = null!;

        //Company
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

        public decimal? Rating { get; set; }

        public string Position { get; set; } = null!;

        public short? UserbidId { get; set; }

        [DataType(DataType.Upload)]
        public IFormFile? RegisterFile { get; set; } = null!;

        [DataType(DataType.Upload)]
        public IFormFile? PanFile { get; set; } = null!;

        public bool IsVerified { get; set; } = false;
        //Bank

        public short BankId { get; set; }

        public string BankName { get; set; } = null!;

        public string AccountNumber { get; set; } = null!;

        public string AccountType { get; set; } = null!;

        public string AccountHolderName { get; set; } = null!;

        public short? UserbankId { get; set; }

    }
}
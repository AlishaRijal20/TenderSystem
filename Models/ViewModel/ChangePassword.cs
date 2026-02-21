using System.ComponentModel.DataAnnotations;

namespace TenderSystem.Models
{
    public class ChangePassword
    {
        [Required(ErrorMessage = "Please, Enter your new password")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Please, Enter your current Password")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = null!;

        [Required(ErrorMessage = "Please, Enter your confirm password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = null!;

        public string EmailToken { get; set; } = null!;
        public string EmailAddress { get; set; } = null!;
    }
}
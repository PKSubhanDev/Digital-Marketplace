using System.ComponentModel.DataAnnotations;
using UoNMarketPlace.Model;

namespace UoNMarketPlace.ViewModel
{
    public class RegisterViewModel
    {
        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9_\-\.]+@uon\.edu\.au$", ErrorMessage = "Please enter a valid e-mail address with the domain uon.edu.au")]
        [Required(ErrorMessage = "Email is Required")]
        public string Email { get; set; }
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Password is Required")]
        public string Password { get; set; }
        [Required(ErrorMessage = "UserName is Required")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Role is Required")]
        public Role Role { get; set; }
        [Required(ErrorMessage = "Gender is Required")]
        public Gender Gender { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace UoNMarketPlace.ViewModel
{
    public class LoginViewModel
    {
        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9_\-\.]+@uon\.edu\.au$", ErrorMessage = "Please enter a valid e-mail address with the domain uon.edu.au")]
        [Required(ErrorMessage = "Email is Required")]
        public string Email { get; set; }
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Password is Required")]
        public string Password { get; set; }
    }
}

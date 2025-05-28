using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UoNMarketPlace.DataContext;
using UoNMarketPlace.Model;
using UoNMarketPlace.ViewModel;

namespace UoNMarketPlace.Controllers
{
    public class LoginController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UoNDB _context;
        public LoginController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager, UoNDB context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
        }
        #region HttpGet
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        #endregion
        #region HttpPost
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var signedUser = await _userManager.FindByEmailAsync(model.Email);
                if(signedUser ==  null)
                {
                    ModelState.AddModelError(string.Empty, "The Email you entered is not existed need to sign up");
                    return View(model);
                }
                var isPasswordValid = await _userManager.CheckPasswordAsync(signedUser, model.Password);
                var result = await _signInManager.PasswordSignInAsync(signedUser.UserName, model.Password, false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Retrieve the user
                    var user = await _signInManager.UserManager.FindByEmailAsync(model.Email);

                    // Retrieve user roles
                    var roles = await _userManager.GetRolesAsync(user);

                    // Now, 'roles' contains the roles associated with the user

                    // Add user ID as a claim
                    await _signInManager.UserManager.AddClaimAsync(user, new Claim(ClaimTypes.NameIdentifier, user.Id));

                    // Redirect to the appropriate action
                    if (roles.Contains("Student"))
                    {
                        return RedirectToAction("LandingPage", "Product");
                    }
                    else if (roles.Contains("Alumini"))
                    {
                        return RedirectToAction("Forum", "Alumini");
                    }
                    else if (roles.Contains("Admin"))
                    {
                        return RedirectToAction("AdminLandingPage", "Admin");
                    }
                    //else
                    //{
                    //    return RedirectToAction("Index", "Service");
                    //}
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt");
            }

            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                // Log or print the error message
                var errorMessage = error.ErrorMessage;
                // Handle the error as needed
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userExist = await _userManager.FindByEmailAsync(model.Email);

                if (userExist == null)
                {
                    var user = new User
                    {
                        Email = model.Email,
                        UserName = model.UserName,
                        Gender = model.Gender,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        EmailConfirmed = true
                    };
                    // Check if the role exists, and create it if not
                    var roleExists = await _roleManager.RoleExistsAsync(model.Role.ToString());
                    if (!roleExists)
                    {
                        var role = new IdentityRole(model.Role.ToString());
                        await _roleManager.CreateAsync(role);
                    }

                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, model.Role.ToString());
                        return RedirectToAction("Login", "Login");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "User with this email already exists");
                }
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
        #endregion

    }
}

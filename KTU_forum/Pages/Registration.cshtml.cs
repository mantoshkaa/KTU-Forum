using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KTU_forum.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; // Import for logging
using BCrypt.Net;
using KTU_forum.Data;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using System.Configuration;
using Microsoft.Extensions.Configuration;


namespace KTU_forum.Pages
{
    public class RegistrationModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegistrationModel> _logger;
        private readonly IConfiguration _configuration;
        // Logger for registration

        [BindProperty]
        public UserModel NewUser { get; set; }

        public RegistrationModel(ApplicationDbContext context, ILogger<RegistrationModel> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        // **API-Like Endpoint for JavaScript to Check Username Uniqueness**
        public JsonResult OnGetCheckUsername(string username)
        {
            bool exists = _context.Users.Any(u => u.Username == username);
            return new JsonResult(new { isTaken = exists });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var adminEmails = _configuration.GetSection("AdminSettings:AdminEmails").Get<List<string>>();

            // Log the registration attempt
            _logger.LogInformation($"User attempted to register with username: {NewUser.Username}");

            if (!NewUser.Email.EndsWith("@ktu.lt"))
            {
                _logger.LogWarning($"Invalid email domain: {NewUser.Email}. Only @ktu.lt emails are allowed.");

                ModelState.AddModelError("Email", "Only emails from @ktu.lt are allowed.");
                return Page();
            }

            // Check if email already exists
            if (_context.Users.Any(u => u.Email == NewUser.Email))
            {
                _logger.LogWarning($"Email '{NewUser.Email}' is already registered.");
                ModelState.AddModelError("NewUser.Email", "An account with this email already exists.");
                return Page();
            }

            // Check if username already exists
            if (_context.Users.Any(u => u.Username == NewUser.Username))
            {
                _logger.LogWarning($"Username '{NewUser.Username}' is already taken.");

                ModelState.AddModelError("NewUser.Username", "This username is already taken.");
                return Page();
            }
            if (!IsPasswordValid(NewUser.PasswordHash))
            {
                ModelState.AddModelError("NewUser.PasswordHash", "Password must be at least 6 characters long and contain at least one uppercase letter, one lowercase letter, and one number.");
                return Page();
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(NewUser.PasswordHash); // Hash the password before storing it
                       
            NewUser.PasswordHash = hashedPassword; // Store the hashed password in the database

            // Add this line to set the role based on email
            NewUser.Role = adminEmails.Contains(NewUser.Email) ? "Admin" : "Student";

            string token = Guid.NewGuid().ToString(); // or use any secure generator
            NewUser.EmailVerificationToken = token;
            NewUser.IsVerified = false;


            // Save the user 
            _context.Users.Add(NewUser);
            _context.SaveChanges();

            // Generate verification URL
            var verificationUrl = Url.Page(
                "/EmailVerification",
                null,
                new { token = NewUser.EmailVerificationToken },
                Request.Scheme
            );

            // Send the email
            await SendVerificationEmailAsync(NewUser.Email, verificationUrl);

            // Log successful registration
            _logger.LogInformation($"User '{NewUser.Username}' successfully registered.");

            return RedirectToPage("/Login"); // Redirect after successful registration
        }

        public async Task OnGetAsync()
        {
            try
            {
                // Retrieve all users from the DATABASE asynchronously
                var users = await _context.Users.ToListAsync();

                // Log all users (just for debugging or inspection purposes)
                foreach (var user in users)
                {
                    _logger.LogInformation($"Retrieved user - Username: {user.Username}, Email: {user.Email}");
                }
            }
            catch (Exception ex)
            {
                // Log any errors that occur during the database retrieval
                _logger.LogError($"An error occurred while retrieving users: {ex.Message}");
            }
        }
        private async Task SendVerificationEmailAsync(string email, string verificationLink)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("KTU Forum", "ktu.forum.test@gmail.com"));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = "Email Verification Request";
            message.Body = new TextPart("plain")
            {
                Text = $"To verify your email, click the link below:\n\n{verificationLink}\n\nIf you did not create an account, you can ignore this email."
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync("ktu.forum.test@gmail.com", "erub womf uykd bhdo");
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        private bool IsPasswordValid(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            var hasUpper = password.Any(char.IsUpper);
            var hasLower = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);

            return password.Length >= 6 && hasUpper && hasLower && hasDigit;
        }



    }
}

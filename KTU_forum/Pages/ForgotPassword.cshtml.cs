using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using KTU_forum.Models;
using KTU_forum.Data;
using System;
using System.Net;
using System.Net.Mail;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;

namespace KTU_forum.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(ApplicationDbContext context, ILogger<ForgotPasswordModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public string Email { get; set; }

        public string Message { get; set; }
        public string Error { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email))
            {
                Error = "Please enter a valid email address.";
                return Page();
            }

            // Check if the email exists in the database
            var user = _context.Users.FirstOrDefault(u => u.Email == Email);

            if (user == null)
            {
                Error = "No account associated with that email.";
                return Page();
            }

            // Generate a password reset token (this can be a GUID or a secure token)
            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Set expiry to 1 hour

            // Save the token in the database
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Generate the reset link (assumes ResetPassword is a Razor Page with a token query parameter)
           // var resetLink = Url.Page("/ResetPassword", new { token = user.PasswordResetToken });
            // Generate verification URL
            var resetLink = Url.Page(
                "/ResetPassword",
            null,
                new { token =  user.PasswordResetToken },
                Request.Scheme
            );

            // Send the email using SMTP directly
            var smtpHost = "smtp.gmail.com"; // Your SMTP server
            var smtpPort = 465; // SMTP Port

            var smtpUser = "ktu.forum.test@gmail.com"; // Your email
            var smtpPassword = "erub womf uykd bhdo"; // Your email password

            var fromAddress = new MailAddress(smtpUser, "KTU Forum");
            var toAddress = new MailAddress(user.Email);

            using (var smtpClient = new SmtpClient(smtpHost))
            {

                smtpClient.Host = "smtp.gmail.com";
                smtpClient.Port = 587;
                smtpClient.EnableSsl = true; // This enables STARTTLS
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);

                using (var message = new MailMessage(fromAddress, toAddress)
                //using (var message = new MailMessage(fromAddress, new MailAddress("gintarejasiuk@gmail.com"))
                {
                    Subject = "Password Reset Request",
                    Body = $"To reset your password, click the link below:\n\n{resetLink}\n\nThis link will expire in 1 hour."
                })

                    try
                    {
                        await smtpClient.SendMailAsync(message);

                        // Log the password reset request
                        _logger.LogInformation($"Password reset link sent to {Email}");

                        // Show the message to the user
                        Message = "If an account with that email exists, we have sent a password reset link.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send password reset email.");
                        Error = "Failed to send reset link. Please try again later.";
                    }
            }

            return Page();
        }
    }
}

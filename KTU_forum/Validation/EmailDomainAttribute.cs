using System.ComponentModel.DataAnnotations;
using System;

namespace KTU_forum.Validation
{
    public class EmailDomainAttribute : ValidationAttribute
    {
        private readonly string _allowedDomain;

        public EmailDomainAttribute(string allowedDomain)
        {
            _allowedDomain = allowedDomain;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var email = value as string;
            if (email != null && email.EndsWith("@" + _allowedDomain, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Success;
            }
            return new ValidationResult($"Email must be from the @{_allowedDomain} domain.");
        }
    }
}

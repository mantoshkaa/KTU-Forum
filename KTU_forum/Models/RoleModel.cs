using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KTU_forum.Models
{
    public class RoleModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        // Color for display in UI (hex code or color name)
        public string Color { get; set; }

        // Priority for display when user has multiple roles (lower number = higher priority)
        public int DisplayPriority { get; set; }

        // Navigation property - users with this role
        public List<UserRoleModel> UserRoles { get; set; } = new();
    }
}
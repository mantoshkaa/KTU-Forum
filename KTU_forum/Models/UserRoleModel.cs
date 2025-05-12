using System;

namespace KTU_forum.Models
{
    public class UserRoleModel
    {
        public int UserId { get; set; }
        public UserModel User { get; set; }

        public int RoleId { get; set; }
        public RoleModel Role { get; set; }

        // When this role was assigned to the user
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
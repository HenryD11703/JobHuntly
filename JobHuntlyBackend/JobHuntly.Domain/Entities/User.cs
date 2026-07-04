using System;
using System.Collections.Generic;

namespace JobHuntly.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relación 1 a muchos: Un usuario tiene muchas postulaciones
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}

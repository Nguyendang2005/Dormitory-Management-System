using System;

namespace DormCare.Domain.Entities
{
    public class AuditLog
    {
        public long AuditLogId { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

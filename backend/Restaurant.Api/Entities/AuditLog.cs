using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class AuditLog
{
    public long AuditLogId { get; set; }

    public int? UserId { get; set; }

    public string ActionName { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? OldData { get; set; }

    public string? NewData { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}

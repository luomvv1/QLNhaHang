using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class AiConversation
{
    public int ConversationId { get; set; }

    public int? CustomerId { get; set; }

    public string? Title { get; set; }

    public string ConversationStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AiMessage> AiMessages { get; set; } = new List<AiMessage>();

    public virtual Customer? Customer { get; set; }
}

using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class AiMessage
{
    public int MessageId { get; set; }

    public int ConversationId { get; set; }

    public string SenderType { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? RecommendedFoodIds { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AiConversation Conversation { get; set; } = null!;
}

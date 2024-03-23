using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace comp4870project.Model;

public class SavedMessage
{
    [Key]
    public Guid ID { get; set; } // Primary Key

    public Guid MessageId { get; set; } // All versions of same message in different languages share this

    public string? Language { get; set; } // Language of the message

    public string? SenderName { get; set; } // Name of Sender
    
    public bool OriginalMessage { get; set; } // If this is an untranslated message

    public string? Content { get; set; } // Content of the message

    public Guid ConversationId { get; set; } // Id of the conversation

    public DateTime SentDate { get; set; } // Time that the message was sent
}

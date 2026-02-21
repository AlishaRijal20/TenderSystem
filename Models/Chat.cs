using System;
using System.Collections.Generic;

namespace TenderSystem.Models;

public partial class Chat
{
    public short ChatId { get; set; }

    public short SenderId { get; set; }

    public short ReceiverId { get; set; }

    public string MessageText { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public virtual UserList Receiver { get; set; } = null!;

    public virtual UserList Sender { get; set; } = null!;
}

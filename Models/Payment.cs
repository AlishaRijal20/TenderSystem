using System;
using System.Collections.Generic;

namespace TenderSystem.Models;

public partial class Payment
{
    public short PaymentId { get; set; }

    public short? PayTenderId { get; set; }

    public short? PayCompanyId { get; set; }

    public short PayToUser { get; set; }

    public short PayByUser { get; set; }

    public decimal PaymentAmount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? SlipUpload { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public virtual UserList PayByUserNavigation { get; set; } = null!;

    public virtual Company? PayCompany { get; set; }

    public virtual TenderDetail? PayTender { get; set; }

    public virtual UserList PayToUserNavigation { get; set; } = null!;
}

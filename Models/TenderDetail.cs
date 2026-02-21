using System;
using System.Collections.Generic;

namespace TenderSystem.Models;

public partial class TenderDetail
{
    public short TenderId { get; set; }

    public string Title { get; set; } = null!;

    public string? TenderDescription { get; set; }

    public decimal BudgetEstimation { get; set; }

    public string IssuedBy { get; set; } = null!;

    public DateOnly IssuedDate { get; set; }

    public DateOnly OpeningDate { get; set; }

    public DateOnly ClosingDate { get; set; }

    public string TenderType { get; set; } = null!;

    public string? ProjectDuration { get; set; }

    public string TenderDocument { get; set; } = null!;

    public short? AwardCompanyId { get; set; }

    public DateOnly? AwardDate { get; set; }

    public short PublishedByUserId { get; set; }

    public string IsVerified { get; set; } = null!;

    public string TenderStatus { get; set; } = null!;

    public string AwardStatus { get; set; } = null!;

    public virtual Company? AwardCompany { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual UserList PublishedByUser { get; set; } = null!;

    public virtual ICollection<TenderApplication> TenderApplications { get; set; } = new List<TenderApplication>();
}

using System;
using System.Collections.Generic;

namespace TenderSystem.Models;

public partial class TenderApplication
{
    public short ApplicationId { get; set; }

    public short TenderAppllyId { get; set; }

    public short CompanyApplyId { get; set; }

    public decimal ProposedBudget { get; set; }

    public string ProposedDuration { get; set; } = null!;

    public string ApplicationDocument { get; set; } = null!;

    public string ApplicationStatus { get; set; } = null!;

    public virtual Company CompanyApply { get; set; } = null!;

    public virtual TenderDetail TenderApplly { get; set; } = null!;
}

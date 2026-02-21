using System;
using System.Collections.Generic;

namespace TenderSystem.Models;

public partial class Company
{
    public short CompanyId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string FullAddress { get; set; } = null!;

    public string OfficeEmail { get; set; } = null!;

    public string? CompanyWebsiteUrl { get; set; }

    public string RegistrationNumber { get; set; } = null!;

    public string RegistrationDocument { get; set; } = null!;

    public string PanNumber { get; set; } = null!;

    public string PanDocument { get; set; } = null!;

    public string CompanyType { get; set; } = null!;

    public string Position { get; set; } = null!;

    public decimal? Rating { get; set; }

    public short? UserbidId { get; set; }

    public bool IsVerified { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual ICollection<TenderApplication> TenderApplications { get; set; } = new List<TenderApplication>();

    public virtual ICollection<TenderDetail> TenderDetails { get; set; } = new List<TenderDetail>();

    public virtual UserList? Userbid { get; set; }
}

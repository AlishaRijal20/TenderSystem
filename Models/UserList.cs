using System;
using System.Collections.Generic;

namespace TenderSystem.Models;

public partial class UserList
{
    public short UserId { get; set; }

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public string Province { get; set; } = null!;

    public string District { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string EmailAddress { get; set; } = null!;

    public string? UserPhoto { get; set; }

    public string UserRole { get; set; } = null!;

    public string UserPassword { get; set; } = null!;

    public virtual ICollection<Bank> Banks { get; set; } = new List<Bank>();

    public virtual ICollection<BlogContent> BlogContents { get; set; } = new List<BlogContent>();

    public virtual ICollection<Chat> ChatReceivers { get; set; } = new List<Chat>();

    public virtual ICollection<Chat> ChatSenders { get; set; } = new List<Chat>();

    public virtual ICollection<Company> Companies { get; set; } = new List<Company>();

    public virtual ICollection<Payment> PaymentPayByUserNavigations { get; set; } = new List<Payment>();

    public virtual ICollection<Payment> PaymentPayToUserNavigations { get; set; } = new List<Payment>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual ICollection<TenderDetail> TenderDetails { get; set; } = new List<TenderDetail>();
}

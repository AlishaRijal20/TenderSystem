using System;
using System.Collections.Generic;

namespace TenderSystem.Models;

public partial class Bank
{
    public short BankId { get; set; }

    public string BankName { get; set; } = null!;

    public string AccountNumber { get; set; } = null!;

    public string AccountType { get; set; } = null!;

    public string AccountHolderName { get; set; } = null!;

    public short? UserbankId { get; set; }

    public bool IsVerified { get; set; }

    public virtual UserList? Userbank { get; set; }
}

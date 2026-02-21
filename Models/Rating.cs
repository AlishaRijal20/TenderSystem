using System;
using System.Collections.Generic;

namespace TenderSystem.Models;

public partial class Rating
{
    public short RatingId { get; set; }

    public string? RatingDescription { get; set; }

    public decimal? Rating1 { get; set; }

    public short? RatingBy { get; set; }

    public short? RatingFor { get; set; }

    public decimal Rate { get; set; }

    public virtual UserList? RatingByNavigation { get; set; }

    public virtual Company? RatingForNavigation { get; set; }
}

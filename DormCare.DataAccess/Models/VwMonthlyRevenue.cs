using System;
using System.Collections.Generic;

namespace DormCare.DataAccess.Models;

public partial class VwMonthlyRevenue
{
    public int? PaymentYear { get; set; }

    public int? PaymentMonth { get; set; }

    public decimal? TotalRevenue { get; set; }

    public int? PaymentCount { get; set; }
}

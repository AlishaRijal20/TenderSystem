using TenderSystem.Models;

public class PublisherDashboardViewModel
{
    public int ActiveTenders { get; set; }
    public int ActiveAuctions { get; set; }
    public int TotalBidders { get; set; }
    public int CompletionRate { get; set; }
    public List<ActivityViewModel> RecentActivities { get; set; } = new List<ActivityViewModel>();

    // For statistics, calculate month-over-month changes
    public int TendersChange { get; set; } = 8; // Default to 8% for demo
    public int BiddersChange { get; set; } = 24; // Default to 24% for demo
    public int CompletionRateChange { get; set; } = 3; // Default to 3% for demo
}
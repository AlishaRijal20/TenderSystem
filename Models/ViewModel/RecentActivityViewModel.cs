namespace TenderSystem.Models
{
    public class ActivityViewModel
    {
        public string Type { get; set; } // "Tender"
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Time { get; set; }
        public string Status { get; set; }
        public string IconClass { get; set; }

        public string GetTimeDisplay()
        {
            var now = DateTime.Now;
            var diff = now - Time;

            if (diff.TotalDays < 1)
            {
                return Time.ToString("Today, h:mm tt");
            }
            else if (diff.TotalDays < 2)
            {
                return "Yesterday, " + Time.ToString("h:mm tt");
            }
            else if (diff.TotalDays < 7)
            {
                return $"{(int)diff.TotalDays} days ago";
            }
            else
            {
                return Time.ToString("MMM d, yyyy");
            }
        }

        public string GetStatusClass()
        {
            return Status.ToLower() switch
            {
                "completed" or "verified" or "awarded" or "won" => "status-success",
                "pending" => "status-pending",
                _ => "status-pending"
            };
        }
    }
}
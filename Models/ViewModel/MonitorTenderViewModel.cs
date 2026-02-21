using TenderSystem.Services;

namespace TenderSystem.Models
{
    public class MonitorTenderViewModel
    {
        public TenderEdit Tender { get; set; }
        public List<TenderApplicationViewModel> Applications { get; set; }

    }
}
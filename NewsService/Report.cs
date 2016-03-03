using System;

namespace NewsService
{
    public class Report
    {
        public DateTime PublicationDateTime { get; set; }
        public string CompanyName { get; set; }
        public string ReportLink { get; set; }
        public string ReportKind { get; set; }
    }
}

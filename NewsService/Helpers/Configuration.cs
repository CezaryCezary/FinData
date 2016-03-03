using System;
using System.Web.Hosting;

namespace NewsService.Helpers
{
    public interface IConfiguration
    {
        DateTime LastReportsCheckDateTime { get; set; }
        DateTime NewReportsCheckDateTime { get; }
        string Path { get; set; }
    }

    public class Configuration : IConfiguration
    {
        private string _path;

        public DateTime LastReportsCheckDateTime { get; set; }
        public DateTime NewReportsCheckDateTime { get {return DateTime.Now;} }
        public string Path
        {
            get { return _path ?? (_path = HostingEnvironment.MapPath(@"~/App_Data/LastReportsCheckDateTime.json")); }
            set { _path = value; }
        }
    }
}

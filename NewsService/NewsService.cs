using CsQuery;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using log4net;
using NewsService.Helpers;

namespace NewsService
{
    /// <summary>
    /// Service is searching newly added reports from News Portal
    /// </summary>
    public class NewsService : INewsService
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IConfiguration _config;
        private readonly ISerializer _serializer;
        private readonly IUrlBuilder _urlBuilder;
        private readonly IPageGetter _pageGetter;

        public DateTime LastReportsCheckDateTime { get; set; }

        /// <summary>
        /// Ctor with default <see cref="Configuration"/>
        /// </summary>
        public NewsService() : this(new Configuration()) {}

        /// <summary>
        /// Ctor with <see cref="Configuration"/> and default <see cref="Serializer"/>
        /// </summary>
        /// <param name="config"><see cref="Configuration"/> class for News Service</param>
        public NewsService(IConfiguration config) : this(config, new Serializer(), new PageGetter(), new UrlBuilder()) {}

        /// <summary>
        /// Ctor using given <see cref="Configuration"/>, <see cref="Serializer"/> 
        /// <see cref="PageGetter"/> and <see cref="UrlBuilder"/> classes
        /// </summary>
        /// <param name="config"><see cref="Configuration"/> class for News Service</param>
        /// <param name="serializer"><see cref="Serializer"/> class for serializing last reports DateTime check</param>
        /// <param name="pageGetter"><see cref="PageGetter"/></param>
        /// <param name="urlBuilder"><see cref="UrlBuilder"/></param>
        public NewsService(IConfiguration config, ISerializer serializer, IPageGetter pageGetter, IUrlBuilder urlBuilder)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (pageGetter == null) throw new ArgumentNullException(nameof(pageGetter));
            if (urlBuilder == null) throw new ArgumentNullException(nameof(urlBuilder));

            _config = config;
            _serializer = serializer;
            _urlBuilder = urlBuilder;
            _pageGetter = pageGetter;
            //TODO: remove LastReportsCheckDateTime from NewsService, use this one from Configuration class, read(deserialize) in GetReportsFromLastCheckTime()
            LastReportsCheckDateTime = _config.LastReportsCheckDateTime == default(DateTime)
                ? _serializer.Deserialize<DateTime>(_config.Path)
                : default(DateTime);
            Log.Info("News service ctor called");
        }

        public List<Report> GetReportsFromLastCheckTime()
        {
            var reports = new List<Report>();
            var reportsCheckDateTime = _config.NewReportsCheckDateTime;

            try
            {
                while (LastReportsCheckDateTime < reportsCheckDateTime)
                {
                    var pageNumber = 0;
                    int? numOfPages = null;
                    string url;
                    var dom = CQ.Create();

                    do
                    {
                        ++pageNumber;
                        url = _urlBuilder.GetUrlFromDate(LastReportsCheckDateTime, pageNumber);
                        dom = _pageGetter.GetPage(url);

                        if (numOfPages == null)
                        {
                            numOfPages = GetNumberOfPages(dom, url);
                        }
                        
                        var reportsDom = dom[".espi .inf"];
                        foreach (var report in reportsDom)
                        {
                            var tempReport = CreateReportFromCqObj(report.InnerHTML);
                        
                            if (tempReport == null)
                                break;
                            reports.Add(tempReport);
                        }
                    } while (pageNumber < numOfPages);

                    UpdateLastReportsCheckDateTime(reportsCheckDateTime);

                    Log.InfoFormat("{0} reports parsed from page(date) {1}", reports.Count, url);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Exception is thrown from reason {0}. Number of parsed reports {1}", ex.Message, reports.Count);
            }
            return reports;
        }

        private static int GetNumberOfPages(CQ dom, string url)
        {
            var pagesExists = CQ.CreateFragment(dom[".stronicowanie b"])[1];
            if (pagesExists == null)
            {
                return 1;
            }
            int pages;
            var isConversionSuccessful = int.TryParse(pagesExists.InnerText, out pages);
            if (!isConversionSuccessful)
                Log.ErrorFormat("Error when parsing number of pages from value {0} from page {1}", pagesExists.InnerText, url);

            return isConversionSuccessful ? pages : 1;
        }

        private Report CreateReportFromCqObj(string reportHtml)
        {
            var reportDom = CQ.CreateFragment(reportHtml)["td"];

            try
            {
                var publicationDateTime = GetPublicationDateTime(reportDom[0].InnerText);

                if (publicationDateTime.TimeOfDay <= LastReportsCheckDateTime.TimeOfDay)
                    return null;

                return new Report
                {
                    PublicationDateTime = publicationDateTime,
                    CompanyName = GetCompanyName(reportDom[2].InnerHTML),
                    ReportLink = GetReportLink(reportDom[3].InnerHTML),
                    ReportKind = GetReportKind(reportDom[3].InnerHTML)
                };
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Exception is thrown from reason {0}", ex.Message);
                return new Report();
            }
        }

        private DateTime GetPublicationDateTime(string time)
        {
            var dayTime = TimeSpan.ParseExact(time, "hh\\:mm", CultureInfo.InvariantCulture);
            return new DateTime(
                LastReportsCheckDateTime.Year,
                LastReportsCheckDateTime.Month,
                LastReportsCheckDateTime.Day,
                dayTime.Hours,
                dayTime.Minutes,
                dayTime.Seconds);
        }

        private static string GetCompanyName(string companyNameHtml)
        {
            string cmpName = CQ.CreateFragment(companyNameHtml)["b"][0].InnerText;
            return cmpName.EndsWith(" SA") ? cmpName.Substring(0, cmpName.Length-3) : cmpName;
        }

        private string GetReportLink(string reportLinkHtml)
        {
            var startPosition = reportLinkHtml.IndexOf(_urlBuilder.GetServerAddress(), StringComparison.InvariantCulture);
            return reportLinkHtml.Substring(startPosition).Split('"')[0];
        }

        private static string GetReportKind(string reportKindHtml)
        {
            return CQ.CreateFragment(reportKindHtml)["a"][0].InnerText.Trim();
        }

        private void UpdateLastReportsCheckDateTime(DateTime reportsCheckDateTime)
        {
            if (LastReportsCheckDateTime.Date == reportsCheckDateTime.Date)
            {
                UpdateAndSerializeLastReportsCheckDateTime(reportsCheckDateTime);
            }
            else
            {
                AddDayAndResetTimeOfLastReportsCheckDateTime();
            }
        }

        private void UpdateAndSerializeLastReportsCheckDateTime(DateTime reportsDateTime)
        {
            LastReportsCheckDateTime = reportsDateTime;
            _config.LastReportsCheckDateTime = LastReportsCheckDateTime;
            _serializer.Serialize(_config.LastReportsCheckDateTime, _config.Path);
        }

        private void AddDayAndResetTimeOfLastReportsCheckDateTime()
        {
            LastReportsCheckDateTime = LastReportsCheckDateTime.AddDays(1);
            LastReportsCheckDateTime = LastReportsCheckDateTime.Add(-LastReportsCheckDateTime.TimeOfDay);
        }
    }
}

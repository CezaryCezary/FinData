using NewsService;
using System;
using System.Linq;
using CsQuery;
using NewsService.Helpers;
using Rhino.Mocks;
using Xunit;

namespace NewsServiceTests
{
    public class NewsServiceTests
    {
        private readonly INewsService _sut;
        private readonly IConfiguration _config;
        private readonly IPageGetter _pageGetter;
        private readonly IUrlBuilder _urlBuilder;
        private const string SampleUrl = "sampleUrl";

        public NewsServiceTests()
        {
            _config = MockRepository.GenerateMock<IConfiguration>();
            _config.Path = "LastReportsCheckDateTime.json";
            var serializer = MockRepository.GenerateMock<ISerializer>();
            serializer.Stub(e => e.Deserialize<DateTime>(_config.Path)).Return(default(DateTime));
            _pageGetter = MockRepository.GenerateMock<IPageGetter>();
            _urlBuilder = MockRepository.GenerateMock<IUrlBuilder>();
            _urlBuilder.Stub(e => e.GetUrlFromDate(Arg<DateTime>.Is.Anything, Arg<int>.Is.Equal(1)))
                .Return(SampleUrl);
            _urlBuilder.Stub(e => e.GetServerAddress()).Return("http");

            _sut = new NewsService.NewsService(_config, serializer, _pageGetter, _urlBuilder);

            _config.AssertWasCalled(e => e.Path);
            _config.AssertWasCalled(e => e.LastReportsCheckDateTime);
            serializer.AssertWasCalled(e => e.Deserialize<DateTime>(_config.Path));
            Assert.IsType<NewsService.NewsService>(_sut);
        }
        
        [Fact]
        public void GetReportsFromLastCheckTime_ReturnsEmptyReportsList_WhenPageWithoutReports()
        {
            _sut.LastReportsCheckDateTime = new DateTime(2015, 9, 16);
            _config.Stub(e => e.NewReportsCheckDateTime).Return(_sut.LastReportsCheckDateTime.AddDays(1));
            SetUpPageGetterStubGetPageMethod(@"..\..\TestFiles\Raporty 2015-09-16.html");
            
            var reports = _sut.GetReportsFromLastCheckTime();

            Assert.Equal(0, reports.Count);
            Assert.Equal(new DateTime(2015, 9, 17), _sut.LastReportsCheckDateTime);
        }

        [Fact]
        public void GetReportsFromLastCheckTime_ReturnsOneReport_WhenPageWithReport()
        {
            var lastReportsCheckDateTime = new DateTime(2015, 9, 14);
            _sut.LastReportsCheckDateTime = lastReportsCheckDateTime;
            _config.Stub(e => e.NewReportsCheckDateTime).Return(lastReportsCheckDateTime.AddDays(1));
            SetUpPageGetterStubGetPageMethod(@"..\..\TestFiles\Raporty 2015-09-14.html");

            var reports = _sut.GetReportsFromLastCheckTime();

            var report = new Report
            {
                PublicationDateTime = lastReportsCheckDateTime.Add(new TimeSpan(9, 33, 0)),
                CompanyName = "Izo-Blok",
                ReportKind = "Raport kwartalny"
            };
            Assert.Equal(1, reports.Count);
            Assert.Equal(report.CompanyName, reports.First().CompanyName);
            Assert.Equal(report.ReportKind, reports.First().ReportKind);
            Assert.Equal(report.PublicationDateTime, reports.First().PublicationDateTime);
        }

        [Fact]
        public void GetReportsFromLastCheckTime_Returns21Reports_WhenTwoReportPages()
        {
            const string secondPageUrl = "second page url";
            _sut.LastReportsCheckDateTime = new DateTime(2015, 8, 26);
            _config.Stub(e => e.NewReportsCheckDateTime).Return(_sut.LastReportsCheckDateTime.AddDays(1));
            _urlBuilder.Stub(e => e.GetUrlFromDate(Arg<DateTime>.Is.Anything, Arg<int>.Is.Equal(2)))
                .Return(secondPageUrl);
            SetUpPageGetterStubGetPageMethod(@"..\..\TestFiles\Raporty 2015-08-26.html");
            _pageGetter.Stub(e => e.GetPage(secondPageUrl))
                .Return(CQ.CreateFromFile(@"..\..\TestFiles\Raporty 2015-08-26 page2.html"));

            var reports = _sut.GetReportsFromLastCheckTime();

            Assert.Equal(21, reports.Count);
        }

        [Fact]
        public void GetReportsFromLastCheckTime_Returns20Reports_WhenFullOneReportPage()
        {
            _sut.LastReportsCheckDateTime = new DateTime(2014, 5, 9);
            _config.Stub(e => e.NewReportsCheckDateTime).Return(_sut.LastReportsCheckDateTime.AddDays(1));
            SetUpPageGetterStubGetPageMethod(@"..\..\TestFiles\Raporty 2014-05-09.html");

            var reports = _sut.GetReportsFromLastCheckTime();

            Assert.Equal(20, reports.Count);
        }

        [Fact]
        public void GetReportsFromLastCheckTime_ReturnsOneAndZeroReports_WhenCalledTwice()
        {
            _sut.LastReportsCheckDateTime = new DateTime(2015, 9, 14);
            _config.Stub(e => e.NewReportsCheckDateTime).Return(_sut.LastReportsCheckDateTime.AddDays(1));
            SetUpPageGetterStubGetPageMethod(@"..\..\TestFiles\Raporty 2015-09-14.html");

            var reports = _sut.GetReportsFromLastCheckTime();
            Assert.Equal(1, reports.Count);

            reports = _sut.GetReportsFromLastCheckTime();
            Assert.Equal(0, reports.Count);
        }

        private void SetUpPageGetterStubGetPageMethod(string fileName)
        {
            _pageGetter.Stub(e => e.GetPage(SampleUrl))
                .Return(CQ.CreateFromFile(fileName));
        }
    }
}

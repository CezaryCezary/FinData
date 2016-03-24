using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace FinData.Web.IntegrationTests.Infrastructure
{
    [Trait("category", "acceptance")]
    public class SystemUnderTest
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public SystemUnderTest()
        {
            _server = new TestServer(TestServer.CreateBuilder().UseStartup<Startup>()); //.UseServer("Microsoft.AspNet.Server.Kester")
            _client = _server.CreateClient();
        }

        [Fact]
        public async Task ReturnHelloWorld()
        {
            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            Assert.Equal("Hello World!", responseString);
        }

        [Fact]
        public async Task NoCompaniesInTheSystem()
        {
            //TODO: implement me ;)
        }
    }
}
using System;
using System.Text;

namespace NewsService.Helpers
{
    public interface IUrlBuilder
    {
        string GetUrlFromDate(DateTime date, int pageNumber);
        string GetServerAddress();
    }
    public class UrlBuilder : IUrlBuilder
    {
        private const string ServerAddress = "aHR0cDovL2Jpem5lcy5wYXAucGw=";
        private const string ReportsAddress = ServerAddress + "L3BsL3JlcG9ydHMvZXNwaS90ZXJtLA==";

        private string DecodeFrom64(string encodedData)
        {
            var encodedDataAsBytes = Convert.FromBase64String(encodedData);
            return Encoding.ASCII.GetString(encodedDataAsBytes);
        }

        public string GetUrlFromDate(DateTime date, int pageNumber)
        {
            return DecodeFrom64(ReportsAddress)
                + date.Year + "," + date.Month + ","
                + date.Day + "," + pageNumber;
        }

        public string GetServerAddress()
        {
            return DecodeFrom64(ServerAddress);
        }
    }
}

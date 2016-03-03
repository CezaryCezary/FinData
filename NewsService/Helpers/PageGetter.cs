using CsQuery;

namespace NewsService.Helpers
{
    public interface IPageGetter
    {
        CQ GetPage(string url);
    }
    public class PageGetter : IPageGetter
    {
        public CQ GetPage(string url)
        {
            return CQ.CreateFromUrl(url);
        }
    }
}

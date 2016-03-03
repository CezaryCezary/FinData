using System;
using System.Collections.Generic;

namespace NewsService
{
    /// <summary>
    /// Service, which periodically check News Portal with searching newly added reports
    /// </summary>
    public interface INewsService
    {
        List<Report> GetReportsFromLastCheckTime();

        DateTime LastReportsCheckDateTime { get; set; }
    }
}

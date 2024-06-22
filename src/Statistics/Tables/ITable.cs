using Contingent.Models.Infrastructure;

namespace Contingent.Statistics.Tables;

public interface ITable
{
    public string DisplayedName { get; }
    public string HtmlContent { get; }
    public Period StatisticPeriod { get; set; }


}
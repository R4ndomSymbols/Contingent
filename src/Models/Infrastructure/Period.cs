using Contingent.Models.Domain.Orders;

namespace Contingent.Models.Infrastructure;

public class Period
{
    // Периодизация отчетности
    // Основной период на год x 1 октября x-1 года по 30 сентября x года включительно
    // Периодизация приказов на год 1 января - 31 декабря текущего года
    // Ожидаемый выпуск на год x - 1 октября x года по 30 октября x + 1 года включительно 
    public const int ORG_CREATION_YEAR = 1972;
    public readonly DateTime Start;
    public readonly DateTime End;

    public static Period CurrentOrderPeriodPeriodEndDate => GetOrderPeriodByYear(DateTime.Now.Year);

    public static Period CurrentReportingPeriod
    {
        get
        {
            var now = DateTime.Now;
            return GetReportingPeriodByYear(now.Year - now.Month < 10 ? 1 : 0);
        }
    }
    public static Period OrganizationLifetime => new Period(new DateTime(ORG_CREATION_YEAR, 1, 1), DateTime.Now.AddYears(1));

    public Period(DateTime start, DateTime end)
    {
        if (start > end)
        {
            throw new ArgumentException("Дата начала периода не может быть больше даты окончания");
        }
        Start = start;
        End = end;
    }

    public static Period GetReportingPeriodByYear(int startYear)
    {
        // 1 октября year
        // 30 сентября year + 1
        return new Period(new DateTime(startYear, 10, 1), new DateTime(startYear + 1, 9, 30));
    }
    public static Period GetOrderPeriodByYear(int startYear)
    {
        // 1 января year
        // 31 декабря year
        return new Period(new DateTime(startYear, 1, 1), new DateTime(startYear, 12, 31));
    }

    public static Period FromOrder(Order order)
    {
        return GetOrderPeriodByYear(order.SpecifiedDate.Year);
    }

    public bool IsWithin(DateTime date)
    {
        return date >= Start && date <= End;
    }
}
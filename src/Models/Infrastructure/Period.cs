using Contingent.DTOs.In;
using Contingent.Models.Domain.Orders;
using Contingent.Utilities;

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

    public static Period CurrentOrderPeriod => GetOrderPeriodByYear(DateTime.Now.Year);

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
    public Period(DateTime start)
    {
        Start = start;
        End = start;
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
    public bool IsEndedByDate(DateTime date)
    {
        return End < date;
    }

    public bool IsEndedNow()
    {
        return IsEndedByDate(DateTime.Now);
    }
    // -1 если не закончился,
    // +n, если закончился в определенном времени в прошлом
    public int GetEndedDaysAgoCount()
    {
        return EndedDaysAgoCount(DateTime.Today);
    }

    public int EndedDaysAgoCount(DateTime onDate)
    {
        if (!IsEndedByDate(onDate))
        {
            return -1;
        }
        return (onDate - End).Days;
    }

    public bool IsOneMoment()
    {
        return Start.Date == End.Date;
    }

    public static Result<Period> CreateFromDTO(PeriodDTO? dto)
    {
        if (dto is null)
        {
            return Result<Period>.Failure(new ValidationError(nameof(dto), "Период не указан"));
        }
        if (!Utils.TryParseDate(dto.StartDate, out DateTime start))
        {
            return Result<Period>.Failure(new ValidationError(nameof(Start), "Неверно указана дата начала периода"));
        };
        if (dto.EndDate is not null)
        {
            if (!Utils.TryParseDate(dto.EndDate, out DateTime end))
            {
                return Result<Period>.Failure(new ValidationError(nameof(End), "Неверно указана дата окончания периода"));
            }
            if (start > end)
            {
                return Result<Period>.Failure(new ValidationError(nameof(End), "Дата окончания периода не может быть меньше даты начала"));
            }
            return Result<Period>.Success(new Period(start, end));
        }
        return Result<Period>.Success(new Period(start));
    }

    public override string ToString()
    {
        return string.Format("{0} - {1}", Start.ToString("dd.MM.yyyy"), End.ToString("dd.MM.yyyy"));
    }
}
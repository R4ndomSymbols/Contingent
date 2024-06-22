using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Contingent.Models.Domain.Orders.OrderData;

public class OrderTypeInfo
{
    public readonly GroupDisplayBehavior FrontendGroupBehavior;
    public readonly string OrderTypeName;
    public readonly OrderTypes Type;
    private OrderTypeInfo(OrderTypes orderType, string orderTypeName, GroupDisplayBehavior behavior)
    {
        if (orderType.ToString().StartsWith("Free", StringComparison.OrdinalIgnoreCase))
        {
            OrderTypeName = orderTypeName + " (К)";
        }
        else
        {
            OrderTypeName = orderTypeName + " (ДК)";
        }
        Type = orderType;
        FrontendGroupBehavior = behavior;
    }

    public enum GroupDisplayBehavior
    {
        MustChange = 2,
        WipeOut = 3,
        Undefined = 4,
        PeriodInput = 5,
    }

    private static readonly Dictionary<OrderTypes, OrderTypeInfo> _types = new()
    {

        {OrderTypes.FreeEnrollment, new(
            OrderTypes.FreeEnrollment,
            "Зачисление на первый курс",
            GroupDisplayBehavior.MustChange
        )},
        {
            OrderTypes.FreeEnrollmentFromAnotherOrg,
            new(
                OrderTypes.FreeEnrollmentFromAnotherOrg,
                "Зачисление в порядке перевода",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.FreeReEnrollment,
            new(
                OrderTypes.FreeReEnrollment,
                "Зачисление в порядке восстановления",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.FreeTransferBetweenSpecialties,
            new(
                OrderTypes.FreeTransferBetweenSpecialties,
                "Перевод внутри организации (между специальностями)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.FreeTransferNextCourse,
            new(
                OrderTypes.FreeTransferNextCourse,
                "Перевод на следующий курс",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.FreeDeductionWithAcademicDebt,
            new(
                OrderTypes.FreeDeductionWithAcademicDebt,
                "Отчисление в связи с академической задолженностью",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.FreeDeductionWithGraduation,
            new(
                OrderTypes.FreeDeductionWithGraduation,
                "Отчисление в связи с выпуском",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.FreeDeductionWithOwnDesire,
            new(
                OrderTypes.FreeDeductionWithOwnDesire,
                "Отчисление по собственному желанию",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.FreeDeductionWithTransfer,
            new(
                OrderTypes.FreeDeductionWithTransfer,
                "Отчисление в связи с переводом в другую организацию",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.FreeDeductionWithAcademicVacationNoReturn,
            new(
                OrderTypes.FreeDeductionWithAcademicVacationNoReturn,
                "Отчисление в связи с невыходом из академического отпуска",
                GroupDisplayBehavior.WipeOut
            )
        },
         {
            OrderTypes.FreeDeductionWithEducationProcessNotInitiated,
            new(
                OrderTypes.FreeDeductionWithEducationProcessNotInitiated,
                "Отчисление в связи с неприступлением к обучению",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.FreeAcademicVacationSend,
            new (
                OrderTypes.FreeAcademicVacationSend,
                "О предоставлении академического отпуска",
                GroupDisplayBehavior.PeriodInput
            )
        },
        {
            OrderTypes.FreeAcademicVacationReturn,
            new (
                OrderTypes.FreeAcademicVacationReturn,
                "О восстановлении из академического отпуска",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidEnrollment,
            new(
                OrderTypes.PaidEnrollment,
                "Зачисление на первый курс",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidEnrollmentWithTransfer,
            new(
                OrderTypes.PaidEnrollmentWithTransfer,
                "Зачисление в порядке перевода",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidReEnrollment,
            new(
                OrderTypes.PaidReEnrollment,
                "Зачисление в порядке восстановления",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidTransferBetweenSpecialties,
            new(
                OrderTypes.PaidTransferBetweenSpecialties,
                "Перевод внутри организации (между специальностями)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidTransferNextCourse,
            new(
                OrderTypes.PaidTransferNextCourse,
                "Перевод на следующий курс",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.FreeTransferFromPaidToFree,
            new(
                OrderTypes.FreeTransferFromPaidToFree,
                "Перевод на бюджет",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidDeductionWithAcademicDebt,
            new(
                OrderTypes.PaidDeductionWithAcademicDebt,
                "Отчисление в связи с академической задолженностью",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidDeductionWithGraduation,
            new(
                OrderTypes.PaidDeductionWithGraduation,
                "Отчисление в связи с выпуском",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidDeductionWithOwnDesire,
            new(
                OrderTypes.PaidDeductionWithOwnDesire,
                "Отчисление по собственному желанию",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidDeductionWithTransfer,
            new (
                OrderTypes.PaidDeductionWithTransfer,
                "Отчисление в связи с переводом в другую организацию",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidDeductionWithAcademicVacationNoReturn,
            new (
                OrderTypes.PaidDeductionWithTransfer,
                "Отчисление в связи с невыходом из академического отпуска",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidDeductionWithEducationProcessNotInitiated,
            new (
                OrderTypes.PaidDeductionWithTransfer,
                "Отчисление в связи с неприступлением к обучению",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidAcademicVacationReturn,
            new (
                OrderTypes.PaidAcademicVacationReturn,
                "Восстановление из академического отпуска",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidAcademicVacationSend,
            new (
                OrderTypes.PaidAcademicVacationSend,
                "О предоставлении академического отпуска",
                GroupDisplayBehavior.PeriodInput
            )
        },
        {
            OrderTypes.EmptyOrder,
            new(
                OrderTypes.EmptyOrder,
                "Не указано",
                GroupDisplayBehavior.Undefined
            )
        }
    };



    public static OrderTypeInfo GetByType(OrderTypes type)
    {
        if (_types.TryGetValue(type, out OrderTypeInfo? value))
        {
            return value;
        }
        else
        {
            throw new ArgumentException("Приказ типа " + type.ToString() + " не зарегистрирован");
        }
    }

    public static IEnumerable<OrderTypeInfo> GetAllTypes()
    {
        return _types.Select(x => x.Value);
    }

    public bool IsAnyEnrollment()
    {
        return Type.ToString().Contains("Enrollment", StringComparison.OrdinalIgnoreCase);
    }
    public bool IsAnyDeduction()
    {
        return Type.ToString().Contains("Deduction", StringComparison.OrdinalIgnoreCase);
    }
    public bool CanBePreviousToReEnrollment()
    {
        return IsAnyDeduction() && Type != OrderTypes.FreeDeductionWithGraduation && Type != OrderTypes.PaidDeductionWithGraduation;
    }
    public bool IsAcademicVacationSend()
    {
        return Type == OrderTypes.PaidAcademicVacationSend || Type == OrderTypes.FreeAcademicVacationSend;
    }

    public static int ImportOrderType(string? v)
    {
        if (v is null)
        {
            return (int)OrderTypes.EmptyOrder;
        }
        foreach (var pair in _types)
        {
            if (pair.Value.OrderTypeName == v)
            {
                return (int)pair.Key;
            }
        }
        return (int)OrderTypes.EmptyOrder;
    }
    public static IEnumerable<OrderTypeInfo> EnrollmentTypes { get; } = _types.Where(x => x.Value.IsAnyEnrollment()).Select(x => x.Value);

    public static IEnumerable<OrderTypeInfo> DeductionTypes { get; } = _types.Where(x => x.Value.IsAnyDeduction()).Select(x => x.Value);
    public static IEnumerable<OrderTypeInfo> AcademicVacationSendTypes { get; } = _types.Where(x => x.Value.IsAcademicVacationSend()).Select(x => x.Value);
    public static IEnumerable<OrderTypeInfo> AcademicVacationCloseTypes { get; } = _types.Where(x =>
    new OrderTypes[]{
        OrderTypes.PaidAcademicVacationReturn,
        OrderTypes.FreeAcademicVacationReturn,
        OrderTypes.FreeDeductionWithAcademicVacationNoReturn,
        OrderTypes.FreeDeductionWithOwnDesire,
        OrderTypes.PaidDeductionWithAcademicVacationNoReturn,
        OrderTypes.PaidDeductionWithOwnDesire,
    }.Any(t => t == x.Key)).Select(x => x.Value);
}

namespace Contingent.Models.Domain.Orders.OrderData;

public class OrderTypeInfo
{
    public readonly GroupDisplayBehavior FrontendGroupBehavior;
    public readonly string OrderTypeName;
    public readonly OrderTypes Type;
    public OrderTypeInfo(OrderTypes orderType, string orderTypeName, GroupDisplayBehavior behavior)
    {
        OrderTypeName = orderTypeName;
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
            "Зачисление на первый курс (бюджет)",
            GroupDisplayBehavior.MustChange
        )},
        {
            OrderTypes.FreeEnrollmentFromAnotherOrg,
            new(
                OrderTypes.FreeEnrollmentFromAnotherOrg,
                "Зачисление в порядке перевода (бюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.FreeReEnrollment,
            new(
                OrderTypes.FreeReEnrollment,
                "Зачисление в порядке восстановления (бюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.FreeTransferBetweenSpecialties,
            new(
                OrderTypes.FreeTransferBetweenSpecialties,
                "Перевод внутри организации (между специальностями) (бюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.FreeTransferNextCourse,
            new(
                OrderTypes.FreeTransferNextCourse,
                "Перевод на следующий курс (бюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.FreeDeductionWithAcademicDebt,
            new(
                OrderTypes.FreeDeductionWithAcademicDebt,
                "Отчисление в связи с академической задолженностью (бюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.FreeDeductionWithGraduation,
            new(
                OrderTypes.FreeDeductionWithGraduation,
                "Отчисление в связи с выпуском (бюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.FreeDeductionWithOwnDesire,
            new(
                OrderTypes.FreeDeductionWithOwnDesire,
                "Отчисление по собственному желанию (бюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.FreeDeductionWithTransfer,
            new(
                OrderTypes.FreeDeductionWithTransfer,
                "Отчисление в связи с переводом (бюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.FreeDeductionWithAcademicVacationNoReturn,
            new(
                OrderTypes.FreeDeductionWithAcademicVacationNoReturn,
                "Отчисление в связи с невыходом из академического отпуска (бюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
         {
            OrderTypes.FreeDeductionWithEducationProcessNotInitiated,
            new(
                OrderTypes.FreeDeductionWithEducationProcessNotInitiated,
                "Отчисление в связи с неприступлением к обучению (бюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.FreeAcademicVacationSend,
            new (
                OrderTypes.FreeAcademicVacationSend,
                "О предоставлении академического отпуска (бюджет)",
                GroupDisplayBehavior.PeriodInput
            )
        },
        {
            OrderTypes.FreeAcademicVacationReturn,
            new (
                OrderTypes.FreeAcademicVacationReturn,
                "О восстановлении из академического отпуска (бюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidEnrollment,
            new(
                OrderTypes.PaidEnrollment,
                "Зачисление на первый курс (внебюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidEnrollmentWithTransfer,
            new(
                OrderTypes.PaidEnrollmentWithTransfer,
                "Зачисление в порядке перевода (внебюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidReEnrollment,
            new(
                OrderTypes.PaidReEnrollment,
                "Зачисление в порядке восстановления (внебюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidTransferBetweenSpecialties,
            new(
                OrderTypes.PaidTransferBetweenSpecialties,
                "Перевод внутри организации (между специальностями) (внебюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidTransferNextCourse,
            new(
                OrderTypes.PaidTransferNextCourse,
                "Перевод на следующий курс (внебюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.FreeTransferFromPaidToFree,
            new(
                OrderTypes.FreeTransferFromPaidToFree,
                "Перевод на бюджет (бюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidDeductionWithAcademicDebt,
            new(
                OrderTypes.PaidDeductionWithAcademicDebt,
                "Отчисление в связи с академической задолженностью (внебюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidDeductionWithGraduation,
            new(
                OrderTypes.PaidDeductionWithGraduation,
                "Отчисление в связи с выпуском (внебюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidDeductionWithOwnDesire,
            new(
                OrderTypes.PaidDeductionWithOwnDesire,
                "Отчисление по собственному желанию (внебюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidDeductionWithTransfer,
            new (
                OrderTypes.PaidDeductionWithTransfer,
                "Отчисление в связи с переводом в другую организацию (внебюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidDeductionWithAcademicVacationNoReturn,
            new (
                OrderTypes.PaidDeductionWithTransfer,
                "Отчисление в связи с невыходом из академического отпуска (внебюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidDeductionWithEducationProcessNotInitiated,
            new (
                OrderTypes.PaidDeductionWithTransfer,
                "Отчисление в связи с неприступлением к обучению (внебюджет)",
                GroupDisplayBehavior.WipeOut
            )
        },
        {
            OrderTypes.PaidAcademicVacationReturn,
            new (
                OrderTypes.PaidAcademicVacationReturn,
                "Отчисление в связи с неприступлением к обучению (внебюджет)",
                GroupDisplayBehavior.MustChange
            )
        },
        {
            OrderTypes.PaidAcademicVacationSend,
            new (
                OrderTypes.PaidAcademicVacationSend,
                "Отчисление в связи с неприступлением к обучению (внебюджет)",
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
}

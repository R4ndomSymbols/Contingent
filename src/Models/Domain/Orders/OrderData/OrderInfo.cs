namespace Contingent.Models.Domain.Orders.OrderData;

public class OrderTypeInfo
{
    public readonly GroupDisplayBehaviour FrontendGroupBehaviour;
    public readonly string OrderTypeName;
    public readonly OrderTypes Type;
    public OrderTypeInfo(OrderTypes orderType, string orderTypeName, GroupDisplayBehaviour behaviour)
    {
        OrderTypeName = orderTypeName;
        Type = orderType;
        FrontendGroupBehaviour = behaviour;
    }

    public enum GroupDisplayBehaviour
    {
        NoChange = 1,
        MustChange = 2,
        Vipe = 3,
        Undefined = 4,
    }

    private static Dictionary<OrderTypes, OrderTypeInfo> _types = new Dictionary<OrderTypes, OrderTypeInfo>{

        {OrderTypes.FreeEnrollment, new(
            OrderTypes.FreeEnrollment,
            "Зачисление на первый курс (бюджет)",
            GroupDisplayBehaviour.MustChange
        )},
        {
            OrderTypes.FreeEnrollmentWithTransfer,
            new(
                OrderTypes.FreeEnrollmentWithTransfer,
                "Зачисление в порядке перевода (бюджет)",
                GroupDisplayBehaviour.MustChange
            )
        },
        {
            OrderTypes.FreeReenrollment,
            new(
                OrderTypes.FreeReenrollment,
                "Зачисление в порядке восстановления (бюджет)",
                GroupDisplayBehaviour.MustChange
            )
        },
        {
            OrderTypes.FreeTransferBetweenSpecialities,
            new(
                OrderTypes.FreeTransferBetweenSpecialities,
                "Перевод внутри организации (между специальностями) (бюджет)",
                GroupDisplayBehaviour.MustChange
            )
        },
        {
            OrderTypes.FreeTransferNextCourse,
            new(
                OrderTypes.FreeTransferNextCourse,
                "Перевод на следующий курс (бюджет)",
                GroupDisplayBehaviour.MustChange
            )
        },
        {
            OrderTypes.FreeDeductionWithAcademicDebt,
            new(
                OrderTypes.FreeDeductionWithAcademicDebt,
                "Отчисление в связи с академической задолженностью (бюджет)",
                GroupDisplayBehaviour.Vipe
            )
        },
        {
            OrderTypes.FreeDeductionWithGraduation,
            new(
                OrderTypes.FreeDeductionWithGraduation,
                "Отчисление в связи с выпуском (бюджет)",
                GroupDisplayBehaviour.Vipe
            )
        },
        {
            OrderTypes.FreeDeductionWithOwnDesire,
            new(
                OrderTypes.FreeDeductionWithOwnDesire,
                "Отчисление по собственному желанию (бюджет)",
                GroupDisplayBehaviour.Vipe
            )
        },
        {
            OrderTypes.PaidEnrollment,
            new(
                OrderTypes.PaidEnrollment,
                "Зачисление на первый курс (внебюджет)",
                GroupDisplayBehaviour.MustChange
            )
        },
        {
            OrderTypes.PaidEnrollmentWithTransfer,
            new(
                OrderTypes.PaidEnrollmentWithTransfer,
                "Зачисление в порядке перевода (внебюджет)",
                GroupDisplayBehaviour.MustChange
            )
        },
        {
            OrderTypes.PaidReenrollment,
            new(
                OrderTypes.PaidReenrollment,
                "Зачисление в порядке восстановления (внебюджет)",
                GroupDisplayBehaviour.MustChange
            )
        },
        {
            OrderTypes.PaidTransferBetweenSpecialities,
            new(
                OrderTypes.PaidTransferBetweenSpecialities,
                "Перевод внутри организации (между специальностями) (внебюджет)",
                GroupDisplayBehaviour.MustChange
            )
        },
        {
            OrderTypes.PaidTransferNextCourse,
            new(
                OrderTypes.PaidTransferNextCourse,
                "Перевод на следующий курс (внебюджет)",
                GroupDisplayBehaviour.MustChange
            )
        },
        {
            OrderTypes.PaidTransferFromPaidToFree,
            new(
                OrderTypes.PaidTransferFromPaidToFree,
                "Перевод на бюджет (внебюджет)",
                GroupDisplayBehaviour.MustChange
            )
        },
        {
            OrderTypes.PaidDeductionWithAcademicDebt,
            new(
                OrderTypes.PaidDeductionWithAcademicDebt,
                "Отчисление в связи с академической задолженностью (внебюджет)",
                GroupDisplayBehaviour.Vipe
            )
        },
        {
            OrderTypes.PaidDeductionWithGraduation,
            new(
                OrderTypes.PaidDeductionWithGraduation,
                "Отчисление в связи с выпуском (внебюджет)",
                GroupDisplayBehaviour.Vipe
            )
        },
        {
            OrderTypes.PaidDeductionWithOwnDesire,
            new(
                OrderTypes.PaidDeductionWithOwnDesire,
                "Отчисление по собственному желанию (внебюджет)",
                GroupDisplayBehaviour.Vipe
            )
        },
        {
            OrderTypes.PaidDeductionWithTransfer,
            new (
                OrderTypes.PaidDeductionWithTransfer,
                "Отчисление в связи с переводом в другую организаю (внебюджет)",
                GroupDisplayBehaviour.Vipe
            )
        },
        {
            OrderTypes.EmptyOrder,
            new(
                OrderTypes.EmptyOrder,
                "Не указано",
                GroupDisplayBehaviour.Undefined
            )
        }
    };



    public static OrderTypeInfo GetByType(OrderTypes type)
    {
        if (_types.ContainsKey(type))
        {
            return _types[type];
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
        return Type.ToString().Contains("Enrollment");
    }
    public bool IsAnyDeduction()
    {
        return Type.ToString().Contains("Deduction");
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

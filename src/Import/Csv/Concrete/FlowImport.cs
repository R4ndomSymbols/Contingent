using System.Security.Cryptography.X509Certificates;
using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;

namespace Contingent.Import.CSV;

public class FlowImport : ImportCSV
{
    private List<Order> _ordersToConduct;
    public bool CloseOrders { get; set; }
    public FlowImport(Stream dataSource, ObservableTransaction scope) : base(dataSource, scope)
    {
        _ordersToConduct = new List<Order>();
    }

    public override ResultWithoutValue Import()
    {
        var dtos = Read(() => new OrderIdentityDTO(), out List<CSVRow> rows);
        if (dtos.IsFailure)
        {
            return ResultWithoutValue.Failure(dtos.Errors);
        }
        int rowNumber = 1;
        foreach (var orderDTO in dtos.ResultObject)
        {
            if (string.IsNullOrEmpty(orderDTO.OrgId) || string.IsNullOrEmpty(orderDTO.OrderSpecifiedYear))
            {
                return ResultWithoutValue.Failure(new ImportValidationError(
                   string.Format("Не указан год приказа или его номер, строка {0} ", rowNumber)
                ));
            }
            if (!int.TryParse(orderDTO.OrderSpecifiedYear, out int year))
            {
                return ResultWithoutValue.Failure(new ImportValidationError(
                    "Год приказа указан неверно, строка " + rowNumber
                ));
            }
            var orderDetermined = _ordersToConduct.Find(o =>
                o.OrderOrgId.Equals(orderDTO.OrgId, StringComparison.OrdinalIgnoreCase) &&
                o.SpecifiedDate.Year == year
            );
            if (orderDetermined is null)
            {
                // поиск приказа в базе данных
                var ordersFromDb = Order.FindOrdersByParameters(new OrderSearchParameters()
                {
                    OrderOrgId = orderDTO.OrgId,
                    Year = year
                });
                if (ordersFromDb.Count != 1)
                {
                    return ResultWithoutValue.Failure(new ImportValidationError(
                        "Приказ не удалось определить однозначно, либо его не удалось найти, строка " + rowNumber
                    ));
                }
                orderDetermined = ordersFromDb.First();
                _ordersToConduct.Add(orderDetermined);
            }
            var mapped = orderDetermined.MapFromCSV(rows[rowNumber - 1]);
            if (mapped.IsFailure)
            {
                return ResultWithoutValue.Failure(new ImportValidationError(
                    string.Format("Ошибка при получении данных для проведения по приказу, строка {0}, ошибка: {1} ", rowNumber, mapped)
                ));
            }
            rowNumber++;

        }
        return ResultWithoutValue.Success();
    }

    public override ResultWithoutValue Save(bool commit)
    {
        // приказы нужно отсортировать
        // по дате, чтобы импортировать последовательно
        // в добавок, из-за механизма кеширования истории студентов, все студенты в приказах должны быть разными
        // даже если у них один и тот же ID
        _ordersToConduct.Sort(Order.OrderByEffectiveDateComparison);
        Console.WriteLine("ЧИСЛО ПРИКАЗОВ " + _ordersToConduct.Count);
        foreach (var order in _ordersToConduct)
        {
            var result = order.ConductByOrder(_scope);
            if (result.IsFailure)
            {
                FinishImport(false);
                return ResultWithoutValue.Failure(new ImportValidationError(result.ToString()));
            }
            if (CloseOrders)
            {
                order.Close(_scope);
            }
        }
        FinishImport(commit);
        return ResultWithoutValue.Success();
    }
}
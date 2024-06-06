using System.Security.Cryptography.X509Certificates;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;

namespace Contingent.Import;

public class FlowImport : IFromCSV<FlowImport>
{
    public const string GradeBookFieldName = "номер зачетной книжки";
    public const string StudentFullNameFieldName = "фамилия";
    public const string GroupFieldName = "группа";
    public const string OrderFieldOrgIdName = "приказ";
    public const string OrderFieldDateName = "дата приказа";
    private static int _index = 0;
    private static int CurrentNumber
    {
        get
        {
            _index++;
            return _index;
        }
    }

    private int _rowNumber;
    private Order? _toConduct;
    private FlowImportBatch _batch;
    public Order? ByOrder => _toConduct;
    public FlowImport(FlowImportBatch batch)
    {
        _batch = batch;
        _rowNumber = CurrentNumber;
    }
    public Result<FlowImport> MapFromCSV(CSVRow row)
    {
        string? orderOrgId = row[OrderFieldOrgIdName];
        string? orderDateRaw = row[OrderFieldDateName];
        Order orderDetermined = null!;
        if (orderOrgId is not null && int.TryParse(orderDateRaw, out int year) && !string.IsNullOrEmpty(orderOrgId))
        {
            var orders = _batch.GetOrderBy(orderOrgId, year);
            if (orders.Count == 0)
            {
                var ordersFromDb = Order.FindOrdersByParameters(new OrderSearchParameters()
                {
                    OrderOrgId = orderOrgId,
                    Year = year
                });
                if (ordersFromDb.Count != 1)
                {
                    return Result<FlowImport>.Failure(new ValidationError(
                        string.Format("Приказ на строке {0} не получилось однозначно определить", row.LineNumber)
                    ));
                }
                orderDetermined = ordersFromDb.First();
            }
            else if (orders.Count == 1)
            {
                orderDetermined = orders.First();
            }
            else
            {
                return Result<FlowImport>.Failure(new ValidationError(
                    string.Format("Приказ на строке {0} не получилось однозначно определить", row.LineNumber)
                ));
            }
            var orderInsertionResult = orderDetermined.MapFromCSV(row);
            if (orderInsertionResult.IsFailure)
            {
                return Result<FlowImport>.Failure(orderInsertionResult.Errors);
            }
            _toConduct = orderInsertionResult.ResultObject;
            _batch.AddImport(this);
            return Result<FlowImport>.Success(this);
        }
        return Result<FlowImport>.Failure(new ValidationError("Год или номер приказа не указан или указан неверно: строка " + row.LineNumber));

    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not FlowImport)
        {
            return false;
        }
        return ((FlowImport)obj)._rowNumber == _rowNumber;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}

public class FlowImportBatch
{
    private List<FlowImport> _imports;
    private List<Order> _ordersToConduct;

    public FlowImportBatch()
    {
        _imports = new List<FlowImport>();
        _ordersToConduct = new List<Order>();
    }

    public bool AddImport(FlowImport import)
    {
        if (import.ByOrder is not null)
        {
            if (!CheckImportUniqueness(import))
            {
                return false;
            }
            if (CheckOrderUniqueness(import.ByOrder))
            {
                _ordersToConduct.Add(import.ByOrder);
            }
            _imports.Add(import);
            return true;
        }
        return false;
    }

    public List<Order> GetOrderBy(string orgId, int year)
    {
        return _ordersToConduct.Where(o => o.OrderOrgId == orgId && o.SpecifiedDate.Year == year).ToList();
    }
    public ResultWithoutValue MassConduct()
    {
        foreach (var order in _ordersToConduct)
        {
            var conductionResult = order.ConductByOrder();
            if (conductionResult.IsFailure)
            {
                Console.WriteLine(conductionResult);
            }
        }
        _imports.Clear();
        _ordersToConduct.Clear();
        return ResultWithoutValue.Success();
    }

    private bool CheckOrderUniqueness(Order order)
    {
        return !_ordersToConduct.Any(x => x.Equals(order));
    }
    private bool CheckImportUniqueness(FlowImport import)
    {
        return !_imports.Any(x => x.Equals(import));
    }


}
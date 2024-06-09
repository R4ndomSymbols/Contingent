using System.Text.Json;
using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Orders;
using Contingent.Utilities;

namespace Contingent.Import.CSV;

public class OrderImport : ImportCSV
{
    private List<Order> _orders = new List<Order>();
    public OrderImport(Stream dataSource, ObservableTransaction scope) : base(dataSource, scope)
    {
        _orders = new List<Order>();
    }
    public override ResultWithoutValue Import()
    {
        var dtos = Read(() => new OrderDTO(), out List<CSVRow> rows);
        if (dtos.IsFailure)
        {
            return ResultWithoutValue.Failure(dtos.Errors);
        }
        foreach (var orderDTO in dtos.ResultObject)
        {
            var order = Order.Build(JsonSerializer.Serialize(orderDTO));
            if (order.IsFailure)
            {
                return ResultWithoutValue.Failure(order.Errors);
            }
            _orders.Add(order.ResultObject);
        }
        return ResultWithoutValue.Success();
    }
    public override ResultWithoutValue Save(bool commit)
    {
        foreach (var order in _orders)
        {
            order.Save(_scope);
        }
        FinishImport(commit);
        return ResultWithoutValue.Success();
    }
}
using System.Text.Json;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Orders;
using Utilities;

namespace StudentTracking.Import;

public class OrderImport : IFromCSV<OrderImport>
{
    public Order? ImportedOrder { get; set; }
    public OrderImport(){
        ImportedOrder = null;
    }
    public Result<OrderImport> MapFromCSV(CSVRow row)
    {
        var orderDTO = new OrderDTO().MapFromCSV(row).ResultObject;
        var order = Order.Build(JsonSerializer.Serialize(orderDTO));
        if (order.IsFailure){
            return Result<OrderImport>.Failure(order.Errors);
        }
        ImportedOrder = order.ResultObject;
        return Result<OrderImport>.Success(this);
    }
}
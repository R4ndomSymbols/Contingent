using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;

namespace Tests;

public class OrderRowDataSource : IRowSource
{

    private static readonly List<string> _orderTypeNames = OrderTypeInfo.GetAllTypes().Where(x => x.Type != Contingent.Models.Domain.Orders.OrderTypes.EmptyOrder).Select(x => x.OrderTypeName).ToList();
    private Random _rng;
    private string[] _data;
    private string[] _headers;

    public int ColumnCount => _headers.Length;

    public OrderRowDataSource()
    {
        _rng = new Random();
        _headers = new[] {
            OrderDTO.OrderNameFieldName,
            OrderDTO.OrderTypeFieldName,
            OrderDTO.SpecifiedDateFieldName,
            OrderDTO.EffectiveDateFieldName,
            OrderDTO.OrderDescriptionFieldName
        };
        UpdateState();
    }

    public string GetData(int pos)
    {
        return _data[pos];
    }

    public string? GetHeader(int pos)
    {
        return _headers[pos];
    }

    public void UpdateState()
    {
        _data = new string[_headers.Length];
        long tickOffset = new TimeSpan(5, 0, 0, 0).Ticks;
        DateTime orderDate = new DateTime(_rng.NextInt64(new DateTime(2020, 1, 1).Ticks, new DateTime(2025, 1, 1).Ticks));
        _data[2] = Utils.FormatDateTime(orderDate);
        _data[3] = Utils.FormatDateTime(
            new DateTime(orderDate.Ticks - (_rng.NextInt64(0, tickOffset * 2) - tickOffset))
        );
        _data[1] = RandomPicker<string>.Pick(_orderTypeNames);
        _data[0] = _data[1] + " от " + _data[2];
        _data[4] = _rng.NextInt64(10000L, 10000000).ToString();

    }

}



using Microsoft.AspNetCore.Http.Features;
using Npgsql;
using Utilities;

namespace StudentTracking.Models.Domain.Orders.Infrastructure;

public class FreeOrderSequentialGuardian : OrderSequentialGuardian
{

    private List<(FreeContingentOrder order, int bias)> _foundFree;

    private static FreeOrderSequentialGuardian? _instance;

    private FreeOrderSequentialGuardian()
    {
        _foundFree = new List<(FreeContingentOrder order, int bias)>();
    }

    public static FreeOrderSequentialGuardian Instance
    {
        get
        {
            _instance ??= new FreeOrderSequentialGuardian();
            return _instance;
        }
    }

    protected override int YearWithin
    {
        get => _yearEnd.Year;
        set
        {
            if (_yearStart.Year != value)
            {
                _yearStart = new DateTime(value, 1, 1);
                _yearEnd = new DateTime(value, 12, 31);
                SetSource(_yearStart, _yearEnd);
            }
        }
    }

    public override int GetSequentialOrderNumber(Order? toInsert)
    {
        if (toInsert is null || toInsert is not FreeContingentOrder)
        {
            throw new Exception("Приказ не может быть не указан и должнен иметь правильный тип");
        }
        YearWithin = toInsert.SpecifiedDate.Year;
        // если приказов на год нет, то это первый
        if (!_foundFree.Any())
        {
            return 1;
        }
        // если приказ уже есть, то возвращается он же
        if (_foundFree.Any(o => o.order.Equals(toInsert)))
        {
            return toInsert.OrderNumber;
        }
        // ищется приказ с минимально большей датой
        // его замещает приказ, который нужно вставить
        int orderNumber = 0;
        for (; orderNumber < _foundFree.Count && _foundFree[orderNumber].order.SpecifiedDate <= toInsert.SpecifiedDate; orderNumber++)
        // итерация до того момента, пока дата приказа в списке приказов меньше либо равна дате приказа
        {
            // неявная зависимость от способа сортировки времени, когда приказ был создан
            // приказ на дату, который создается, всегда будет хронологически последним среди
            // всех приказов с такой же датой
        }
        // индекс остановки совпадает с номером
        return ++orderNumber;
    }

    public override void Insert(Order toInsert)
    {
        var orderNumber = GetSequentialOrderNumber(toInsert);
        if (_foundFree.Any(o => o.order.Equals(toInsert)))
        {
            return;
        }
        _foundFree.Insert(orderNumber - 1, ((FreeContingentOrder)toInsert, 0));
        for (int i = orderNumber; i < _foundFree.Count; i++)
        {
            _foundFree[i] = (_foundFree[i].order, _foundFree[i].bias + 1);
        }
    }

    public override void Save()
    {
        Console.WriteLine(string.Join("\n", _foundFree.Select(x => x.order.SpecifiedDate + " " + x.bias + " " + x.order.Id + " " + x.order.OrderOrgId)));
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        var cmdText = "UPDATE orders SET serial_number = @p1, org_id = @p3 WHERE id = @p2";
        for (int orderIndex = 0; orderIndex < _foundFree.Count; orderIndex++)
        {
            if (_foundFree[orderIndex].order.Id == Utils.INVALID_ID)
            {
                throw new Exception("Приказ должен быть сохранен прежде, чем обновлен");
            }
            if (_foundFree[orderIndex].bias != 0 || _foundFree[orderIndex].order.OrderNumber!=orderIndex+1)
            {
                using var cmd = new NpgsqlCommand(cmdText, conn);
                // новый индекс приказа - складывается из текущего положения, единицы и смещения
                var newNumber = orderIndex + 1 + _foundFree[orderIndex].bias;
                // по идее, так быть не должно, но я не вижу другого способа инкапсулировать смену состояния
                _foundFree[orderIndex].order.OrderNumber = newNumber;
                cmd.Parameters.Add(new NpgsqlParameter<int>("p1", newNumber));
                cmd.Parameters.Add(new NpgsqlParameter<int>("p2", _foundFree[orderIndex].order.Id));
                cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _foundFree[orderIndex].order.OrderOrgId));
                cmd.ExecuteNonQuery();
                _foundFree[orderIndex] = (_foundFree[orderIndex].order, 0);
                
            }
        }
    }

    private void SetSource(DateTime start, DateTime end){
        _foundFree = Order.FindWithinRangeSortedByTime(start, end, SQL.OrderByCondition.OrderByTypes.ASC)
        .Where(ord => ord is FreeContingentOrder).Select(o => ((FreeContingentOrder)o,0)).ToList();
    }
}

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
            if (_instance is null)
            {
                _instance = new FreeOrderSequentialGuardian();
            }
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

    public override int GetSequentialIndex(Order? toInsert)
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
        if (_foundFree.Any(o => o.Equals(toInsert)))
        {
            return toInsert.OrderNumber;
        }
        // ищется приказ с минимально большей датой
        // его замещает приказ, который нужно вставить
        int i = 0;
        for (; i < _foundFree.Count && _foundFree[i].order.SpecifiedDate <= toInsert.SpecifiedDate; i++)
        // итерация до того момента, пока дата приказа в списке приказов меньше либо равна дате приказа
        {
            // неявная зависимость от способа сортировки времени, когда приказ был создан
            // приказ на дату, который создается, всегда будет хронологически последним среди
            // всех приказов с такой же датой
        }
        // индекс остановки совпадает с номером
        return ++i;
    }

    public override void Insert(Order toInsert)
    {
        var sequentialIndex = GetSequentialIndex(toInsert);
        if (_foundFree.Any(o => o.order.Equals(toInsert)))
        {
            Console.WriteLine(
                string.Join("\n", _foundFree.Select(x => x.order.Id + " " + x.bias + " " + x.order.OrderCreationDate.ToString()))
            );
            Console.WriteLine("Вставка: " + toInsert.Id);
            return;
        }
        _foundFree.Insert(sequentialIndex - 1, ((FreeContingentOrder)toInsert, 0));
        for (int i = sequentialIndex; i < _foundFree.Count; i++)
        {
            _foundFree[i] = (_foundFree[i].order, _foundFree[i].bias + 1);
        }
    }

    public override void Save()
    {
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        var cmdText = "UPDATE orders SET serial_number = @p1 WHERE id = @p2";
        for (int i = 0; i < _foundFree.Count; i++)
        {
            if (_foundFree[i].order.Id == Utils.INVALID_ID)
            {
                throw new Exception("Приказ должен быть сохранен прежде, чем обновлен");
            }
            if (_foundFree[i].bias != 0)
            {
                using var cmd = new NpgsqlCommand(cmdText, conn);
                // новый индекс приказа - складывается из текущего положения, единицы и смещения
                var newNumber = i + _foundFree[i].bias;
                cmd.Parameters.Add(new NpgsqlParameter<int>("p1", newNumber));
                cmd.Parameters.Add(new NpgsqlParameter<int>("p2", _foundFree[i].order.Id));
                cmd.ExecuteNonQuery();
                _foundFree[i] = (_foundFree[i].order, 0);
                // по идее, так быть не должно, но я не вижу другого способа инкапсулировать смену состояния
                _foundFree[i].order.OrderNumber = newNumber;
            }
        }
        SetSource(_yearStart, _yearEnd);
    }

    private void SetSource(DateTime start, DateTime end){
        _foundFree = Order.FindWithinRangeSortedByTime(start, end, SQL.OrderByCondition.OrderByTypes.ASC)
        .Where(ord => ord is FreeContingentOrder).Select(o => ((FreeContingentOrder)o,0)).ToList();
    }
}

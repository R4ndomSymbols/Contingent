using Npgsql;
using Utilities;

namespace Contingent.Models.Domain.Orders.Infrastructure;

public class PaidOrderSequentialGuardian : OrderSequentialGuardian
{

    private List<(AdditionalContingentOrder order, int bias)> _foundPaid;
    private static PaidOrderSequentialGuardian? _instance;

    private PaidOrderSequentialGuardian()
    {
        _foundPaid = new List<(AdditionalContingentOrder order, int bias)>();
    }

    public static PaidOrderSequentialGuardian Instance
    {
        get
        {
            if (_instance is null)
            {
                _instance = new PaidOrderSequentialGuardian();
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

    public override int GetSequentialOrderNumber(Order? toInsert)
    {
        if (toInsert is null || toInsert is not AdditionalContingentOrder)
        {
            throw new Exception("Приказ не может быть не указан и должнен иметь правильный тип");
        }
        YearWithin = toInsert.SpecifiedDate.Year;
        // если приказов на год нет, то это первый
        if (!_foundPaid.Any())
        {
            return 1;
        }
        // если приказ уже есть, то возвращается он же
        if (_foundPaid.Any(o => o.order.Equals(toInsert)))
        {
            return toInsert.OrderNumber;
        }
        // ищется приказ с минимально большей датой
        // его замещает приказ, который нужно вставить
        int i = 0;
        for (; i < _foundPaid.Count && _foundPaid[i].order.SpecifiedDate <= toInsert.SpecifiedDate; i++)
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
        var sequentialIndex = GetSequentialOrderNumber(toInsert);
        if (_foundPaid.Any(o => o.order.Equals(toInsert)))
        {
            return;
        }
        _foundPaid.Insert(sequentialIndex - 1, ((AdditionalContingentOrder)toInsert, 0));
        for (int i = sequentialIndex; i < _foundPaid.Count; i++)
        {
            // смещение приказа
            _foundPaid[i] = (_foundPaid[i].order, _foundPaid[i].bias + 1);
        }
    }

    public override void Save()
    {
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        var cmdText = "UPDATE orders SET serial_number = @p1, org_id = @p3 WHERE id = @p2";
        for (int i = 0; i < _foundPaid.Count; i++)
        {
            if (_foundPaid[i].order.Id == Utils.INVALID_ID)
            {
                throw new Exception("Приказ должен быть сохранен прежде, чем обновлен");
            }
            if (_foundPaid[i].bias != 0)
            {
                using var cmd = new NpgsqlCommand(cmdText, conn);
                // новый индекс приказа - складывается из текущего положения, единицы и смещения
                var newNumber = i + _foundPaid[i].bias;
                // по идее, так быть не должно, но я не вижу другого способа инкапсулировать смену состояния
                _foundPaid[i].order.OrderNumber = newNumber;
                cmd.Parameters.Add(new NpgsqlParameter<int>("p1", newNumber));
                cmd.Parameters.Add(new NpgsqlParameter<int>("p2", _foundPaid[i].order.Id));
                cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _foundPaid[i].order.OrderOrgId));
                cmd.ExecuteNonQuery();
                _foundPaid[i] = (_foundPaid[i].order, 0);
            }
        }
        SetSource(_yearStart, _yearEnd);
    }

    private void SetSource(DateTime start, DateTime end)
    {
        _foundPaid = Order.FindWithinRangeSortedByTime(start, end, SQL.OrderByCondition.OrderByTypes.ASC)
        .Where(ord => ord is AdditionalContingentOrder).Select(o => ((AdditionalContingentOrder)o, 0)).ToList();
    }
}
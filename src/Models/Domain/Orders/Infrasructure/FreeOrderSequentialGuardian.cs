using Microsoft.AspNetCore.Http.Features;
using Npgsql;
using Contingent.Utilities;

namespace Contingent.Models.Domain.Orders.Infrastructure;

public class FreeOrderSequentialGuardian : OrderSequentialGuardian
{

    private List<FreeContingentOrder> _foundFree;

    private static FreeOrderSequentialGuardian? _instance;

    private FreeOrderSequentialGuardian()
    {
        _foundFree = new List<FreeContingentOrder>();
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
    }

    public override int GetSequentialOrderNumber(Order? toInsert, ObservableTransaction? scope)
    {
        if (toInsert is null || toInsert is not FreeContingentOrder)
        {
            throw new Exception("Приказ не может быть не указан и должнен иметь правильный тип");
        }
        SetYearWithin(toInsert.SpecifiedDate.Year, scope);
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
        int orderNumber = 0;
        for (; orderNumber < _foundFree.Count && _foundFree[orderNumber].SpecifiedDate <= toInsert.SpecifiedDate; orderNumber++)
        // итерация до того момента, пока дата приказа в списке приказов меньше либо равна дате приказа
        {
            // неявная зависимость от способа сортировки времени, когда приказ был создан
            // приказ на дату, который создается, всегда будет хронологически последним среди
            // всех приказов с такой же датой
        }
        // индекс остановки совпадает с номером
        return ++orderNumber;
    }

    public override void Insert(Order toInsert, ObservableTransaction scope)
    {
        var orderNumber = GetSequentialOrderNumber(toInsert, scope);
        if (_foundFree.Any(o => o.Equals(toInsert)))
        {
            return;
        }
        _foundFree.Insert(orderNumber - 1, (FreeContingentOrder)toInsert);
    }

    public override void Save(ObservableTransaction scope)
    {
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        var cmdText = "UPDATE orders SET serial_number = @p1, org_id = @p3 WHERE id = @p2";
        for (int orderIndex = 0; orderIndex < _foundFree.Count; orderIndex++)
        {
            if (!Utils.IsValidId(_foundFree[orderIndex].Id))
            {
                throw new Exception("Приказ должен быть сохранен прежде, чем обновлен");
            }
            int rightPlacement = orderIndex + 1;
            // если смещение равно нулю или приказ на своем месте
            if (_foundFree[orderIndex].OrderNumber == rightPlacement)
            {
                continue;
            }
            NpgsqlCommand cmd;
            if (scope is not null)
            {
                cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            }
            else
            {
                cmd = new NpgsqlCommand(cmdText, conn);
            }
            // по идее, так быть не должно, но я не вижу другого способа инкапсулировать смену состояния
            _foundFree[orderIndex].OrderNumber = rightPlacement;
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", rightPlacement));
            cmd.Parameters.Add(new NpgsqlParameter<int>("p2", _foundFree[orderIndex].Id));
            cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _foundFree[orderIndex].OrderOrgId));
            using (cmd)
            {
                cmd.ExecuteNonQuery();
            }
        }
    }

    private void SetSource(DateTime start, DateTime end, ObservableTransaction? scope)
    {
        _foundFree = Order.FindWithinRangeSortedByTime(start, end, SQL.OrderByCondition.OrderByTypes.ASC, scope)
        .Where(ord => ord is FreeContingentOrder).Select(o => (FreeContingentOrder)o).ToList();
    }

    protected override void SetYearWithin(int year, ObservableTransaction? scope)
    {
        if (_yearStart.Year != year)
        {
            _yearStart = new DateTime(year, 1, 1);
            _yearEnd = new DateTime(year, 12, 31);
            SetSource(_yearStart, _yearEnd, scope);
        }

    }
}

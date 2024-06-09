using Npgsql;
using Contingent.Utilities;

namespace Contingent.Models.Domain.Orders.Infrastructure;

public class PaidOrderSequentialGuardian : OrderSequentialGuardian
{

    private List<AdditionalContingentOrder> _foundPaid;
    private static PaidOrderSequentialGuardian? _instance;

    private PaidOrderSequentialGuardian()
    {
        _foundPaid = new List<AdditionalContingentOrder>();
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
    }

    public override int GetSequentialOrderNumber(Order? toInsert, ObservableTransaction? scope)
    {
        if (toInsert is null || toInsert is not AdditionalContingentOrder)
        {
            throw new Exception("Приказ не может быть не указан и должнен иметь правильный тип");
        }
        SetYearWithin(toInsert.SpecifiedDate.Year, scope);
        // если приказов на год нет, то это первый
        if (!_foundPaid.Any())
        {
            return 1;
        }
        // если приказ уже есть, то возвращается он же
        if (_foundPaid.Any(o => o.Equals(toInsert)))
        {
            return toInsert.OrderNumber;
        }
        // ищется приказ с минимально большей датой
        // его замещает приказ, который нужно вставить
        int orderPlace = 0;
        for (; orderPlace < _foundPaid.Count && _foundPaid[orderPlace].SpecifiedDate <= toInsert.SpecifiedDate; orderPlace++)
        // итерация до того момента, пока дата приказа в списке приказов меньше либо равна дате приказа
        {
            // неявная зависимость от способа сортировки времени, когда приказ был создан
            // приказ на дату, который создается, всегда будет хронологически последним среди
            // всех приказов с такой же датой
        }
        // индекс остановки совпадает с номером
        return orderPlace;
    }

    public override void Insert(Order toInsert, ObservableTransaction scope)
    {
        var sequentialIndex = GetSequentialOrderNumber(toInsert, scope);
        if (_foundPaid.Any(o => o.Equals(toInsert)))
        {
            return;
        }
        _foundPaid.Insert(sequentialIndex, (AdditionalContingentOrder)toInsert);
    }

    public override void Save(ObservableTransaction scope)
    {
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        var cmdText = "UPDATE orders SET serial_number = @p1, org_id = @p3 WHERE id = @p2";
        for (int orderIndex = 0; orderIndex < _foundPaid.Count; orderIndex++)
        {
            if (!Utils.IsValidId(_foundPaid[orderIndex].Id))
            {
                throw new Exception("Приказ должен быть сохранен прежде, чем обновлен");
            }
            int rightPlacement = orderIndex + 1;
            if (_foundPaid[orderIndex].OrderNumber == rightPlacement)
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
            _foundPaid[orderIndex].OrderNumber = rightPlacement;
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", rightPlacement));
            cmd.Parameters.Add(new NpgsqlParameter<int>("p2", _foundPaid[orderIndex].Id));
            cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _foundPaid[orderIndex].OrderOrgId));
            using (cmd)
            {
                cmd.ExecuteNonQuery();
            }

        }
    }

    private void SetSource(DateTime start, DateTime end, ObservableTransaction? scope)
    {
        _foundPaid = Order.FindWithinRangeSortedByTime(start, end, SQL.OrderByCondition.OrderByTypes.ASC, scope)
        .Where(ord => ord is AdditionalContingentOrder).Select(o => (AdditionalContingentOrder)o).ToList();
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
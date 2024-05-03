using Npgsql;
using Npgsql.Replication.PgOutput.Messages;
using Utilities;

namespace Contingent.Models.Domain.Orders.Infrastructure;

// 26 марта 2023
// 1. приказы К и ДК нумеруются разными последовательностями
// 2. номер приказа можно обновлять, если появился новый приказ старше этого
// 3. ввести дополнительную метку времени, для определения хронологичности приказов
// 4. подсказка при вводе даты приказа (сверху - последняя) 
public abstract class OrderSequentialGuardian
{

    // список отсортирован по возрастанию даты 
    protected DateTime _yearStart;
    protected DateTime _yearEnd;
    protected abstract int YearWithin { get; set; }
    protected OrderSequentialGuardian()
    {

    }
    // получает индeкс для данного приказа
    public abstract int GetSequentialOrderNumber(Order? toInsert);
    // метод обновляет индекс всех приказов
    public abstract void Insert(Order toInsert);
    // мтеод обновляет индекс приказа
    public abstract void Save();
}


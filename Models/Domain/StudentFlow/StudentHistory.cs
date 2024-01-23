using System.Collections;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.SQL;
using Utilities;

namespace StudentTracking.Models.Domain.Flow;


public class StudentHistory : IEnumerable<StudentFlowRecord>
{
    private List<StudentFlowRecord> _history;
    private int _studentId;
    private bool _populated;
    public IEnumerator GetEnumerator()
    {
        return _history.GetEnumerator();
    }
    IEnumerator<StudentFlowRecord> IEnumerable<StudentFlowRecord>.GetEnumerator()
    {
        return _history.GetEnumerator();
    }

    public StudentHistory(int studentId)
    {
        _studentId = studentId;
        _history = new List<StudentFlowRecord>();
        _populated = false;
    }

    public StudentFlowRecord? GetClosestBefore(DateTime anchor)
    {
        TimeSpan minDiff = TimeSpan.MaxValue;
        int indexOfClosest = -1;
        for (int i = 0; i < _history.Count; i++)
        {
            var effDate = _history[i].OrderEffectiveDate;
            if (effDate <= anchor)
            {
                var diff = anchor - effDate;
                if (diff < minDiff)
                {
                    indexOfClosest = i;
                }
            }
        }
        if (indexOfClosest == -1)
        {
            return null;
        }
        else
        {
            return _history[indexOfClosest];
        }
    }
    public StudentFlowRecord? GetClosestAfter(DateTime anchor)
    {
        TimeSpan minDiff = TimeSpan.MaxValue;
        int indexOfClosest = -1;
        for (int i = 0; i < _history.Count; i++)
        {
            var effDate = _history[i].OrderEffectiveDate;
            if (effDate >= anchor)
            {
                var diff = anchor - effDate;
                if (diff < minDiff)
                {
                    indexOfClosest = i;
                }
            }
        }
        if (indexOfClosest == -1)
        {
            return null;
        }
        else
        {
            return _history[indexOfClosest];
        }
    }

    public StudentFlowRecord? GetLastOrder()
    {
        if (!_populated)
        {
            return null;
        }
        else
        {
            StudentFlowRecord last = new StudentFlowRecord();
            DateTime max = DateTime.MinValue;
            for (int i = 0; i < _history.Count; i++)
            {
                if (_history[i].OrderEffectiveDate > max)
                {
                    last = _history[i];
                    max = last.OrderEffectiveDate;
                }
            }
            return last;
        }
    }

    // получает всю историю студента в приказах
    // история отсортирована по дате регистрации приказа
    public async Task PopulateHistory()
    {
        _history.Clear();
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT student_flow.id AS sid, student_id, order_id, group_id_to, orders.effective_date AS oed, orders.type AS ot " +
        " FROM student_flow" +
        " JOIN orders ON orders.id = student_flow.order_id " +
        " WHERE student_id = @p1 " +
        " ORDER BY oed ASC";
        NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);
        await using (conn)
        await using (cmd)
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return;
            }
            while (reader.Read())
            {
                _history.Add(
                new StudentFlowRecord(
                    (int)reader["sid"],
                    (int)reader["order_id"],
                    (int)reader["student_id"],
                    reader["group_id_to"].GetType() == typeof(DBNull) ? null : (int)reader["group_id_to"],
                    (DateTime)reader["oed"],
                    (OrderTypes)(int)reader["ot"]
                ));
            }
        }
    }




    public static async Task<GroupModel?> GetCurrentStudentGroup(int studentId)
    {   
        var joins = new JoinSection()
        .AppendJoin(
            JoinSection.JoinType.RightJoin,
            new Column("id", "educational_group"),
            new Column("group_id_to", "student_flow")
            
        )
        .AppendJoin(JoinSection.JoinType.InnerJoin,
            new Column("order_id", "student_flow"),
            new Column("id" , "orders")
        );
        
        var parameters = new SQLParameterCollection();
        var p1 = parameters.Add(studentId);
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("student_id", "student_flow"),
                p1,
                WhereCondition.Relations.Equal
            )
        );
        var orderBy = new OrderByCondition(new Column("effective_date", "orders"), OrderByCondition.OrderByTypes.DESC);
        var found = await GroupModel.FindGroups(new QueryLimits(0,1), 
            additionalJoins: joins, 
            additionalConditions: where, 
            additionalOrderBy: orderBy, 
            addtitionalParameters: parameters);
        if (found.Count == 0){
            return null;
        }
        return found.First();
    }
}

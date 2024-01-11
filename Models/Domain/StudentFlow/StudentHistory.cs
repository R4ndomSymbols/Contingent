using System.Collections;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.JSON.Responses;
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




    public static async Task<GroupViewJSONResponse?> GetCurrentStudentGroup(int studentId)
    {
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT educational_group.id AS gid, group_name, name_generated FROM educational_group " +
        "RIGHT JOIN student_flow ON student_flow.group_id_to = educational_group.id " +
        "JOIN orders ON student_flow.order_id = orders.id " +
        "WHERE student_flow.student_id = @p1 " +
        "ORDER BY effective_date DESC " +
        "LIMIT 1";
        NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", studentId));
        await using (conn)
        await using (cmd)
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return null;
            }
            reader.Read();

            // изменить
            return null;
        }
    }
}

namespace StudentTracking.Models.Services;

using Npgsql;
using StudentTracking.Models;
using Utilities;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.Domain;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.JSON;
using StudentTracking.Models.JSON.Responses;
using StudentTracking.Models.Domain.Orders.OrderData;
using Microsoft.AspNetCore.Authentication;

public class OrderConsistencyMaintain
{

    public static async Task<int> GetNextOrderNumber(DateTime orderSpecifiedDate)
    {

        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        DateTime lowest = new DateTime(orderSpecifiedDate.Year, 1, 1);
        DateTime highest = new DateTime(orderSpecifiedDate.Year, 12, 31);

        string cmdText = "SELECT MAX(serial_number) AS current_max FROM public.orders WHERE " +
        "specified_date >= @p1 AND specified_date <= @p2";
        NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p1", lowest));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p2", highest));
        await using (conn)
        await using (cmd)
        {

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return 1;
            }
            await reader.ReadAsync();
            if (reader["current_max"].GetType() == typeof(DBNull))
            {
                return 1;
            }
            else
            {
                var next = (int)reader["current_max"];
                next++;
                return next;
            }
        }
    }

    public static async Task<StudentHistory?> GetHistoryByStudent(int studentId, ObservableTransaction? scope)
    {
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT student_flow.id AS sid, student_id, order_id, group_id_to, orders.effective_date AS oed, orders.type AS ot " +
        " FROM student_flow" +
        " JOIN orders ON orders.id = student_flow.order_id " +
        " WHERE student_id = @p1";
        NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);
        await using (conn)
        await using (cmd)
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return null;
            }
            var toReturn = new StudentHistory();
            while (await reader.ReadAsync())
            {
                toReturn.Add(
                    new StudentFlowRecord(
                        (int)reader["sid"],
                        (int)reader["order_id"],
                        (int)reader["student_id"],
                        (reader["group_id_to"].GetType() == typeof(DBNull) ? null : (int)reader["group_id_to"]),
                        (DateTime)reader["oed"],
                        (OrderTypes)(int)reader["ot"]

                ));
            }
            return toReturn;
        }
    }
    public static async Task InsertMany(IEnumerable<StudentFlowRecord> records)
    {
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "COPY student_flow (student_id, order_id, group_id_to)" +
        " FROM STDIN (FORMAT BINARY) ";
        await using (var writer = await conn.BeginBinaryImportAsync(cmdText))
        {
            foreach (var r in records)
            {
                writer.StartRow();
                writer.Write<int>(r.StudentId);
                writer.Write<int>(r.OrderId);
                if (r.GroupToId != null)
                {
                    writer.Write<int>((int)r.GroupToId);
                }
                else
                {
                    writer.Write(DBNull.Value, NpgsqlTypes.NpgsqlDbType.Integer);
                }
            }
            await writer.CompleteAsync();
        }
    }

    public static async Task<Order?> GetByIdTypeIndependent(int id)
    {
        var r = await FreeEnrollmentOrder.GetById(id, null);
        if (r != null)
        {
            return r;
        }
        r = await TransferGroupToGroupOrder.GetById(id, null);
        if (r != null)
        {
            return r;
        }
        r = await DeductionWithGraduationOrder.GetById(id, null);
        return r;
    }
    
    public static async Task<List<StudentViewJSONResponse>?> GetStudentsByOrder(int id)
    {
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT student_id FROM student_flow WHERE order_id = @p1";
        NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        await using (conn)
        await using (cmd)
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return null;
            }
            var toReturn = new List<StudentViewJSONResponse>();
            while (reader.Read())
            {
                var student = await StudentModel.GetStudentById((int)reader["student_id"]);
                if (student == null){
                    throw new ArgumentException("Id студента не может быть null");
                }
                string name = await student.GetName();
                GroupViewJSONResponse? group = await StudentHistory.GetCurrentStudentGroup(student.Id);
                
               
            }
            return toReturn;
        }
    } 
}




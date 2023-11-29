using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using StudentTracking.Models;
using Npgsql;
using System.Data;
using System.Text.Json.Serialization;
using Utilities;

public class StudentRecord
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }
    [JsonPropertyName("studentId")]
    public int StudentId { get; set; }
    [JsonPropertyName("groupFromId")]
    public int? GroupFromId { get; set; }
    [JsonPropertyName("groupToId")]
    public int? GroupToId { get; set; }
    [JsonPropertyName("previousRecord")]
    public int? PreviousRecordId { get; set; }
    [JsonPropertyName("nextRecord")]
    public int? NextRecordId { get; set; }

    public StudentRecord()
    {
        Id = Utils.INVALID_ID;
        OrderId = Utils.INVALID_ID;
        StudentId = Utils.INVALID_ID;
        GroupFromId = null;
        GroupToId = null;
        PreviousRecordId = null;
        NextRecordId = null;
    } 



    public static async Task<List<StudentEssentials>?> FilterByNextOrderType(OrderTypesId id, int currentOrderId, string? searchText = null, string? groupNameLike = null)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            int[]? ordersAllowed = OrderModel.GetRulesBefore(id);
            if (ordersAllowed != null)
            {

                string query = "SELECT * FROM get_student_allowed_for_order(@p1, @p2, @p3, @p4)";
                if (id == OrderTypesId.FromPaidToFreeGroup)
                {
                    query += " WHERE group_type = 2"; 
                }
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@p1", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer).Value = ordersAllowed;
                    cmd.Parameters.Add("@p2", NpgsqlTypes.NpgsqlDbType.Text).Value = (searchText == null || searchText == "") ? DBNull.Value : "%" + searchText + "%";
                    cmd.Parameters.Add("@p3", NpgsqlTypes.NpgsqlDbType.Text).Value = (groupNameLike == null || groupNameLike == "") ? DBNull.Value : "%" + groupNameLike + "%";
                    cmd.Parameters.Add("@p4", NpgsqlTypes.NpgsqlDbType.Integer).Value = currentOrderId;
                    using var reader = cmd.ExecuteReader();
                    var studentsGot = new List<StudentEssentials>();
                    if (!reader.HasRows)
                    {
                        return null;
                    }
                    while (reader.Read())
                    {
                        var essential = new StudentEssentials();
                        essential.Id = (int)reader["id"];
                        essential.FullName = (string)reader["surname"] + " " + (string)reader["name"] + " " + (string)reader["patronymic"];
                        if (reader["group_id"].GetType() != typeof(System.DBNull))
                        {
                            essential.GroupId = (int)reader["group_id"];
                            essential.GroupName = (string)reader["group_name"];
                        }
                        studentsGot.Add(essential);
                    }
                    return studentsGot;

                }

            }
            else
            {
                return null;
            }
        }
    }
    public static async Task<List<StudentEssentials>?> GetAssociatedStudents(int orderId)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {

            await using (var cmd = new NpgsqlCommand("SELECT * FROM get_students_in_order (@p1)", conn)
            {
                Parameters = {
                   new  ("p1", orderId)
                }
            })
            {
                using  var reader = cmd.ExecuteReader();
                var studentsGot = new List<StudentEssentials>();
                if (!reader.HasRows)
                {
                    return null;
                }
                while (reader.Read())
                {
                    var essential = new StudentEssentials();
                    essential.Id = (int)reader["id"];
                    essential.FullName = (string)reader["surname"] + " " + (string)reader["name"] +" "+ (string)reader["patronymic"];
                    if (reader["group_id"].GetType() != typeof(System.DBNull))
                    {
                        essential.GroupId = (int)reader["group_id"];
                        essential.GroupName = (string)reader["group_name"];
                    }
                    studentsGot.Add(essential);
                }
                return studentsGot;
            }
        }
    }
    // возвращает число записанных студентов
    public static async Task SaveRecords(List<StudentRecord> toSave){
        using (var conn = await Utils.GetAndOpenConnectionFactory()){
            for (int i = 0; i < toSave.Count; i++){
                // перед установкой в базу необходимо проверить валидность типа приказа
                // доделать
                
                string query = "INSERT INTO student_flow (\"order\", student, group_from, group_to, previous_record, next_record) " + 
                " VALUES (@p1, @p2, @p3, @p4, NULL, NULL)";
                var record = toSave[i];
                using( var cmd = new NpgsqlCommand(query, conn) {
                    Parameters = {
                        new ("p1", record.OrderId),
                        new ("p2", record.StudentId),
                        new ("p3", (record.GroupFromId == null || record.GroupFromId == Utils.INVALID_ID) ? DBNull.Value : record.GroupFromId),
                        new ("p4", (record.GroupToId == null || record.GroupToId == Utils.INVALID_ID) ? DBNull.Value : record.GroupToId),
                    }
                }){
                    var reader = cmd.ExecuteNonQuery();
                }
            }
        }
    } 

}




public class StudentEssentials
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("groupId")]
    public int GroupId { get; set; }
    [JsonPropertyName("name")]
    public string FullName { get; set; }
    [JsonPropertyName("groupName")]
    public string GroupName { get; set; }

    public StudentEssentials()
    {
        Id = -1;
        GroupId = -1;
        GroupName = "Нет";
        FullName = "";
    }
}
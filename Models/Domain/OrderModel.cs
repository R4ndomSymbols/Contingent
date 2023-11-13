using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using Npgsql;
using Utilities;

namespace StudentTracking.Models;

public class OrderModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("effectiveDate")]
    public DateTime EffectiveDate { get; set; }
    [JsonPropertyName("orderName")]
    public string OrderInOrganisationId { get; set; }
    [JsonPropertyName("orderDescription")]
    public string OrderDescription { get; set; }
    [JsonPropertyName("serialNumber")]
    public int SerialNumber { get; set; }
    [JsonPropertyName("orderType")]
    public int OrderTypeId { get; set; }

    public OrderModel()
    {
        Id = -1;
        EffectiveDate = DateTime.Now;
        OrderInOrganisationId = "";
        OrderDescription = "";
        SerialNumber = -1;
        OrderTypeId = -1;
    }

    private static readonly Dictionary<OrderTypesId, bool> _groupToFlags = new Dictionary<OrderTypesId, bool>(){
        {OrderTypesId.NoOrder, false},
        {OrderTypesId.Enrollment, true},
        {OrderTypesId.DeductionWithOwnDesire, false},
        {OrderTypesId.DeductionWithPoorProgress, false},
        {OrderTypesId.DeductionWithGraduation, false},
        {OrderTypesId.TransferGroupToGroup, true},
        {OrderTypesId.TransferOrgToGroup, true},
        {OrderTypesId.AcademicVacationSend, false},
        {OrderTypesId.AcademicVacationReturn, false},
        {OrderTypesId.ReenrollmentAfterDeduction, true},
        {OrderTypesId.FromPaidToFreeGroup, true},
    };
    
    private static readonly int[][] _rulesAfter = {
        //0 NoOrder - приказы, доступные для студентов, не отмеченных в базе
        new int []{
            (int)OrderTypesId.Enrollment,
            (int)OrderTypesId.TransferOrgToGroup,
        },
        //1 Enrollment - приказы после зачисления
        new int []{
            (int)OrderTypesId.DeductionWithOwnDesire,
            (int)OrderTypesId.DeductionWithPoorProgress,
            (int)OrderTypesId.DeductionWithGraduation,
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.AcademicVacationSend,
            (int)OrderTypesId.FromPaidToFreeGroup,
        },
        //2 DeductionWithOwnDesire - приказы после отчисления по собственному желанию
        new int []{
            (int)OrderTypesId.ReenrollmentAfterDeduction,
        },
        //3 DeductionWithPoorProgress - приказы после отчисления из-за неуспеваемости
        new int []{
            (int)OrderTypesId.ReenrollmentAfterDeduction,
        },
        //4 DeductionWithGraduation - приказы после отчисления в связи с выпуском
        new int []{
            (int)OrderTypesId.Enrollment,
        },
        //5 TransferGroupToGroup - перевод из группы в группу
        new int []{
            (int)OrderTypesId.DeductionWithOwnDesire,
            (int)OrderTypesId.DeductionWithPoorProgress,
            (int)OrderTypesId.DeductionWithGraduation,
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.AcademicVacationSend,
            (int)OrderTypesId.FromPaidToFreeGroup,
        },
        //6 TransferOrgToGroup
        new int []{
            (int)OrderTypesId.DeductionWithOwnDesire,
            (int)OrderTypesId.DeductionWithPoorProgress,
            (int)OrderTypesId.DeductionWithGraduation,
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.AcademicVacationSend,
            (int)OrderTypesId.FromPaidToFreeGroup,
        },
        // 7 AcademicVacationSend - приказы после академ отпуска
        new int []{
            (int)OrderTypesId.AcademicVacationReturn,
        },
        // 8 AcademicVacationReturn - восстановление после академ отпуска
        new int []{
            (int)OrderTypesId.DeductionWithOwnDesire,
            (int)OrderTypesId.DeductionWithPoorProgress,
            (int)OrderTypesId.DeductionWithGraduation,
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.AcademicVacationSend,
            (int)OrderTypesId.FromPaidToFreeGroup,
        },
        // 9 ReenrollmentAfterDeduction - восстановление после отчисления
        new int []{
            (int)OrderTypesId.DeductionWithOwnDesire,
            (int)OrderTypesId.DeductionWithPoorProgress,
            (int)OrderTypesId.DeductionWithGraduation,
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.AcademicVacationSend,
            (int)OrderTypesId.FromPaidToFreeGroup,
        },
        // 10 - FromPaidToFreeGroup - перевод с платного на бесплатное
        new int []{
            (int)OrderTypesId.DeductionWithOwnDesire,
            (int)OrderTypesId.DeductionWithPoorProgress,
            (int)OrderTypesId.DeductionWithGraduation,
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.AcademicVacationSend,
        },

    };
    private static readonly int[]?[] _rulesBefore = {
        //0 NoOrder - этот приказ не имеет предшественников
        null,
        //1 Enrollment - приказы, которые могут предшествовать зачислению
        new int []{
            (int)OrderTypesId.NoOrder,
            (int)OrderTypesId.DeductionWithGraduation,
        },
        //2 DeductionWithOwnDesire - приказы до отчисления по собственному желанию
        new int []{
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.AcademicVacationReturn,
            (int)OrderTypesId.FromPaidToFreeGroup,
            (int)OrderTypesId.Enrollment,
            (int)OrderTypesId.TransferOrgToGroup,
            (int)OrderTypesId.ReenrollmentAfterDeduction,
        },
        //3 DeductionWithPoorProgress - приказы до отчисления из-за неуспеваемости
        new int []{
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.AcademicVacationReturn,
            (int)OrderTypesId.FromPaidToFreeGroup,
            (int)OrderTypesId.Enrollment,
            (int)OrderTypesId.TransferOrgToGroup,
            (int)OrderTypesId.ReenrollmentAfterDeduction,
        },
        //4 DeductionWithGraduation - приказы до отчисления в связи с выпуском
        new int []{
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.AcademicVacationReturn,
            (int)OrderTypesId.FromPaidToFreeGroup,
            (int)OrderTypesId.Enrollment,
            (int)OrderTypesId.TransferOrgToGroup,
            (int)OrderTypesId.ReenrollmentAfterDeduction,
        },
        //5 TransferGroupToGroup - перевод из группы в группу
        new int []{
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.FromPaidToFreeGroup,
            (int)OrderTypesId.AcademicVacationReturn,
            (int)OrderTypesId.Enrollment,
            (int)OrderTypesId.TransferOrgToGroup,
            (int)OrderTypesId.ReenrollmentAfterDeduction,
        },
        //6 TransferOrgToGroup
        new int []{
            (int)OrderTypesId.DeductionWithOwnDesire,
            (int)OrderTypesId.DeductionWithPoorProgress,
            (int)OrderTypesId.DeductionWithGraduation,
            (int)OrderTypesId.NoOrder,
        },
        // 7 AcademicVacationSend - приказы до академ отпуска
        new int []{
            (int)OrderTypesId.AcademicVacationReturn,
            (int)OrderTypesId.Enrollment,
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.TransferOrgToGroup,
            (int)OrderTypesId.ReenrollmentAfterDeduction,
            (int)OrderTypesId.FromPaidToFreeGroup,

        },
        // 8 AcademicVacationReturn - приказы до восстановления после академ отпуска
        new int []{
            (int)OrderTypesId.AcademicVacationSend,
        },
        // 9 ReenrollmentAfterDeduction - восстановление после отчисления
        new int []{
            (int)OrderTypesId.DeductionWithOwnDesire,
            (int)OrderTypesId.DeductionWithPoorProgress,
            (int)OrderTypesId.DeductionWithGraduation,
        },
        // 10 - FromPaidToFreeGroup - перевод с платного на бесплатное
        new int []{
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.AcademicVacationReturn,
            (int)OrderTypesId.Enrollment,
            (int)OrderTypesId.TransferGroupToGroup,
            (int)OrderTypesId.TransferOrgToGroup,
            (int)OrderTypesId.ReenrollmentAfterDeduction,
        },

    };
    public static int[] GetRulesAfter(OrderTypesId order){
        return _rulesAfter[(int)order];
    }
    public static int[]? GetRulesBefore(OrderTypesId order){
        return _rulesBefore[(int)order];
    }
    public static bool GetGroupFlag(OrderTypesId id){
        return _groupToFlags[id];
    }
    

    public static OrderModel? GetById(int id)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT * FROM orders WHERE id = @p1", conn)
            {
                Parameters = {
                    new NpgsqlParameter("p1", id)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                reader.Read();
                return new OrderModel()
                {
                    Id = id,
                    EffectiveDate = (DateTime)reader["date"],
                    OrderInOrganisationId = (string)reader["order_identity"],
                    SerialNumber = (int)reader["serial_number"],
                    OrderTypeId = (int)reader["order_type"],
                    OrderDescription = (string)reader["order_description"]
                };
            }
        }
    }
    public static List<OrderType>? GetAllTypes()
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT * FROM order_types", conn))
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                var toReturn = new List<OrderType>();
                while (reader.Read())
                {
                    var orderT = new OrderType();
                    orderT.Id = (int)reader["id"];
                    orderT.Name = (string)reader["type_name"];
                    orderT.Postfix = (string)reader["order_name_postfix"];
                    orderT.GroupFlag = GetGroupFlag((OrderTypesId)orderT.Id);
                    toReturn.Add(orderT);
                }
                return toReturn;
            }
        }
    }



    public static int CreateOrUpdateOrder(OrderModel toProcess)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            if (toProcess.Id == -1)
            {
                using (var cmd = new NpgsqlCommand("INSERT INTO orders (date, order_identity, order_description, order_type, serial_number) "
                + " VALUES (@p1,@p2,@p3,@p4,@p5) RETURNING id", conn)
                {
                    Parameters = {
                        new ("p1", toProcess.EffectiveDate),
                        new ("p2", toProcess.OrderInOrganisationId),
                        new ("p3", toProcess.OrderDescription),
                        new ("p4", toProcess.OrderTypeId),
                        new ("p5", toProcess.SerialNumber),
                    }
                })
                {
                    var reader = cmd.ExecuteReader();
                    if (!reader.HasRows)
                    {
                        return -1;
                    }
                    reader.Read();
                    return (int)reader["id"];
                }
            }
            else
            {
                string commandText = "UPDATE orders SET date = @p1, order_identity = @p2, "+
                "order_description = @p3, serial_number = @p4 WHERE id = @p5";
                using (var cmd = new NpgsqlCommand(commandText, conn)
                {
                    Parameters = {
                        new ("p1", toProcess.EffectiveDate),
                        new ("p2", toProcess.OrderInOrganisationId),
                        new ("p3", toProcess.OrderDescription),
                        new ("p4", toProcess.SerialNumber),
                        new ("p5", toProcess.Id),

                    }
                })
                {
                    var reader = cmd.ExecuteNonQuery();
                    return toProcess.Id;
                }
            }
        }
    }

    public static List<OrderModel>? FindOrdersByOrgId(string orgId){
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT * FROM orders WHERE order_identity LIKE @p1", conn)
            {
                Parameters = {
                    new ("p1", "%" + orgId + "%") 
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                var toReturn = new List<OrderModel>();
                while (reader.Read())
                {
                    toReturn.Add(new OrderModel()
                    {
                        Id = (int)reader["id"],
                        OrderInOrganisationId = (string)reader["order_identity"],
                        OrderDescription = (string)reader["order_description"],
                        OrderTypeId = (int)reader["order_type"]
                    });
                }
                return toReturn;
            }
        }

    }
}

public enum OrderTypesId
{
    NoOrder = 0,
    // зачисление
    Enrollment = 1,
    // отчисление по собственному желанию
    DeductionWithOwnDesire = 2,
    // отчисление из-за неуспеваемости
    DeductionWithPoorProgress = 3,
    // отчисление в связи с выпуском 
    DeductionWithGraduation = 4,
    // перевод из группы в группу
    TransferGroupToGroup = 5,
    // перевод из другой организации в группу
    TransferOrgToGroup = 6,
    // академический отпуск
    AcademicVacationSend = 7,
    // восстановление из отпуска
    AcademicVacationReturn = 8,
    // восстановление после отчисления
    ReenrollmentAfterDeduction = 9,
    // перевод с платного на бесплатное
    FromPaidToFreeGroup = 10,
}

public class OrderType
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("postfix")]
    public string Postfix { get; set; }
    [JsonPropertyName("groupFlag")]
    public bool GroupFlag { get; set; }

    public OrderType()
    {
        Id = Utils.INVALID_ID;
        Name = "";
        Postfix = "";
        GroupFlag = false;
    }
}

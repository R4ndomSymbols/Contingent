using Npgsql;
using Utilities;
using Utilities.Validation;
using StudentTracking.Models.SQL;
using StudentTracking.Controllers.DTO;
using StudentTracking.Controllers.DTO.Out;
using System.Text.Json;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using System.Data.SqlTypes;

namespace StudentTracking.Models.Domain.Orders;

public abstract class Order
{
    protected int _id;
    protected DateTime _specifiedDate;
    protected DateTime _effectiveDate;
    // порядковый номер приказа в году и по типу
    protected int _orderNumber;
    protected string? _orderDescription;
    protected string _orderDisplayedName;
    protected bool _alreadyConducted;

    public int Id
    {
        get => _id;
    }
    // дата вступления в силу
    public DateTime EffectiveDate
    {
        get => _effectiveDate;
        private set => _effectiveDate = value;
    }
    public DateTime SpecifiedDate
    {
        get => _specifiedDate;
        private set => _specifiedDate = value;
    }
    public int OrderNumber
    {
        get => _orderNumber;
        private set {
            _orderNumber = value;
        }
    }
    // индентификатор приказа в организации
    public abstract string OrderOrgId {get;}
    // описание приказа
    public string OrderDescription
    {
        get => _orderDescription ?? "";
        private set => _orderDescription = value;
    }
    // отображаемое имя приказа
    public string OrderDisplayedName
    {
        get => _orderDisplayedName;
        private set => _orderDisplayedName = value;
    }

    protected Order()
    {
        
    }

    protected Result<Order?> MapBase(OrderDTO? source){

        // добавить проверку на диапазон дат дату, но потом
        var errors = new List<ValidationError?>();
        var nullErr = (source is not null).CheckRuleViolation("DTO приказа не может быть null"); 
        if (nullErr is not null){
            return Result<Order?>.Failure(nullErr);
        }

        if (errors.IsValidRule(Utils.TryParseDate(source.EffectiveDate), 
        message: "Дата вступления в силу указана неверно",
        propName: nameof(EffectiveDate))){
            _effectiveDate = Utils.ParseDate(source.EffectiveDate);
        }
        if (errors.IsValidRule(
            Utils.TryParseDate(source.SpecifiedDate),
            message: "Дата приказа указана неверно",
            propName: nameof(SpecifiedDate)
        )){
            _specifiedDate = Utils.ParseDate(source.SpecifiedDate);
        }
        Console.WriteLine(source.OrderDescription);
        if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(source.OrderDescription, ValidatorCollection.OnlyText) || source.OrderDescription == null,
            message: "Описание приказа указано неверно",
            propName: nameof(OrderDescription))
        ){
            _orderDescription = source.OrderDescription;
        }
        if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(source.OrderDisplayedName, ValidatorCollection.OnlyText),
            message: "Описание приказа указано неверно",
            propName: nameof(OrderDisplayedName))
        ){
            _orderDisplayedName = source.OrderDisplayedName;
        }
        errors.IsValidRule(
            TryParseOrderType(source.OrderType),
            message: "Тип приказа указан неверно",
            propName: "orderType"
        );

        if (errors.Any()){
            return Result<Order?>.Failure(errors);
        }
        else {
            return Result<Order?>.Success(this);
        }
    }
    protected async Task<Result<Order?>> GetBase(int id){
        using var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT * FROM orders WHERE id = @p1";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        using (cmd){
            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows){
                return Result<Order?>.Failure(new ValidationError(nameof(_id), "Приказа с таким id не существует"));
            }
            reader.Read();
            OrderTypes typeGot = (OrderTypes)(int)reader["type"]; 
            if (GetOrderTypeDetails().Type != typeGot){
                return Result<Order?>.Failure(new ValidationError(nameof(GetOrderTypeDetails), "Тип приказа не совпадает с найденным"));
            }
            _id = id;
            _effectiveDate = (DateTime)reader["effective_date"];
            _specifiedDate = (DateTime)reader["specified_date"];
            var description = reader["description"];
            if (description.GetType() == typeof(DBNull)){
                _orderDescription = null;
            }
            else {
                _orderDescription = (string)reader["description"];
            }
            _orderDisplayedName = (string)reader["name"];
            _orderNumber = (int)reader["serial_number"];
            return Result<Order?>.Success(this);
        }
    }


    internal abstract Task<bool> CheckConductionPossibility(); 
    public OrderTypeInfo GetOrderTypeDetails(){
        return OrderTypeInfo.GetByType(GetOrderType());
    }
    public abstract Task ConductByOrder();
    public abstract Task Save(ObservableTransaction? scope);
    protected abstract OrderTypes GetOrderType();
    protected async Task RequestAndSetNumber()
    {
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        DateTime lowest = new DateTime(_specifiedDate.Year, 1, 1);
        DateTime highest = new DateTime(_specifiedDate.Year, 12, 31);

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
                _orderNumber = 1;
            }
            await reader.ReadAsync();
            if (reader["current_max"].GetType() == typeof(DBNull))
            {
                _orderNumber = 1;
            }
            else
            {
                var next = (int)reader["current_max"];
                next++;
                _orderNumber = next;
            }
        } 
    }
    public static async Task<IReadOnlyCollection<Order>?> FindOrders(ComplexWhereCondition? filter, JoinSection? additionalJoins, SQLParameterCollection? additionalParams, QueryLimits limits){
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var mapper = new Mapper<Order>(
            async (r) => { 
                return (await GetOrderById((int)r["ordid"])).ResultObject;
            },
            new List<Column>(){
                new Column("id", "ordid", "orders"),
            } 
        );
        var result = SelectQuery<Order>.Init("orders")
        .AddMapper(mapper)
        .AddJoins(additionalJoins)
        .AddWhereStatement(filter)
        .AddOrderByStatement(new OrderByCondition(new Column("specified_date", "orders"), OrderByCondition.OrderByTypes.DESC))
        .AddParameters(additionalParams)
        .Finish();
        if (!result.IsSuccess){
            throw new Exception("Запрос сгенерирован неверно");
        }
        var query = result.ResultObject;
        return await query.Execute(conn, limits);
    }

    public static async Task<bool> IsOrderExists(int id){
        var conn = await Utils.GetAndOpenConnectionFactory();
        string cmtText = "SELECT EXISTS(SELECT id FROM orders WHERE id = @p1)";
        NpgsqlCommand cmd = new NpgsqlCommand(cmtText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        using (conn)
        using (cmd) {
            using var reader = cmd.ExecuteReader();
            reader.Read();
            return (bool)reader["exists"];
        }
    }
    protected static async Task InsertMany(IEnumerable<StudentFlowRecord> records)
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

    // получение приказа по Id независимо от типа

    public static async Task<Result<Order?>> GetOrderById(int id){
        var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT type FROM orders WHERE id = @p1";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        Order? found = null; 
        using(cmd){
            using var reader = cmd.ExecuteReader();
            if (!reader.HasRows){
                return Result<Order?>.Failure(new ValidationError(nameof(id), "Приказа не существует"));
            }
            reader.Read();
            OrderTypes type = (OrderTypes)(int)reader["type"];
            // добавить еще классы
            switch(type)
            {
                case OrderTypes.FreeEnrollment:
                    found = (await FreeEnrollmentOrder.Create(id)).ResultObject;
                    break;
                case OrderTypes.FreeDeductionWithGraduation:
                    found = (await FreeDeductionWithGraduationOrder.Create(id)).ResultObject;
                    break;
                case OrderTypes.FreeTransferGroupToGroup:
                    found = (await FreeTransferGroupToGroupOrder.Create(id)).ResultObject;
                    break;
                
                default:
                    return Result<Order?>.Failure(new ValidationError(nameof(GetOrderById), "Неизвестная ошибка")); 
            }
        }
        return Result<Order?>.Success(found);
    }

    public static async Task<Result<Order?>> GetOrderForConduction(int id, string conductionDataDTO){
        var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT type FROM orders WHERE id = @p1";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        using (conn) 
        using(cmd){
            using var reader = cmd.ExecuteReader();
            if (!reader.HasRows){
                return Result<Order?>.Failure(new ValidationError(nameof(id), "Приказа не существует"));
            }
            OrderTypes type = (OrderTypes)(int)reader["type"];
            IReadOnlyCollection<ValidationError>? errors;
            Utilities.IResult result;
            // добавить еще классы
            switch(type)
            {
                case OrderTypes.FreeEnrollment:
                    var data1 = JsonSerializer.Deserialize<EnrollmentOrderFlowDTO>(conductionDataDTO);
                    result = await FreeEnrollmentOrder.Create(id, data1);
                    break;
                case OrderTypes.FreeDeductionWithGraduation:
                    var data2 = JsonSerializer.Deserialize<DeductionWithGraduationOrderFlowDTO>(conductionDataDTO);
                    result = await FreeDeductionWithGraduationOrder.Create(id, data2);
                    break;
                case OrderTypes.FreeTransferGroupToGroup:
                    var data3 = JsonSerializer.Deserialize<TransferGroupToGroupOrderFlowDTO>(conductionDataDTO);
                    result = await FreeTransferGroupToGroupOrder.Create(id, data3);
                    break;
                
                default:
                    return Result<Order?>.Failure(new ValidationError(nameof(GetOrderById), "Неизвестная ошибка")); 
            }
            if (result.IsSuccess){
                return Result<Order?>.Success((Order?)result.GetResultObject());    
            }
            else {
                return Result<Order?>.Failure(result.GetErrors());
            }
            
        }

        
    }
    public static async Task<Result<Order?>> Build(string orderJson){
        OrderDTO? mapped = null;
        try {
            mapped = JsonSerializer.Deserialize<OrderDTO>(orderJson);
        }
        catch (Exception ex){
            return Result<Order?>.Failure(new ValidationError(nameof(orderJson), ex.Message));
        }
        var err = (mapped is not null).CheckRuleViolation("Ошибка при маппинге JSON"); 
        if (err is not null){
            return Result<Order?>.Failure(err);
        }
        err = TryParseOrderType(mapped.OrderType).CheckRuleViolation("Такого типа приказа не существует"); 
        if (err is not null){
            return Result<Order?>.Failure(err);
        }
    
        Utilities.IResult result;
        OrderTypes type = (OrderTypes)mapped.OrderType;
        // добавить еще классы
        switch(type)
        {
            case OrderTypes.FreeEnrollment:
                result = await FreeEnrollmentOrder.Create(mapped);
                break;
            case OrderTypes.FreeDeductionWithGraduation:
                result = await FreeDeductionWithGraduationOrder.Create(mapped);
                break;
            case OrderTypes.FreeTransferGroupToGroup:
                result = await FreeTransferGroupToGroupOrder.Create(mapped);
                break;
            
            default:
                return Result<Order?>.Failure(new ValidationError(nameof(GetOrderById), "Неизвестная ошибка")); 
        }
        if (result.IsSuccess){
            return Result<Order?>.Success((Order?)result.GetResultObject());
        }
        else {
            return Result<Order?>.Failure(result.GetErrors());
        }
    }

    private static bool TryParseOrderType(int orderType){
        try{
            var type = (OrderTypes)orderType;
            return true;
        }
        catch (InvalidCastException){
            return false;
        }
    }

}

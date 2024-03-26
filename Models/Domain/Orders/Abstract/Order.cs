using Npgsql;
using Utilities;
using Utilities.Validation;
using StudentTracking.SQL;
using StudentTracking.Controllers.DTO.In;
using System.Text.Json;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using System.Net;
using StudentTracking.Models.Domain.Orders.Infrastructure;

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
    protected OrderConductionStatus _conductionStatus;
    protected bool _isClosed;
    // время создания приказа пользователем
    protected DateTime _creationTimestamp;

    public int Id
    {
        get => _id;
    }
    public bool IsClosed
    {
        get => _isClosed;
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
        internal set
        {
            _orderNumber = value;
        }
    }
    // индентификатор приказа в организации
    public abstract string OrderOrgId { get; }
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

    public DateTime OrderCreationDate => _creationTimestamp;

    protected abstract OrderSequentialGuardian SequentialGuardian { get; }

    public static Mapper<Order> GetMapper(Column? source, JoinSection.JoinType joinType = JoinSection.JoinType.InnerJoin)
    {
        var mapper = new Mapper<Order>(
            (reader) =>
            {
                var idraw = reader["ord_id"];
                if (idraw.GetType() == typeof(DBNull))
                {
                    return QueryResult<Order>.NotFound();
                }
                var id = (int)idraw;
                Utilities.IResult? result = null;
                OrderTypes typeGot = (OrderTypes)(int)reader["type"];
                switch (typeGot)
                {
                    case OrderTypes.FreeEnrollment:
                        result = FreeEnrollmentOrder.Create(id, reader);
                        break;
                    case OrderTypes.FreeReenrollment:
                        result = FreeReenrollmentOrder.Create(id, reader);
                        break;
                    case OrderTypes.FreeEnrollmentWithTransfer:
                        result = FreeEnrollmentWithTransferOrder.Create(id, reader);
                        break;

                }
                // приказы об отчилении
                switch (typeGot)
                {
                    case OrderTypes.FreeDeductionWithGraduation:
                        result = FreeDeductionWithGraduationOrder.Create(id, reader);
                        break;
                    case OrderTypes.FreeDeductionWithAcademicDebt:
                        result = FreeDeductionWithAcademicDebtOrder.Create(id, reader);
                        break;
                    case OrderTypes.FreeDeductionWithOwnDesire:
                        result = FreeDeductionWithOwnDesireOrder.Create(id, reader);
                        break;
                }
                // приказы о переводе
                switch (typeGot)
                {
                    case OrderTypes.FreeNextCourseTransfer:
                        result = FreeTransferToTheNextCourseOrder.Create(id, reader);
                        break;
                    case OrderTypes.FreeTransferBetweenSpecialities:
                        result = FreeTransferBetweenSpecialitiesOrder.Create(id, reader);
                        break;
                }
                if (result is null){
                    return QueryResult<Order>.NotFound();
                }
                return QueryResult<Order>.Found((Order)result.GetResultObject());
            },
            new Column[]{
                new Column("id", "ord_id", "orders"),
                new Column("is_closed", "orders"),
                new Column("effective_date", "orders"),
                new Column("specified_date", "orders"),
                new Column("description", "orders"),
                new Column("name", "orders"),
                new Column("type", "orders"),
                new Column("serial_number", "orders"),
                new Column("creation_timestamp", "orders")

            }
        );
        if (source is not null){
            mapper.PathTo.AddHead(joinType,source, new Column("id", "orders"));
        }
        return mapper;
    }
    protected Order()
    {
        _conductionStatus = OrderConductionStatus.ConductionNotAllowed;
    }
    protected Order(int id)
    {
        _id = id;
        _conductionStatus = OrderConductionStatus.ConductionNotValidated;
    }

    protected static async Task<Result<T?>> MapBase<T>(OrderDTO? source, T model) where T : Order
    {

        // добавить проверку на диапазон дат дату, но потом
        var errors = new List<ValidationError?>();
        if (source is null)
        {
            return Result<T>.Failure(new OrderValidationError("DTO приказа не может быть пустым"));
        }

        if (errors.IsValidRule(Utils.TryParseDate(source.EffectiveDate),
        message: "Дата вступления в силу указана неверно",
        propName: nameof(EffectiveDate)))
        {
            model._effectiveDate = Utils.ParseDate(source.EffectiveDate);
        }
        if (errors.IsValidRule(
            Utils.TryParseDate(source.SpecifiedDate),
            message: "Дата приказа указана неверно",
            propName: nameof(SpecifiedDate)
        ))
        {
            model._specifiedDate = Utils.ParseDate(source.SpecifiedDate);
        }
        Console.WriteLine(source.OrderDescription);
        if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(source.OrderDescription, ValidatorCollection.OnlyText) || source.OrderDescription == null,
            message: "Описание приказа указано неверно",
            propName: nameof(OrderDescription))
        )
        {
            model._orderDescription = source.OrderDescription;
        }
        if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(source.OrderDisplayedName, ValidatorCollection.OnlyText),
            message: "Имя приказа укзано неверно",
            propName: nameof(OrderDisplayedName))
        )
        {
            model._orderDisplayedName = source.OrderDisplayedName;
        }
        errors.IsValidRule(
            TryParseOrderType(source.OrderType),
            message: "Тип приказа указан неверно",
            propName: "orderType"
        );

        if (errors.Any())
        {
            return Result<T>.Failure(errors);
        }
        else
        {   model._creationTimestamp = DateTime.Now;
            model._orderNumber = model.SequentialGuardian.GetSequentialIndex(model);
            return Result<T>.Success(model);
        }
    }
    protected static QueryResult<T?> MapParticialFromDbBase<T>(NpgsqlDataReader reader, T model) where T : Order
    {
        OrderTypes typeGot = (OrderTypes)(int)reader["type"];
        if (model.GetOrderTypeDetails().Type != typeGot)
        {
            return QueryResult<T?>.NotFound();
        }
        model._isClosed = (bool)reader["is_closed"];
        model._effectiveDate = (DateTime)reader["effective_date"];
        model._specifiedDate = (DateTime)reader["specified_date"];
        var description = reader["description"];
        if (description.GetType() == typeof(DBNull))
        {
            model._orderDescription = null;
        }
        else
        {
            model._orderDescription = (string)reader["description"];
        }
        model._orderDisplayedName = (string)reader["name"];
        model._orderNumber = (int)reader["serial_number"];
        model._creationTimestamp = (DateTime)reader["creation_timestamp"];
        return QueryResult<T?>.Found(model);
        
    }

    protected static Result<T?> MapFromDbBaseForConduction<T>(T model) where T : Order
    {
        var got = GetOrderById(model._id).Result;
        if (got.IsFailure || got.ResultObject.IsClosed)
        {
            return Result<T>.Failure(new ValidationError(nameof(IsClosed), "Невозможно получить уже закрытый приказ для проведения, либо приказа не существует"));
        }
        return Result<T>.Success((T?)got.ResultObject);
    }

    // этот метод должен по умолчанию менять статус приказа, без его вызова
    // приказ не может получить статус готового к проведению
    internal abstract Task<ResultWithoutValue> CheckConductionPossibility();
    internal virtual async Task<ResultWithoutValue> CheckBaseConductionPossibility(IEnumerable<StudentModel> toCheck)
    {
        // эта проверка элиминирует необходимость проверки студента на прикрепленность к этому же самому приказу, 
        // для закрытых приказов проведение невозможно
        if (await StudentHistory.IsAnyStudentInNotClosedOrder(toCheck))
        {
            return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов зарегистрированы в незакрытом приказе"));
        }
        foreach(var s in toCheck){
            var record = s.History.GetLastRecord(); 
            if (record is not null && record.ByOrder._effectiveDate > this._effectiveDate){
                ResultWithoutValue.Failure(new OrderValidationError("Приказ не является хронологически последовательным для одного или нескольких студентов"));
            }
        }
        return ResultWithoutValue.Success();
    }

    public OrderTypeInfo GetOrderTypeDetails()
    {
        return OrderTypeInfo.GetByType(GetOrderType());
    }
    public abstract Task ConductByOrder();
    public abstract Task Save(ObservableTransaction? scope);
    protected abstract OrderTypes GetOrderType();
    public static async Task<IReadOnlyCollection<Order>> FindOrders(QueryLimits limits, ComplexWhereCondition? filter = null, JoinSection? additionalJoins = null, SQLParameterCollection? additionalParams = null, OrderByCondition? orderBy = null)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var mapper = GetMapper(null);
        var result = SelectQuery<Order>.Init("orders")
        .AddMapper(mapper)
        .AddJoins(additionalJoins)
        .AddWhereStatement(filter)
        .AddOrderByStatement(orderBy is null ? 
            new OrderByCondition(new Column("specified_date", "orders"), OrderByCondition.OrderByTypes.DESC)
            : orderBy)
        .AddParameters(additionalParams)
        .Finish();
        if (result.IsFailure)
        {
            throw new Exception("Запрос сгенерирован неверно");
        }
        var query = result.ResultObject;
        return await query.Execute(conn, limits);
    }

    public static async Task<bool> IsOrderExists(int id)
    {
        var conn = await Utils.GetAndOpenConnectionFactory();
        string cmtText = "SELECT EXISTS(SELECT id FROM orders WHERE id = @p1)";
        NpgsqlCommand cmd = new NpgsqlCommand(cmtText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        using (conn)
        using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            reader.Read();
            return (bool)reader["exists"];
        }
    }
    protected async Task ConductBase(IEnumerable<StudentFlowRecord> records)
    {
        if (records is null || !records.Any() || _conductionStatus != OrderConductionStatus.ConductionReady || _isClosed)
        {
            throw new Exception("Ошибка при записи в таблицу движения: данные приказа или приказ не соотвествуют форме или приказ закрыт");
        }

        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "COPY student_flow (student_id, order_id, group_id_to)" +
        " FROM STDIN (FORMAT BINARY) ";
        await using (var writer = await conn.BeginBinaryImportAsync(cmdText))
        {
            foreach (var r in records)
            {
                writer.StartRow();
                writer.Write<int>(r.Student.Id.Value);
                writer.Write<int>(r.ByOrder.Id);
                if (r.GroupTo != null)
                {
                    writer.Write<int>((int)r.GroupTo.Id);
                }
                else
                {
                    writer.Write(DBNull.Value, NpgsqlTypes.NpgsqlDbType.Integer);
                }
            }
            await writer.CompleteAsync();
        }

        _conductionStatus = OrderConductionStatus.Conducted;
    }

    protected async Task SaveBase(ObservableTransaction? scope = null)
    {
        NpgsqlConnection? conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "INSERT INTO public.orders( " +
        " specified_date, effective_date, serial_number, org_id, type, name, description, is_closed, creation_timestamp) " +
        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9) RETURNING id";
        NpgsqlCommand cmd;
        if (scope is null)
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }

        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p1", _specifiedDate));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p2", _effectiveDate));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p3", _orderNumber));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p4", OrderOrgId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)GetOrderTypeDetails().Type));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p6", _orderDisplayedName));
        if (_orderDescription is null)
        {
            cmd.Parameters.Add(new NpgsqlParameter<DBNull>("p7", DBNull.Value));
        }
        else
        {
            cmd.Parameters.Add(new NpgsqlParameter<string>("p7", _orderDescription));
        }
        cmd.Parameters.Add(new NpgsqlParameter<bool>("p8", _isClosed));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p9", _creationTimestamp));

        await using (conn)
        await using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            return;
        }
    }

    // получение приказа по Id независимо от типа

    public static async Task<Result<Order?>> GetOrderById(int id)
    {
        var paramCollection = new SQLParameterCollection();
        var p1 = paramCollection.Add(id); 
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("id", "orders"),
                p1,
                WhereCondition.Relations.Equal
            )
        );
        var found = await FindOrders(
            new QueryLimits(0,1), 
            filter: where,
            additionalParams: paramCollection
        );
        if (found.Any()){
             return Result<Order?>.Success(found.First());
        }
        return Result<Order?>.Failure(new OrderValidationError("Приказ не может быть получен из базы данных"));
    }

    public static async Task<Result<Order?>> GetOrderForConduction(int id, string conductionDataDTO)
    {
        var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT type FROM orders WHERE id = @p1";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        using (conn)
        using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                return Result<Order?>.Failure(new ValidationError(nameof(id), "Приказа не существует"));
            }
            reader.Read();
            OrderTypes type = (OrderTypes)(int)reader["type"];
            Utilities.IResult? result = null;

            // приказы о зачислении
            switch (type)
            {
                case OrderTypes.FreeEnrollment:
                    result = await FreeEnrollmentOrder.Create(id,
                    JsonSerializer.Deserialize<StudentGroupChangeMoveDTO>(conductionDataDTO));
                    break;
                case OrderTypes.FreeReenrollment:
                    result = await FreeReenrollmentOrder.Create(id,
                    JsonSerializer.Deserialize<StudentGroupChangeMoveDTO>(conductionDataDTO));
                    break;
                case OrderTypes.FreeEnrollmentWithTransfer:
                    result = await FreeEnrollmentWithTransferOrder.Create(id,
                    JsonSerializer.Deserialize<StudentGroupChangeMoveDTO>(conductionDataDTO));
                    break;

            }
            // приказы об отчилении
            switch (type)
            {
                case OrderTypes.FreeDeductionWithGraduation:
                    result = await FreeDeductionWithGraduationOrder.Create(id,
                    JsonSerializer.Deserialize<StudentGroupNullifyMoveDTO>(conductionDataDTO));
                    break;
                case OrderTypes.FreeDeductionWithAcademicDebt:
                    result = await FreeDeductionWithAcademicDebtOrder.Create(id,
                    JsonSerializer.Deserialize<StudentGroupNullifyMoveDTO>(conductionDataDTO));
                    break;
                case OrderTypes.FreeDeductionWithOwnDesire:
                    result = await FreeDeductionWithOwnDesireOrder.Create(id,
                    JsonSerializer.Deserialize<StudentGroupNullifyMoveDTO>(conductionDataDTO));
                    break;
            }
            // приказы о переводе
            switch (type)
            {
                case OrderTypes.FreeNextCourseTransfer:
                    result = await FreeTransferToTheNextCourseOrder.Create(id,
                    JsonSerializer.Deserialize<StudentGroupChangeMoveDTO>(conductionDataDTO));
                    break;
                case OrderTypes.FreeTransferBetweenSpecialities:
                    result = await FreeTransferBetweenSpecialitiesOrder.Create(id,
                    JsonSerializer.Deserialize<StudentGroupChangeMoveDTO>(conductionDataDTO));
                    break;
            }
            if (result is null)
            {
                return Result<Order?>.Failure(new OrderValidationError("Приказ для проведения не может быть получен из базы данных"));
            }
            if (result.IsFailure)
            {
                return Result<Order?>.Failure(result);
            }
            return Result<Order?>.Success((Order?)result.GetResultObject());
        }
    }
    public static async Task<Result<Order?>> Build(string orderJson)
    {
        OrderDTO? mapped = null;
        try
        {
            mapped = JsonSerializer.Deserialize<OrderDTO>(orderJson);
        }
        catch (Exception ex)
        {
            return Result<Order?>.Failure(new ValidationError(nameof(orderJson), ex.Message));
        }

        var err = (mapped is not null).CheckRuleViolation("Ошибка при маппинге JSON");

        if (mapped is null)
        {
            return Result<Order?>.Failure(new OrderValidationError("Ошибка при маппинге json"));
        }

        if (!TryParseOrderType(mapped.OrderType))
        {
            return Result<Order?>.Failure(new OrderValidationError("Неверно указан тип приказа"));
        }

        Utilities.IResult? result = null;
        OrderTypes type = (OrderTypes)mapped.OrderType;
        // приказы о зачислении
        switch (type)
        {
            case OrderTypes.FreeEnrollment:
                result = await FreeEnrollmentOrder.Create(mapped);
                break;
            case OrderTypes.FreeReenrollment:
                result = await FreeReenrollmentOrder.Create(mapped);
                break;
            case OrderTypes.FreeEnrollmentWithTransfer:
                result = await FreeEnrollmentWithTransferOrder.Create(mapped);
                break;

        }
        // приказы об отчилении
        switch (type)
        {
            case OrderTypes.FreeDeductionWithGraduation:
                result = await FreeDeductionWithGraduationOrder.Create(mapped);
                break;
            case OrderTypes.FreeDeductionWithAcademicDebt:
                result = await FreeDeductionWithAcademicDebtOrder.Create(mapped);
                break;
            case OrderTypes.FreeDeductionWithOwnDesire:
                result = await FreeDeductionWithOwnDesireOrder.Create(mapped);
                break;
        }
        // приказы о переводе
        switch (type)
        {
            case OrderTypes.FreeNextCourseTransfer:
                result = await FreeTransferToTheNextCourseOrder.Create(mapped);
                break;
            case OrderTypes.FreeTransferBetweenSpecialities:
                result = await FreeTransferBetweenSpecialitiesOrder.Create(mapped);
                break;
        }

        if (result is null)
        {
            return Result<Order?>.Failure(new OrderValidationError("Такой тип приказа не поддерживается"));
        }

        if (result.IsSuccess)
        {
            return Result<Order?>.Success((Order?)result.GetResultObject());
        }
        else
        {
            return Result<Order?>.Failure(result.GetErrors());
        }
    }

    private static bool TryParseOrderType(int orderType)
    {
        try
        {
            var type = (OrderTypes)orderType;
            return true;
        }
        catch (InvalidCastException)
        {
            return false;
        }
    }

    public async Task Close()
    {
        if (!_isClosed)
        {
            using var conn = await Utils.GetAndOpenConnectionFactory();
            var cmdText = "UPDATE orders SET is_closed = true WHERE id = @p1";
            var cmd = new NpgsqlCommand(cmdText, conn);
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", _id));
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            _isClosed = true;
        }
    }

    public static IReadOnlyCollection<Order> FindWithinRangeSortedByTime(DateTime start, DateTime end, OrderByCondition.OrderByTypes orderBy = OrderByCondition.OrderByTypes.ASC){
        if (end < start){
            throw new ArgumentException();
        }
        var parameters = new SQLParameterCollection();
        var startP = parameters.Add<DateTime>(start);
        var endP = parameters.Add<DateTime>(end);
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("specified_date", "orders"),
                startP, 
                WhereCondition.Relations.BiggerOrEqual),
            new WhereCondition(
                new Column("specified_date", "orders"),
                endP,
                WhereCondition.Relations.LessOrEqual
                ), ComplexWhereCondition.ConditionRelation.AND
            );
        var orderByClause = new OrderByCondition(new Column("specified_date", "orders"), orderBy);
        orderByClause.AddColumn(
            new Column("creation_timestamp", "orders"), orderBy
        );
        return FindOrders(new QueryLimits(0, 500), additionalParams: parameters, filter: where, orderBy: orderByClause).Result;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not Order)
        {
            return false;
        }
        return ((Order)obj)._id == this._id;
    }


}

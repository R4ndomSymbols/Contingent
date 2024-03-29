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
using StudentTracking.Models.Domain.Flow.History;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

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

    private static Dictionary<OrderTypes, Func<int, NpgsqlDataReader, Utilities.IResult>> _orderTypeMappings = new Dictionary<OrderTypes, Func<int, NpgsqlDataReader, Utilities.IResult>>{
        {
            OrderTypes.FreeEnrollment, FreeEnrollmentOrder.Create
        },
        {
            OrderTypes.FreeEnrollmentWithTransfer, FreeEnrollmentWithTransferOrder.Create
        },
        {
            OrderTypes.FreeReenrollment, FreeReenrollmentOrder.Create
        },
        {
            OrderTypes.FreeTransferNextCourse, FreeTransferToTheNextCourseOrder.Create
        },
        {
            OrderTypes.FreeTransferBetweenSpecialities, FreeTransferBetweenSpecialitiesOrder.Create
        },
        {
            OrderTypes.FreeDeductionWithAcademicDebt, FreeDeductionWithAcademicDebtOrder.Create
        },
        {
            OrderTypes.FreeDeductionWithGraduation, FreeDeductionWithGraduationOrder.Create
        },
        {
            OrderTypes.FreeDeductionWithOwnDesire, FreeDeductionWithOwnDesireOrder.Create
        },
        // платные
        {
            OrderTypes.PaidEnrollment, PaidEnrollmentOrder.Create
        },
        {
            OrderTypes.PaidEnrollmentWithTransfer, PaidEnrollmentWithTransferOrder.Create
        },
        {
            OrderTypes.PaidReenrollment, PaidReenrollmentOrder.Create
        },
        {
            OrderTypes.PaidTransferNextCourse, PaidTransferNextCourseOrder.Create
        },
        {
            OrderTypes.PaidTransferBetweenSpecialities, PaidTransferBetweenSpecialitiesOrder.Create
        },
        {
            OrderTypes.PaidTransferFromPaidToFree, PaidTransferFromPaidToFreeOrder.Create
        },
        {
            OrderTypes.PaidDeductionWithAcademicDebt, PaidDeductionWithAcademicDebtOrder.Create
        },
        {
            OrderTypes.PaidDeductionWithGraduation, PaidDeductionWithGraduationOrder.Create
        },
        {
            OrderTypes.PaidDeductionWithOwnDesire, PaidDeductionWithOwnDesireOrder.Create
        },
        {
            OrderTypes.PaidDeductionWithTransfer, PaidDeductionWithTransferOrder.Create
        }
    };

    private static Dictionary<OrderTypes, Func<int, string, Utilities.IResult>> _orderForConductionMappings = new Dictionary<OrderTypes, Func<int, string, Utilities.IResult>>{
        {
            OrderTypes.FreeEnrollment,
            (id, jsonString) => FreeEnrollmentOrder.Create(id, JsonSerializer.Deserialize<StudentGroupChangeMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.FreeEnrollmentWithTransfer,
            (id, jsonString) => FreeEnrollmentWithTransferOrder.Create(id, JsonSerializer.Deserialize<StudentGroupChangeMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.FreeReenrollment, 
            (id, jsonString) => FreeReenrollmentOrder.Create(id, JsonSerializer.Deserialize<StudentGroupChangeMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.FreeTransferNextCourse, 
            (id, jsonString) => FreeTransferToTheNextCourseOrder.Create(id, JsonSerializer.Deserialize<StudentGroupChangeMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.FreeTransferBetweenSpecialities,
            (id, jsonString) => FreeTransferBetweenSpecialitiesOrder.Create(id, JsonSerializer.Deserialize<StudentGroupChangeMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.FreeDeductionWithAcademicDebt,
            (id, jsonString) => FreeDeductionWithAcademicDebtOrder.Create(id,JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.FreeDeductionWithGraduation,
            (id, jsonString) => FreeDeductionWithGraduationOrder.Create(id,JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.FreeDeductionWithOwnDesire,
            (id, jsonString) => FreeDeductionWithOwnDesireOrder.Create(id,JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)).Result
        },
        // платные
        {
            OrderTypes.PaidEnrollment, 
            (id, jsonString) => PaidEnrollmentOrder.Create(id, JsonSerializer.Deserialize<StudentGroupChangeMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.PaidEnrollmentWithTransfer,
            (id, jsonString) => PaidEnrollmentWithTransferOrder.Create(id, JsonSerializer.Deserialize<StudentGroupChangeMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.PaidReenrollment,
            (id, jsonString) => PaidReenrollmentOrder.Create(id, JsonSerializer.Deserialize<StudentGroupChangeMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.PaidTransferNextCourse,
            (id, jsonString) => PaidTransferNextCourseOrder.Create(id, JsonSerializer.Deserialize<StudentGroupChangeMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.PaidTransferBetweenSpecialities, 
            (id, jsonString) => PaidTransferBetweenSpecialitiesOrder.Create(id, JsonSerializer.Deserialize<StudentGroupChangeMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.PaidTransferFromPaidToFree,
            (id, jsonString) => PaidTransferFromPaidToFreeOrder.Create(id, JsonSerializer.Deserialize<StudentGroupChangeMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.PaidDeductionWithAcademicDebt, 
            (id, jsonString) => PaidDeductionWithAcademicDebtOrder.Create(id,JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.PaidDeductionWithGraduation, 
            (id, jsonString) => PaidDeductionWithGraduationOrder.Create(id,JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.PaidDeductionWithOwnDesire, 
            (id, jsonString) => PaidDeductionWithOwnDesireOrder.Create(id,JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)).Result
        },
        {
            OrderTypes.PaidDeductionWithTransfer, 
            (id, jsonString) => PaidDeductionWithTransferOrder.Create(id,JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)).Result
        }
    };
    
    private static Dictionary<OrderTypes, Func<OrderDTO?, Utilities.IResult>> _orderFromDtoMappings = new Dictionary<OrderTypes, Func<OrderDTO?, Utilities.IResult>>{
        {
            OrderTypes.FreeEnrollment, FreeEnrollmentOrder.Create
        },
        {
            OrderTypes.FreeEnrollmentWithTransfer, FreeEnrollmentWithTransferOrder.Create
        },
        {
            OrderTypes.FreeReenrollment, FreeReenrollmentOrder.Create
        },
        {
            OrderTypes.FreeTransferNextCourse, FreeTransferToTheNextCourseOrder.Create
        },
        {
            OrderTypes.FreeTransferBetweenSpecialities, FreeTransferBetweenSpecialitiesOrder.Create
        },
        {
            OrderTypes.FreeDeductionWithAcademicDebt, FreeDeductionWithAcademicDebtOrder.Create
        },
        {
            OrderTypes.FreeDeductionWithGraduation, FreeDeductionWithGraduationOrder.Create
        },
        {
            OrderTypes.FreeDeductionWithOwnDesire, FreeDeductionWithOwnDesireOrder.Create
        },
        // платные
        {
            OrderTypes.PaidEnrollment, PaidEnrollmentOrder.Create
        },
        {
            OrderTypes.PaidEnrollmentWithTransfer, PaidEnrollmentWithTransferOrder.Create
        },
        {
            OrderTypes.PaidReenrollment, PaidReenrollmentOrder.Create
        },
        {
            OrderTypes.PaidTransferNextCourse, PaidTransferNextCourseOrder.Create
        },
        {
            OrderTypes.PaidTransferBetweenSpecialities, PaidTransferBetweenSpecialitiesOrder.Create
        },
        {
            OrderTypes.PaidTransferFromPaidToFree, PaidTransferFromPaidToFreeOrder.Create
        },
        {
            OrderTypes.PaidDeductionWithAcademicDebt, PaidDeductionWithAcademicDebtOrder.Create
        },
        {
            OrderTypes.PaidDeductionWithGraduation, PaidDeductionWithGraduationOrder.Create
        },
        {
            OrderTypes.PaidDeductionWithOwnDesire, PaidDeductionWithOwnDesireOrder.Create
        },
        {
            OrderTypes.PaidDeductionWithTransfer, PaidDeductionWithTransferOrder.Create
        }
    };
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
                OrderTypes typeGot = (OrderTypes)(int)reader["type"];
                Utilities.IResult? result  = _orderTypeMappings[typeGot](id, reader);
                if (result is null || result.IsFailure) 
                {
                    return QueryResult<Order>.NotFound();
                }
                var found = result.GetResultObject();
                if (found is null)
                {
                    return QueryResult<Order>.NotFound();
                }
                return QueryResult<Order>.Found((Order)found);
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
        if (source is not null)
        {
            mapper.PathTo.AddHead(joinType, source, new Column("id", "orders"));
        }
        return mapper;
    }
    protected Order()
    {
        _conductionStatus = OrderConductionStatus.ConductionNotAllowed;
        _orderDisplayedName = "Не указано";
    }
    protected Order(int id)
    {
        _id = id;
        _orderDisplayedName = "Не указано";
        _conductionStatus = OrderConductionStatus.ConductionNotValidated;
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
    protected static Result<T> MapBase<T>(OrderDTO? source, T model) where T : Order
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
        if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(source.OrderDescription, ValidatorCollection.OnlyText) || source.OrderDescription is null,
            message: "Описание приказа указано неверно",
            propName: nameof(OrderDescription))
        )
        {
            model._orderDescription = source.OrderDescription;
        }
        if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(source.OrderDisplayedName, ValidatorCollection.OnlyText),
            message: "Имя приказа укзано неверно",
            propName: nameof(OrderDisplayedName)) && source.OrderDisplayedName is not null
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
        {
            model._creationTimestamp = DateTime.Now;
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
    protected static Result<T> MapFromDbBaseForConduction<T>(int id) where T : Order
    {
        var got = GetOrderById(id);
        if (got is null || got.IsClosed)
        {
            return Result<T>.Failure(new ValidationError(nameof(IsClosed), "Невозможно получить уже закрытый приказ для проведения, либо приказа не существует"));
        }
        return Result<T>.Success((T)got);
    }
    protected async Task ConductBase(IEnumerable<StudentFlowRecord>? records)
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
                writer.Write<int>(r.StudentNullRestrict.Id.Value);
                writer.Write<int>(this._id);
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
    protected abstract OrderTypes GetOrderType();
    public virtual async Task Save(ObservableTransaction? scope = null)
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
    // этот метод должен по умолчанию менять статус приказа, без его вызова
    // приказ не может получить статус готового к проведению

    // проверяет только студентов на предмет общих зависимостей
    internal virtual ResultWithoutValue CheckConductionPossibility(IEnumerable<StudentModel>? toCheck)
    {
        // эта проверка элиминирует необходимость проверки студента на прикрепленность к этому же самому приказу, 
        // для закрытых приказов проведение невозможно
        if (toCheck is null || !toCheck.Any())
        {
            return ResultWithoutValue.Failure(new OrderValidationError("Приказ не может быть проведен без указания студентов"));
        }
        foreach (var s in toCheck)
        {   
            var record = s.History.GetLastRecord();
            if (record is not null){
                var order = record.OrderNullRestict;
                if (order._isClosed){
                    return ResultWithoutValue.Failure(new OrderValidationError(string.Format("Cтудент {0} зарегистрирован в незакрытом приказе {1}", s.GetName(), order.OrderDisplayedName)));
                }
                if (order._effectiveDate > _effectiveDate
                    || (order._effectiveDate == _effectiveDate && order._creationTimestamp > _creationTimestamp)
                ){
                    ResultWithoutValue.Failure(
                        new OrderValidationError(
                            string.Format("Приказ {0} от {1} не является хронологически последовательным для студента {2}, ему предшествует приказ {3} от {4}",
                                this.OrderDisplayedName, Utils.FormatDateTime(this.SpecifiedDate), s.GetName(), order.OrderDisplayedName, Utils.FormatDateTime(order.SpecifiedDate)) 
                            )
                        );
                }
            }
        }
        return ResultWithoutValue.Success();
    }

    public OrderTypeInfo GetOrderTypeDetails()
    {
        return OrderTypeInfo.GetByType(GetOrderType());
    }
    public abstract ResultWithoutValue ConductByOrder();
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
    // получение приказа по Id независимо от типа
    public static Order? GetOrderById(int? id)
    {
        if (id is null)
        {
            return null;
        }
        var paramCollection = new SQLParameterCollection();
        var p1 = paramCollection.Add((int)id);
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("id", "orders"),
                p1,
                WhereCondition.Relations.Equal
            )
        );
        var found = FindOrders(
            new QueryLimits(0, 1),
            filter: where,
            additionalParams: paramCollection
        ).Result;
        if (found.Any())
        {
            return found.First();
        }
        return null;
    }

    public static async Task<Result<Order>> GetOrderForConduction(int id, string conductionDataDTO)
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
                return Result<Order>.Failure(new ValidationError(nameof(id), "Приказа не существует"));
            }
            reader.Read();
            OrderTypes type = (OrderTypes)(int)reader["type"];
            Utilities.IResult? result = null;
            try {
                result = _orderForConductionMappings[type](id, conductionDataDTO);
            }
            catch {
                return Result<Order>.Failure(new ValidationError(nameof(id), "Аргументы приказа не соответствуют его типу"));
            }
            if (result.IsFailure)
            {
                return Result<Order>.Failure(result.GetErrors());
            }
            return Result<Order>.Success((Order)result.GetResultObject());
        }
    }
    public static Result<Order> Build(string orderJson)
    {
        OrderDTO? mapped = null;
        try
        {
            mapped = JsonSerializer.Deserialize<OrderDTO>(orderJson);
        }
        catch (Exception ex)
        {
            return Result<Order>.Failure(new ValidationError(nameof(orderJson), ex.Message));
        }

        var err = (mapped is not null).CheckRuleViolation("Ошибка при маппинге JSON");

        if (mapped is null)
        {
            return Result<Order>.Failure(new OrderValidationError("Ошибка при маппинге json"));
        }

        if (!TryParseOrderType(mapped.OrderType))
        {
            return Result<Order>.Failure(new OrderValidationError("Неверно указан тип приказа"));
        }

        OrderTypes type = (OrderTypes)mapped.OrderType;
        Utilities.IResult? result = null;
        try {
            result = _orderFromDtoMappings[type](mapped);
        }
        catch {
            return Result<Order>.Failure(new OrderValidationError("Данный тип приказа не поддерживается"));
        }
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.GetErrors());
        }
        return Result<Order>.Success((Order)result.GetResultObject());
    }
    public void Close()
    {
        if (!_isClosed)
        {
            SetOpenCloseState(true);
        }
    }

    private void Open()
    {
        if (_isClosed)
        {
            SetOpenCloseState(false);
        }
    }

    private void SetOpenCloseState(bool closed)
    {
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        var cmdText = "UPDATE orders SET is_closed = @p2 WHERE id = @p1";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", _id));
        cmd.Parameters.Add(new NpgsqlParameter<bool>("p2", closed));
        cmd.ExecuteNonQuery();
        cmd.Dispose();
        _isClosed = closed;
    }

    public static IReadOnlyCollection<Order> FindWithinRangeSortedByTime(DateTime start, DateTime end, OrderByCondition.OrderByTypes orderBy = OrderByCondition.OrderByTypes.ASC)
    {
        if (end < start)
        {
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

    public virtual void RevertAllConducted()
    {
        var history = new OrderHistory(this);
        foreach (var rec in history.History)
        {
            var student = rec.Student;
            if (student is null)
            {
                throw new Exception("Студент в истории должен быть указан");
            }
            student.History.RevertHistory(this);
            Open();
        }
    }
    public virtual void RevertConducted(IEnumerable<StudentModel> student)
    {
        foreach (var std in student)
        {
            var history = std.History;
            history.RevertHistory(this);
        }

    }
    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not Order)
        {
            return false;
        }
        return ((Order)obj)._id == this._id;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

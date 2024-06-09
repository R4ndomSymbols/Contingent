using Npgsql;
using Contingent.Utilities;
using Contingent.Utilities.Validation;
using Contingent.SQL;
using Contingent.Controllers.DTO.In;
using System.Text.Json;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Models.Domain.Orders.Infrastructure;
using Contingent.Models.Domain.Flow.History;
using Contingent.Import;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public abstract class Order : IFromCSV<Order>
{
    protected int _id;
    protected DateTime _specifiedDate;
    protected DateTime _effectiveDate;
    // порядковый номер приказа в году и по типу
    protected int _orderNumber;
    protected string? _orderDescription;
    protected string _orderDisplayedName;
    private OrderConductionStatus _conductionStatus;
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
    public bool IsOpen
    {
        get => !_isClosed;
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

    private static readonly Dictionary<OrderTypes,
        (Func<int, NpgsqlDataReader, Utilities.IResult> byId,
         Func<int, string, Utilities.IResult> forConduction,
         Func<OrderDTO?, Utilities.IResult> fromDTO
        )>

    _orderTypeMappings = new()
    {
        {
            OrderTypes.FreeEnrollment,
            (FreeEnrollmentOrder.Create,
            (id, jsonString) => FreeEnrollmentOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
            FreeEnrollmentOrder.Create)
        },
        {
            OrderTypes.FreeEnrollmentFromAnotherOrg,
            (
                FreeEnrollmentWithTransferOrder.Create,
                (id, jsonString) => FreeEnrollmentWithTransferOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                FreeEnrollmentWithTransferOrder.Create
            )
        },
        {
            OrderTypes.FreeReEnrollment,
            (
                FreeReEnrollmentOrder.Create,
                (id, jsonString) => FreeReEnrollmentOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                FreeReEnrollmentOrder.Create
            )
        },
        {
            OrderTypes.FreeTransferBetweenSpecialties,
            (
                FreeTransferWithinCourseOrder.Create,
                (id, jsonString) => FreeTransferWithinCourseOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                FreeTransferWithinCourseOrder.Create
            )
        },
        {
            OrderTypes.FreeTransferNextCourse,
            (
                FreeTransferToTheNextCourseOrder.Create,
                (id, jsonString) => FreeTransferToTheNextCourseOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                FreeTransferToTheNextCourseOrder.Create
            )
        },
        {
            OrderTypes.FreeTransferFromPaidToFree,
            (
                FreeTransferFromPaidToFreeOrder.Create,
                (id, jsonString) => FreeTransferFromPaidToFreeOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                FreeTransferFromPaidToFreeOrder.Create
            )
        },
        {
            OrderTypes.FreeDeductionWithAcademicDebt,
            (
                FreeDeductionWithAcademicDebtOrder.Create,
                (id, jsonString) => FreeDeductionWithAcademicDebtOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                FreeDeductionWithAcademicDebtOrder.Create
            )
        },
        {
            OrderTypes.FreeDeductionWithGraduation,
            (
                FreeDeductionWithGraduationOrder.Create,
                (id, jsonString) => FreeDeductionWithGraduationOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                FreeDeductionWithGraduationOrder.Create
            )
        },
        {
            OrderTypes.FreeDeductionWithOwnDesire,
            (
                FreeDeductionWithOwnDesireOrder.Create,
                (id, jsonString) => FreeDeductionWithOwnDesireOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                FreeDeductionWithOwnDesireOrder.Create
            )
        },
        {
            OrderTypes.FreeDeductionWithTransfer,
            (
                FreeDeductionWithTransferOrder.Create,
                (id, jsonString) => FreeDeductionWithTransferOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                FreeDeductionWithTransferOrder.Create
            )
        },
        {
            OrderTypes.FreeDeductionWithAcademicVacationNoReturn,
            (
                FreeDeductionWithAcademicVacationNoReturnOrder.Create,
                (id, jsonString) => FreeDeductionWithAcademicVacationNoReturnOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                FreeDeductionWithAcademicVacationNoReturnOrder.Create
            )
        },
        {
            OrderTypes.FreeDeductionWithEducationProcessNotInitiated,
            (
                FreeDeductionWithEducationProcessNotInitiatedOrder.Create,
                (id, jsonString) => FreeDeductionWithEducationProcessNotInitiatedOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                FreeDeductionWithEducationProcessNotInitiatedOrder.Create
            )
        },
        {
            OrderTypes.FreeAcademicVacationReturn,
            (
                FreeAcademicVacationReturnOrder.Create,
                (id, jsonString) => FreeAcademicVacationReturnOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                FreeAcademicVacationReturnOrder.Create
            )
        },
        {
            OrderTypes.FreeAcademicVacationSend,
            (
                FreeAcademicVacationSendOrder.Create,
                (id, jsonString) => FreeAcademicVacationSendOrder.Create(id, JsonSerializer.Deserialize<StudentDurableStatesDTO>(jsonString)),
                FreeAcademicVacationSendOrder.Create
            )
        },
        {
            OrderTypes.PaidEnrollment,
            (
                PaidEnrollmentOrder.Create,
                (id, jsonString) => PaidEnrollmentOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                PaidEnrollmentOrder.Create
            )
        },
        {
            OrderTypes.PaidReEnrollment,
            (
                PaidReEnrollmentOrder.Create,
                (id, jsonString) => PaidReEnrollmentOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                PaidReEnrollmentOrder.Create
            )
        },
        {
            OrderTypes.PaidEnrollmentWithTransfer,
            (
                PaidEnrollmentWithTransferOrder.Create,
                (id, jsonString) => PaidEnrollmentWithTransferOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                PaidEnrollmentWithTransferOrder.Create
            )
        },
        {
            OrderTypes.PaidTransferBetweenSpecialties,
            (
                PaidTransferBetweenSpecialtiesOrder.Create,
                (id, jsonString) => PaidTransferBetweenSpecialtiesOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                PaidTransferBetweenSpecialtiesOrder.Create
            )
        },
        {
            OrderTypes.PaidTransferNextCourse,
            (
                PaidTransferNextCourseOrder.Create,
                (id, jsonString) => PaidTransferNextCourseOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                PaidTransferNextCourseOrder.Create
            )
        },
        {
            OrderTypes.PaidDeductionWithAcademicDebt,
            (
                PaidDeductionWithAcademicDebtOrder.Create,
                (id, jsonString) => PaidDeductionWithAcademicDebtOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                PaidDeductionWithAcademicDebtOrder.Create
            )
        },
        {
            OrderTypes.PaidDeductionWithGraduation,
            (
                PaidDeductionWithGraduationOrder.Create,
                (id, jsonString) => PaidDeductionWithGraduationOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                PaidDeductionWithGraduationOrder.Create
            )
        },
        {
            OrderTypes.PaidDeductionWithOwnDesire,
            (
                PaidDeductionWithOwnDesireOrder.Create,
                (id, jsonString) => PaidDeductionWithOwnDesireOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                PaidDeductionWithOwnDesireOrder.Create
            )
        },
        {
            OrderTypes.PaidDeductionWithTransfer,
            (
                PaidDeductionWithTransferOrder.Create,
                (id, jsonString) => PaidDeductionWithTransferOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                PaidDeductionWithTransferOrder.Create
            )
        },
        {
            OrderTypes.PaidDeductionWithAcademicVacationNoReturn,
            (
                PaidDeductionWithAcademicVacationNoReturnOrder.Create,
                (id, jsonString) => PaidDeductionWithAcademicVacationNoReturnOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                PaidDeductionWithAcademicVacationNoReturnOrder.Create
            )
        },
        {
            OrderTypes.PaidDeductionWithEducationProcessNotInitiated,
            (
                PaidDeductionWithEducationProcessNotInitiatedOrder.Create,
                (id, jsonString) => PaidDeductionWithEducationProcessNotInitiatedOrder.Create(id, JsonSerializer.Deserialize<StudentGroupNullifyMovesDTO>(jsonString)),
                PaidDeductionWithEducationProcessNotInitiatedOrder.Create
            )
        },
        {
            OrderTypes.PaidAcademicVacationSend,
            (
                PaidAcademicVacationSendOrder.Create,
                (id, jsonString) => PaidAcademicVacationSendOrder.Create(id, JsonSerializer.Deserialize<StudentDurableStatesDTO>(jsonString)),
                PaidAcademicVacationSendOrder.Create
            )
        },
        {
            OrderTypes.PaidAcademicVacationReturn,
            (
                PaidAcademicVacationReturnOrder.Create,
                (id, jsonString) => PaidAcademicVacationReturnOrder.Create(id, JsonSerializer.Deserialize<StudentToGroupMovesDTO>(jsonString)),
                PaidAcademicVacationReturnOrder.Create
            )
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
                Utilities.IResult? result = _orderTypeMappings[typeGot].byId(id, reader);
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
        _id = Utils.INVALID_ID;
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
    protected static Result<T> MapBase<T>(OrderDTO? source, T model, ObservableTransaction? scope = null) where T : Order
    {
        // добавить проверку на диапазон дат дату, но потом
        var errors = new List<ValidationError>();
        if (source is null)
        {
            return Result<T>.Failure(new ValidationError("DTO приказа не может быть пустым"));
        }

        if (errors.IsValidRule(Utils.TryParseDate(source.EffectiveDate, out DateTime effDate),
        message: "Дата вступления в силу указана неверно",
        propName: nameof(EffectiveDate)))
        {
            model._effectiveDate = effDate;
        }
        if (errors.IsValidRule(
            Utils.TryParseDate(source.SpecifiedDate, out DateTime specDate),
            message: "Дата приказа указана неверно",
            propName: nameof(SpecifiedDate)
        ))
        {
            model._specifiedDate = specDate;
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
            message: "Имя приказа указано неверно",
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
            model._orderNumber = model.SequentialGuardian.GetSequentialOrderNumber(model, scope);
            return Result<T>.Success(model);
        }
    }
    protected static QueryResult<T?> MapPartialFromDbBase<T>(NpgsqlDataReader reader, T model) where T : Order
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
    // все проводимые приказы всегда будут хронологически последними на каждого из студентов, входящих в приказ
    protected void ConductBase(IEnumerable<StudentFlowRecord>? records, ObservableTransaction? scope)
    {
        if (records is null || !records.Any() || _conductionStatus != OrderConductionStatus.ConductionReady || IsClosed)
        {
            throw new Exception("Ошибка при записи в таблицу движения: данные приказа или приказ не соотвествуют форме или приказ закрыт");
        }
        StudentFlowRecord.InsertBatch(records, scope);
        _conductionStatus = OrderConductionStatus.Conducted;
    }
    protected abstract OrderTypes GetOrderType();
    public virtual void Save(ObservableTransaction scope)
    {
        _conductionStatus = OrderConductionStatus.ConductionNotValidated;
        if (Utils.IsValidId(_id))
        {
            return;
        }
        NpgsqlConnection? conn = Utils.GetAndOpenConnectionFactory().Result;
        string cmdText = "INSERT INTO public.orders( " +
        " specified_date, effective_date, serial_number, org_id, type, name, description, is_closed, creation_timestamp) " +
        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9) RETURNING id";
        NpgsqlCommand cmd;
        if (scope is not null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, conn);

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

        using (conn)
        using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            reader.Read();
            _id = (int)reader["id"];
            return;
        }
    }

    // проверяет только студентов на предмет общих зависимостей
    internal ResultWithoutValue CheckTreeConductionPossibility(ObservableTransaction scope)
    {
        var toCheck = GetStudentsForCheck();
        // эта проверка элиминирует необходимость проверки студента на прикрепленность к этому же самому приказу, 
        // для закрытых приказов проведение невозможно
        if (toCheck is null || !toCheck.Any())
        {
            return ResultWithoutValue.Failure(new ValidationError("OrderGenericError", "Приказ не может быть проведен без указания студентов"));
        }
        if (IsClosed)
        {
            return ResultWithoutValue.Failure(new ValidationError("OrderGenericError", "Проведение для закрытого приказа невозможно"));
        }
        foreach (var student in toCheck)
        {
            var record = student.GetHistory(scope).GetLastRecord();
            if (record is not null)
            {
                var order = record.OrderNullRestrict;
                // если предыдущий приказ открыт, то проведение невозможно
                if (order.IsOpen)
                {
                    return ResultWithoutValue.Failure(new OrderValidationError(string.Format("зарегистрирован в незакрытом приказе {0}", order.OrderDisplayedName), student));
                }
                // проверка хронологичности приказа, на студента
                // если последний приказ на студента есть этот же самый приказ, то проведение невозможно
                if (order._effectiveDate > _effectiveDate
                    || (order._effectiveDate == _effectiveDate && order._creationTimestamp >= _creationTimestamp)
                )
                {
                    ResultWithoutValue.Failure(
                        new OrderValidationError(
                            string.Format("Приказ {0} от {1} не является хронологически последовательным для студента, ему предшествует приказ {2} от {3}",
                                this.OrderDisplayedName, Utils.FormatDateTime(this.SpecifiedDate), order.OrderDisplayedName, Utils.FormatDateTime(order.SpecifiedDate))
                            , student)
                        );
                }
            }
        }
        var subclassCheck = CheckOrderClassSpecificConductionPossibility(toCheck, scope);
        if (subclassCheck.IsFailure)
        {
            return subclassCheck;
        }
        _conductionStatus = OrderConductionStatus.ConductionReady;
        return ResultWithoutValue.Success();
    }

    protected abstract ResultWithoutValue CheckOrderClassSpecificConductionPossibility(IEnumerable<StudentModel> toCheck, ObservableTransaction scope);
    protected abstract IEnumerable<StudentModel>? GetStudentsForCheck();

    public OrderTypeInfo GetOrderTypeDetails()
    {
        return OrderTypeInfo.GetByType(GetOrderType());
    }
    public ResultWithoutValue ConductByOrder(ObservableTransaction scope)
    {
        var check = CheckTreeConductionPossibility(scope);
        if (check.IsFailure)
        {
            return check;
        }
        return ConductByOrderInternal(scope);
    }
    protected abstract ResultWithoutValue ConductByOrderInternal(ObservableTransaction scope);
    public static async Task<IReadOnlyCollection<Order>> FindOrders(QueryLimits limits, ComplexWhereCondition? filter = null, JoinSection? additionalJoins = null, SQLParameterCollection? additionalParams = null, OrderByCondition? orderBy = null, ObservableTransaction? scope = null)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var mapper = GetMapper(null);
        var predefinedOrderBy = new OrderByCondition(new Column("specified_date", "orders"), OrderByCondition.OrderByTypes.DESC);
        predefinedOrderBy.AddColumn(new Column("creation_timestamp", "orders"), OrderByCondition.OrderByTypes.DESC);
        var result = SelectQuery<Order>.Init("orders")
        .AddMapper(mapper)
        .AddJoins(additionalJoins)
        .AddWhereStatement(filter)
        .AddOrderByStatement(orderBy ?? predefinedOrderBy)
        .AddParameters(additionalParams)
        .Finish();
        if (result.IsFailure)
        {
            throw new Exception("Запрос сгенерирован неверно");
        }
        var query = result.ResultObject;
        return await query.Execute(conn, limits, scope);
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
            try
            {
                result = _orderTypeMappings[type].forConduction(id, conductionDataDTO);
            }
            catch
            {
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
            return Result<Order>.Failure(new ValidationError("Ошибка при маппинге json"));
        }

        if (!TryParseOrderType(mapped.OrderType))
        {
            return Result<Order>.Failure(new ValidationError("Неверно указан тип приказа"));
        }

        OrderTypes type = (OrderTypes)mapped.OrderType;
        Utilities.IResult? result = null;
        try
        {
            result = _orderTypeMappings[type].fromDTO(mapped);
        }
        catch
        {
            return Result<Order>.Failure(new ValidationError("Данный тип приказа не поддерживается"));
        }
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.GetErrors());
        }
        return Result<Order>.Success((Order)result.GetResultObject());
    }
    public void Close(ObservableTransaction scope)
    {
        if (IsOpen)
        {
            SetOpenCloseState(true, scope);
        }
    }

    private void Open()
    {
        if (IsClosed)
        {
            SetOpenCloseState(false, null);
        }
    }

    private void SetOpenCloseState(bool closed, ObservableTransaction? scope)
    {
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        var cmdText = "UPDATE orders SET is_closed = @p2 WHERE id = @p1";
        NpgsqlCommand cmd;
        if (scope is not null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        using (cmd)
        {
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", _id));
            cmd.Parameters.Add(new NpgsqlParameter<bool>("p2", closed));
            cmd.ExecuteNonQuery();
            cmd.Dispose();
            _isClosed = closed;
        }
    }

    public static IReadOnlyCollection<Order> FindWithinRangeSortedByTime(DateTime start, DateTime end, OrderByCondition.OrderByTypes orderBy = OrderByCondition.OrderByTypes.ASC, ObservableTransaction? scope = null)
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
            new Column("creation_timestamp", "orders"), orderBy == OrderByCondition.OrderByTypes.DESC ? OrderByCondition.OrderByTypes.ASC : OrderByCondition.OrderByTypes.DESC
        );
        return FindOrders(new QueryLimits(0, 500), additionalParams: parameters, filter: where, orderBy: orderByClause, scope: scope).Result;
    }
    public static IReadOnlyCollection<Order> FindOrdersByParameters(OrderSearchParameters parameters)
    {
        var where = ComplexWhereCondition.Empty;
        var args = new SQLParameterCollection();

        if (parameters.EndDate is not null)
        {
            where = where.Unite(
                ComplexWhereCondition.ConditionRelation.AND,
                new ComplexWhereCondition(new WhereCondition(
                    new Column("specified_date", "orders"),
                    args.Add(parameters.EndDate),
                    WhereCondition.Relations.LessOrEqual
                ))

            );
        }
        if (parameters.StartDate is not null)
        {
            where = where.Unite(
                ComplexWhereCondition.ConditionRelation.AND,
                new ComplexWhereCondition(new WhereCondition(
                    new Column("specified_date", "orders"),
                    args.Add(parameters.StartDate),
                    WhereCondition.Relations.BiggerOrEqual
                ))
            );
        }
        if (parameters.Year is not null)
        {
            where = where.Unite(
                ComplexWhereCondition.ConditionRelation.AND,
                new ComplexWhereCondition(new WhereCondition(
                    Column.GetRaw("EXTRACT(YEAR FROM orders.specified_date)"),
                    args.Add((int)parameters.Year),
                    WhereCondition.Relations.Equal
                ))
            );
        }
        if (parameters.OrderOrgId is not null)
        {
            where = where.Unite(
                ComplexWhereCondition.ConditionRelation.AND,
                new ComplexWhereCondition(new WhereCondition(
                    new Column("lower", "org_id", "orders", null),
                    args.Add(parameters.OrderOrgId.ToLower()),
                    WhereCondition.Relations.Like
                ))
            );
        }

        var orderByClause = new OrderByCondition(new Column("specified_date", "orders"), OrderByCondition.OrderByTypes.ASC);
        orderByClause.AddColumn(
            new Column("creation_timestamp", "orders"), OrderByCondition.OrderByTypes.ASC
        );
        return FindOrders(new QueryLimits(0, 500), additionalParams: args, filter: where, orderBy: orderByClause).Result;
    }


    public virtual void RevertAllConducted(ObservableTransaction scope)
    {
        var history = new OrderHistory(this);
        foreach (var rec in history.History)
        {
            var student = rec.Student;
            if (student is null)
            {
                throw new Exception("Студент в истории должен быть указан");
            }
            student.GetHistory(scope).RevertHistory(this);
            Open();
        }
    }
    public virtual void RevertConducted(IEnumerable<StudentModel> student, ObservableTransaction scope)
    {
        foreach (var std in student)
        {
            var history = std.GetHistory(scope);
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

    public abstract Result<Order> MapFromCSV(CSVRow row);

    public readonly static Comparison<Order> OrderByEffectiveDateComparison = (orderLeft, orderRight) =>
    {

        if (orderLeft.Equals(orderRight))
        {
            return 0;
        }
        if (orderLeft.EffectiveDate == orderRight.EffectiveDate)
        {
            if (orderLeft.OrderCreationDate == orderRight.OrderCreationDate)
            {
                return 0;
            }
            else if (orderLeft.OrderCreationDate > orderRight.OrderCreationDate)
            {
                return 1;
            }
            return -1;
        }
        else if (orderLeft.EffectiveDate > orderRight.EffectiveDate)
        {
            return 1;
        }
        return -1;
    };


}

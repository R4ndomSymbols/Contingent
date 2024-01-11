using System.Xml.Linq;
using Npgsql;
using StudentTracking.Models.Services;
using StudentTracking.Models.JSON;
using Utilities;
using Utilities.Validation;
using StudentTracking.Models.JSON.Responses;
using StudentTracking.Models.SQL;
using System.Data.SqlTypes;

namespace StudentTracking.Models.Domain.Orders;

public abstract class Order : DbValidatedObject
{
    protected int _id;
    protected DateTime _specifiedDate;
    protected DateTime _effectiveDate;
    // получать из базы (при создании или чтении)
    protected int _orderNumber;
    // автогенерация
    protected string _orderStringId;
    protected string _orderDescription;
    protected string _orderDisplayedName;

    public int Id
    {
        get => _id;
    }
    public void SetEffectiveDateFromString(string? effectiveDate)
    {
        if (!PerformValidation(
            () => !string.IsNullOrEmpty(effectiveDate) && !string.IsNullOrWhiteSpace(effectiveDate),
            new ValidationError(nameof(EffectiveDate), "Дата вступления в силу должна быть указана")
        ))
        {
            return;
        }
        if (PerformValidation(
            () => Utils.TryParseDate(effectiveDate),
            new ValidationError(nameof(EffectiveDate), "Дата вступления в силу указана в неверном формате")
        ))
        {
            EffectiveDate = Utils.ParseDate(effectiveDate);
        }
    }
    public void SetSpecifiedDateFromString(string? specifiedDate)
    {
        if (!PerformValidation(
            () => !string.IsNullOrEmpty(specifiedDate) && !string.IsNullOrWhiteSpace(specifiedDate),
            new ValidationError(nameof(SpecifiedDate), "Дата приказа должна быть указана")
        ))
        {
            return;
        }
        if (PerformValidation(
            () => Utils.TryParseDate(specifiedDate),
            new ValidationError(nameof(SpecifiedDate), "Дата приказа указана в неверном формате")
        ))
        {
            SpecifiedDate = Utils.ParseDate(specifiedDate);
        }
    }

    public DateTime EffectiveDate
    {
        get => _effectiveDate;
        set
        {
            var maxDate = DateTime.Now;
            maxDate = maxDate.AddYears(1);
            if (PerformValidation(
                () => ValidatorCollection.CheckDateRange(value, new DateTime(Utils.ORG_CREATION_YEAR, 1, 1), maxDate),
                new ValidationError(nameof(EffectiveDate), "Дата вступления в силу указана неверно")
            ))
            {
                _effectiveDate = value;
            }
        }
    }
    public DateTime SpecifiedDate
    {
        get => _specifiedDate;
        set
        {
            var maxDate = DateTime.Now;
            maxDate = maxDate.AddYears(1);
            if (PerformValidation(
                () => ValidatorCollection.CheckDateRange(value, new DateTime(Utils.ORG_CREATION_YEAR, 1, 1), maxDate),
                new ValidationError(nameof(SpecifiedDate), "Дата приказа указана неверно")
            ))
            {
                _specifiedDate = value;
            }
        }
    }
    public string FormattedSpecifiedDate{
        get => Utils.FormatDateTime(_specifiedDate);
    }
    public string FormattedEffectiveDate{
        get => Utils.FormatDateTime(_effectiveDate);
    }
    // порядковый номер приказа, рассчитывается в пределах года
    public int OrderNumber
    {
        get => _orderNumber;
    }

    public virtual string OrderStringId
    {
        get => _orderNumber.ToString();
    }

    public string OrderDescription
    {
        get => _orderDescription;
        set
        {
            if (PerformValidation(
                () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText) && !string.IsNullOrWhiteSpace(value),
                new ValidationError(nameof(OrderDescription), "Описание приказа указано с нарушением формы")
            ))
            {
                _orderDescription = value;
            }
        }
    }
    public string OrderDisplayedName
    {
        get => _orderDisplayedName;
        set
        {
            if (PerformValidation(
                () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText) && !string.IsNullOrWhiteSpace(value),
                new ValidationError(nameof(OrderDisplayedName), "Название приказа указано в нарушение формы")
            ))
            {
                _orderDisplayedName = value;
            }
        }
    }

    protected Order() : base()
    {
        RegisterProperty(nameof(EffectiveDate));
        RegisterProperty(nameof(SpecifiedDate));
        RegisterProperty(nameof(OrderDescription));
        RegisterProperty(nameof(OrderDisplayedName));
        _orderStringId = "";
        _orderDescription = "";
        _orderDisplayedName = "";
        _effectiveDate = DateTime.Now;
        _specifiedDate = DateTime.Now;
    }
    protected Order(int id) : base(RelationTypes.Bound)
    {
        _orderStringId = "";
        _id = id;
        _orderDescription = "";
        _orderDisplayedName = "";
    }

    protected abstract Task<bool> CheckInsertionPossibility();
    public abstract OrderTypes GetOrderType();
    public abstract Task<bool> ConductOrder(object orderConductData);

    public abstract Task Save(ObservableTransaction? scope);
    public virtual async Task FromJSON(OrderModelJSON json)
    {
        SetEffectiveDateFromString(json.EffectiveDate);
        SetSpecifiedDateFromString(json.SpecifiedDate);
        OrderDescription = json.OrderDescription;
        OrderDisplayedName = json.OrderDisplayedName;
        await RequestIdentity();
    }
    public abstract Task<bool> AddPendingMoves(OrderInStudentFlowJSON json);
    protected async Task RequestIdentity()
    {
        if (!CheckPropertyValidity(nameof(SpecifiedDate)))
        {
            return;
        }
        _orderNumber = await OrderConsistencyMaintain.GetNextOrderNumber(_specifiedDate);
    }

    public override bool Equals(IDbObjectValidated? other)
    {
        throw new NotImplementedException();
    }
    public override Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {
        throw new NotImplementedException();
    }

    public static async Task<List<OrderSuggestionJSONResponse>?> FindOrders(SelectQuery<OrderSuggestionJSONResponse> query){
        using var conn = await Utils.GetAndOpenConnectionFactory();
        return await query.Execute(conn, 20, () => new OrderSuggestionJSONResponse());
    }

    public static async Task<bool> IsOrderExists(int id){
        var conn = await Utils.GetAndOpenConnectionFactory();
        string cmtText = "SELECT EXISTS(SELECT id FROM orders WHERE id = @p1)";
        NpgsqlCommand cmd = new NpgsqlCommand(cmtText, conn);
        using (conn)
        using (cmd) {
            using var reader = cmd.ExecuteReader();
            reader.Read();
            return (bool)reader["exists"];
        }
    }
    // получение приказа по Id
    // генерация запроса, т.к. существует и будет добавлено множество видов приказов

    public static async Task<Order?> GetOrderById(int id){
        var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT type FROM orders WHERE id = @p1";
        var cmd = new NpgsqlCommand(cmdText, conn);
        Order? found = null; 
        using(cmd){
            using var reader = cmd.ExecuteReader();
            if (!reader.HasRows){
                return null;
            }
            OrderTypes type = (OrderTypes)(int)reader["type"];
            // добавить еще классы
            switch(type)
            {
                case OrderTypes.FreeEnrollment:
                    found = new FreeEnrollmentOrder(id);
                    break;
                default:
                    throw new InvalidDataException("Такой тип приказа не зарегистрирован"); 
            }
        }
        var param = new SQLParameters();
        var mapper = new Mapper<Order>(
            (o, r) => {
                o._effectiveDate = (DateTime)r["effective_date"];
                o._specifiedDate = (DateTime)r["specified_date"];
                o._orderDescription = (string)r["description"];
                o._orderDisplayedName = (string)r["name"];
                o._orderNumber = (int)r["serial_number"];
                o._orderStringId = (string)r["org_id"];
            }, new List<Column>(){
                new Column("effective_date", null, "orders"),
                new Column("specified_date", null, "orders"),
                new Column("description", null, "orders"),
                new Column("name", null, "orders"),
                new Column("serial_number", null, "orders"),
                new Column("org_id", null, "orders")
            });
        JoinSection? js = null;
        ComplexWhereCondition cwc = new ComplexWhereCondition(
            new WhereCondition(param, new Column("id", null, "orders"),new SQLParameter<int>(id),  WhereCondition.Relations.Equal));
        var sqlQuery = new SelectQuery<Order>("orders", param, mapper, js, cwc); 
        await sqlQuery.Execute(conn, 1, () => found);
        await conn.DisposeAsync();
        return found;
    }

    public static async Task<Order?> Get(OrderModelJSON? json){
        OrderTypes typeConverted;
        Order? toReturn = null;
        if (json == null){
            return null;
        }
        try {
            typeConverted = (OrderTypes)json.OrderType;
        }
        catch (InvalidCastException){
            return toReturn;
        }
        
        switch (typeConverted){
            case OrderTypes.FreeEnrollment:
                toReturn = new FreeEnrollmentOrder();
                break;
            case OrderTypes.DeductionWithGraduation:
                toReturn = new DeductionWithGraduationOrder();
                break;
            case OrderTypes.TransferGroupToGroup:
                toReturn = new TransferGroupToGroupOrder();
                break;
            default:
                return null;
        }

        await toReturn.FromJSON(json);
        return toReturn;
    }

}

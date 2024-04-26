namespace StudentTracking.Models.Domain.Address;
using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.SQL;
using System.Data;
using Utilities;
public class AddressModel
{
    private FederalSubject? _subjectPart;
    private District? _districtPart;
    private SettlementArea? _settlementAreaPart;
    private Settlement? _settlementPart;
    private Street? _streetPart;
    private Building? _buildingPart;
    private Apartment? _apartmentPart;
    public int? Id
    {
        get
        {
            IAddressPart?[] parts = {
                _apartmentPart,
                _buildingPart,
                _streetPart,
                _settlementPart,
                _settlementAreaPart,
                _districtPart,
                _subjectPart
            };
            var found = parts.Where(p => p is not null && p.Id != Utils.INVALID_ID);
            if (found.Any())
            {
                return found.First().Id;
            }
            else
            {
                return null;
            }
        }
    }
    public string GetAddressInfo()
    {
        List<string> accumulator = new List<string>(){
            FormatTag(_subjectPart, "Субъект: "),
            FormatTag(_districtPart, "Муниципалитет верхнего уровня: "),
            FormatTag(_settlementAreaPart,"Муниципальное поселение: " ),
            FormatTag(_settlementPart, "Населенный пункт: "),
            FormatTag(_streetPart, "Объект дорожной инфраструктуры: "),
            FormatTag(_buildingPart, "Дом: "),
            FormatTag(_apartmentPart,"Квартира: ")
        };
        return string.Join("<br/>", accumulator);
        string FormatTag(IAddressPart? part, string prefix)
        {
            string state;
            if (part == null)
            {
                state = "Не распознан / Не обнаружен";
            }
            else if (part.Id != Utils.INVALID_ID)
            {
                state = "Обнаружен в базе";
            }
            else
            {
                state = "Будет добавлен";
            }
            string partName = prefix + (part is null ? "" : part.ToString());
            state = "[" + state + "]";
            return state + string.Join("", Enumerable.Repeat(' ', 35 - state.Length)) + partName;
        }
    }
    public override string ToString()
    {
        IAddressPart?[] parts = {
                _subjectPart,
                _districtPart,
                _settlementAreaPart,
                _settlementPart,
                _streetPart,
                _buildingPart,
                _apartmentPart
        };
        return string.Join(",", parts.Where(p => p is not null).Select(p => p.ToString()));
    }
    private AddressModel()
    {
    }
    public static async Task<AddressModel?> GetAddressById(int? id)
    {
        if (id is null || id.Value == Utils.INVALID_ID){
            return null;
        }
        var address = new AddressModel();
        var restored = await RestoreAddress((int)id);
        ProcessSubject(restored, address);
        return address;
    }
    public static IEnumerable<string> GetNextSuggestions(string? request)
    {
        if (request is null)
        {
            return new List<string>();
        }
        var trimmed = request.Trim();
        if (trimmed == string.Empty)
        {
            return new List<string>();;
        }
        var split = request.Split(',').Select(x => x.Trim()).Where(x => x != string.Empty).ToArray();
        var address = new AddressModel();
        AddressPartPointer found = new AddressPartPointer();
        ProcessSubject(0, address, split, found);
        if (found.PointTo is null)
        {
            return new List<string>();
        }
        else
        {
            var got = ((IAddressPart)found.PointTo).GetDescendants();
            return got.Select(p => p.ToString());
        }
    }
    public static Result<AddressModel?> Create(AddressInDTO? addressDTO, ObservableTransaction? scope = null)
    {
        if (addressDTO is null)
        {
            return Result<AddressModel>.Failure(new ValidationError(nameof(AddressModel), "Адрес не указан"));
        }
        string address = addressDTO.Address;
        if (string.IsNullOrEmpty(address) || string.IsNullOrWhiteSpace(address))
        {
            return Result<AddressModel>.Failure(new ValidationError(nameof(AddressModel), "Адрес не указан"));
        }
        AddressModel built = new AddressModel();
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        string[] parts = address.Split(',').Select(x => x.Trim()).ToArray();
        try
        {
            errors = ProcessSubject(0, built, parts, new AddressPartPointer(), scope);
        }
        catch (IndexOutOfRangeException e)
        {
            errors = errors.Append(new ValidationError(nameof(AddressModel), "Адрес содержит не все необходимые части"));
        }
        if (errors.Any())
        {
            return Result<AddressModel>.Failure(errors);
        }
        return Result<AddressModel>.Success(built);
    }
    public async Task<ResultWithoutValue> Save(ObservableTransaction scope)
    {
        if (_apartmentPart is not null)
        {
            await _apartmentPart.Save(scope);
            return ResultWithoutValue.Success();
        }
        else if (_buildingPart is not null)
        {
            await _buildingPart.Save(scope);
            return ResultWithoutValue.Success();
        }
        else
        {
            return ResultWithoutValue.Failure(new ValidationError(nameof(AddressModel), "Адрес не может быть сохранен"));
        }
    }
    public static async Task<IEnumerable<AddressRecord>> FindRecords(int? parentId, string name, int type, int level, ObservableTransaction? scope = null)
    {
        var param = new SQLParameterCollection();
        var p1 = parentId is null ? param.Add(DBNull.Value) : param.Add<int>((int)parentId);
        var p2 = param.Add(name);
        var p3 = param.Add(type);
        var p4 = param.Add(level);
        var filter1 = new ComplexWhereCondition(
            parentId is null ?
             new WhereCondition(
                new Column("parent_id", "address_hierarchy"),
                WhereCondition.Relations.Is) :
            new WhereCondition(
                new Column("parent_id", "address_hierarchy"),
                p1,
                WhereCondition.Relations.Equal),
            new WhereCondition(
                new Column("address_name", "address_hierarchy"),
                p2,
                WhereCondition.Relations.Equal
            ), ComplexWhereCondition.ConditionRelation.AND
            );
        var filter2 = new ComplexWhereCondition(
            new WhereCondition(
                new Column("toponym_type", "address_hierarchy"),
                p3,
                WhereCondition.Relations.Equal),
            new WhereCondition(
                new Column("address_level", "address_hierarchy"),
                p4,
                WhereCondition.Relations.Equal
            ), ComplexWhereCondition.ConditionRelation.AND
            );
        var final = new ComplexWhereCondition(filter1, filter2, ComplexWhereCondition.ConditionRelation.AND);
        var found = await FindRecords(new QueryLimits(0, 200), final, param, scope);
        return found;
    }
    // получает список всех адресов с указанным родителем
    public static async Task<IEnumerable<AddressRecord>> FindRecords(int parentId, ObservableTransaction? scope = null)
    {
        var param = new SQLParameterCollection();
        var p1 = param.Add<int>(parentId);
        var filter = new ComplexWhereCondition(
            new WhereCondition(
                new Column("parent_id", "address_hierarchy"),
                p1,
                WhereCondition.Relations.Equal));
        var found = await FindRecords(new QueryLimits(0, 200), filter, param, scope);
        return found;
    }
    public static IEnumerable<AddressRecord> FindByAddressLevel(int addressLevel){
        var param = new SQLParameterCollection();
        var p1 = param.Add<int>(addressLevel);
        var filter = new ComplexWhereCondition(
            new WhereCondition(
                new Column("address_level", "address_hierarchy"),
                p1,
                WhereCondition.Relations.Equal));
        var found = FindRecords(new QueryLimits(0, 200), filter, param, null).Result;
        return found;
    }

    private static async Task<IEnumerable<AddressRecord>> FindRecords(QueryLimits limits, ComplexWhereCondition? condition = null, SQLParameterCollection? parameters = null, ObservableTransaction? scope = null)
    {
        var mapper = new Mapper<AddressRecord>(
            (m) =>
            {
                var mapped = new AddressRecord()
                {
                    AddressPartId = (int)m["address_part_id"],
                    AddressLevelCode = (int)m["address_level"],
                    AddressName = (string)m["address_name"],
                    ToponymType = (int)m["toponym_type"],
                    ParentId = m["parent_id"].GetType() == typeof(DBNull) ? null : (int)m["parent_id"] 
                };
                return QueryResult<AddressRecord>.Found(mapped);
            },
            new List<Column>(){
                new Column("address_part_id", "address_hierarchy"),
                new Column("address_level", "address_hierarchy"),
                new Column("address_name", "address_hierarchy"),
                new Column("toponym_type", "address_hierarchy"),
                new Column("parent_id", "address_hierarchy")
            }
        );
        var sqlQuery = SelectQuery<AddressRecord>.Init("address_hierarchy")
        .AddMapper(mapper)
        .AddWhereStatement(condition)
        .AddParameters(parameters)
        .Finish();
        if (sqlQuery.IsFailure)
        {
            throw new Exception("Запрос на поиск адреса не может быть не создан");
        }
        var query = sqlQuery.ResultObject;
        using var conn = await Utils.GetAndOpenConnectionFactory();
        return await query.Execute(conn, limits, scope);
    }
    // получает все дерево ВВЕРХ от текущего id
    private static async Task<IEnumerable<AddressRecord>> RestoreAddress(int addressId)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText =
        " WITH RECURSIVE address_sequence AS ( " +
        " SELECT address_part_id, parent_id, address_level, toponym_type, address_name FROM address_hierarchy" +
        " WHERE address_part_id = @p1 " +
        " UNION " +
        " SELECT source.address_part_id, source.parent_id, source.address_level, source.toponym_type, source.address_name " +
        " FROM address_sequence AS anchor, address_hierarchy AS source " +
        " WHERE anchor.parent_id = source.address_part_id) " +
        " SELECT * FROM address_sequence ";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", addressId));
        using (cmd)
        {
            using var reader = await cmd.ExecuteReaderAsync();
            var found = new List<AddressRecord>();
            if (!reader.HasRows)
            {
                return found;
            }
            while (reader.Read())
            {
                found.Add(
                    new AddressRecord()
                    {
                        AddressPartId = (int)reader["address_part_id"],
                        AddressLevelCode = (int)reader["address_level"],
                        AddressName = (string)reader["address_name"],
                        ToponymType = (int)reader["toponym_type"],
                        ParentId = reader["parent_id"].GetType() == typeof(DBNull) ? null : (int)reader["parent_id"] 
                    }
                );
            }
            return found;
        }
    }
    public static async Task<int> SaveRecord(IAddressPart address, ObservableTransaction? transaction = null)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        string commandText = "INSERT INTO address_hierarchy (parent_id, address_level, toponym_type, address_name) " +
        "VALUES (@p1,@p2,@p3,@p4) RETURNING address_part_id";
        NpgsqlCommand cmd;
        if (transaction is null)
        {
            cmd = new NpgsqlCommand(commandText, conn);
        }
        else
        {
            cmd = new NpgsqlCommand(commandText, transaction.Connection, transaction.Transaction);
        }
        var record = address.ToAddressRecord();
        cmd.Parameters.Add(new NpgsqlParameter("p1", record.ParentId is null ? DBNull.Value : (int)record.ParentId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p2", record.AddressLevelCode));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p3", record.ToponymType));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p4", record.AddressName));
        using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            reader.Read();
            return (int)reader["address_part_id"];
        }
    }
    // функции парсинга адресной строки
    private static IEnumerable<ValidationError> ProcessSubject(int pointer, AddressModel built, string[] parts, AddressPartPointer stopPoint, ObservableTransaction? scope = null)
    {
        if (pointer == parts.Length)
        {
            return new List<ValidationError>() { new ValidationError(nameof(FederalSubject), "Адрес содержит недостаточное число частей") };
        }
        var result = FederalSubject.Create(parts[pointer], scope);
        if (result.IsSuccess)
        {
            built._subjectPart = result.ResultObject;
            stopPoint.PointTo = built._subjectPart;
            var err = ProcessDistrict(pointer + 1, built, parts, stopPoint, scope);
            return err;
        }
        else
        {
            return result.Errors;
        }
    }
    private static IEnumerable<ValidationError> ProcessDistrict(int pointer, AddressModel built, string[] parts, AddressPartPointer stopPoint, ObservableTransaction? scope = null)
    {
        if (pointer == parts.Length)
        {
            return new List<ValidationError>() { new ValidationError(nameof(District), "Адрес содержит недостаточное число частей") };
        }
        IEnumerable<ValidationError> err = new List<ValidationError>();
        var result = District.Create(parts[pointer], built._subjectPart, scope);
        if (result.IsSuccess)
        {
            built._districtPart = result.ResultObject;
            stopPoint.PointTo = built._districtPart;
            err = ProcessSettlement(pointer + 1, built, parts, stopPoint, scope);
            if (err.Any()){
                err = ProcessSettlementArea(pointer + 1, built, parts, stopPoint, scope);
            }
            return err;  
        }
        else{
            return result.Errors;
        }
        
    }
    private static IEnumerable<ValidationError> ProcessSettlementArea(int pointer, AddressModel built, string[] parts, AddressPartPointer stopPoint, ObservableTransaction? scope = null)
    {
        if (pointer == parts.Length)
        {
            return new List<ValidationError>() { new ValidationError(nameof(SettlementArea), "Адрес содержит недостаточное число частей") };
        }
        var result = SettlementArea.Create(parts[pointer], built._districtPart, scope);
        if (result.IsSuccess)
        {
            built._settlementAreaPart = result.ResultObject;
            stopPoint.PointTo = built._settlementAreaPart;
            var err = ProcessSettlement(pointer + 1, built, parts, stopPoint, scope);
            return err;
        }
        else
        {
            return result.Errors;
        }
    }
    private static IEnumerable<ValidationError> ProcessSettlement(int pointer, AddressModel built, string[] parts, AddressPartPointer stopPoint, ObservableTransaction? scope = null)
    {
        if (pointer == parts.Length)
        {
            return new List<ValidationError>() { new ValidationError(nameof(Settlement), "Адрес содержит недостаточное число частей") };
        }
        var result = Settlement.Create(parts[pointer], built._districtPart, scope);
        if (result.IsSuccess)
        {
            built._settlementPart = result.ResultObject;
            stopPoint.PointTo = built._settlementPart;
            var err = ProcessStreet(pointer + 1, built, parts, stopPoint, scope);
            return err;
        }
        else
        {
            result = Settlement.Create(parts[pointer], built._settlementAreaPart, scope);
            if (result.IsSuccess)
            {
                built._settlementPart = result.ResultObject;
                stopPoint.PointTo = built._settlementPart;
                var err = ProcessStreet(pointer + 1, built, parts, stopPoint, scope);
                return err;
            }
            return result.Errors;
        }
    }
    private static IEnumerable<ValidationError> ProcessStreet(int pointer, AddressModel built, string[] parts, AddressPartPointer stopPoint, ObservableTransaction? scope = null)
    {
        if (pointer == parts.Length)
        {
            return new List<ValidationError>() { new ValidationError(nameof(Street), "Адрес содержит недостаточное число частей") };
        }
        var result = Street.Create(parts[pointer], built._settlementPart, scope);
        if (result.IsSuccess)
        {
            built._streetPart = result.ResultObject;
            stopPoint.PointTo = built._streetPart;
            var err = ProcessBuilding(pointer + 1, built, parts, stopPoint, scope);
            return err;
        }
        else
        {
            return result.Errors;
        }
    }
    private static IEnumerable<ValidationError> ProcessBuilding(int pointer, AddressModel built, string[] parts, AddressPartPointer stopPoint, ObservableTransaction? scope = null)
    {
        if (pointer == parts.Length)
        {
            return new List<ValidationError>() { new ValidationError(nameof(Building), "Адрес содержит недостаточное число частей") };
        }
        var result = Building.Create(parts[pointer], built._streetPart, scope);
        if (result.IsSuccess)
        {
            built._buildingPart = result.ResultObject;
            stopPoint.PointTo = built._buildingPart;
            var err = ProcessApartment(pointer + 1, built, parts, stopPoint, scope);
            return err;
        }
        else
        {
            return result.Errors;
        }
    }
    private static IEnumerable<ValidationError> ProcessApartment(int pointer, AddressModel built, string[] parts, AddressPartPointer stopPoint, ObservableTransaction? scope = null)
    {
        if (pointer == parts.Length)
        {
            return new List<ValidationError>();
        }
        var result = Apartment.Create(parts[pointer], built._buildingPart, scope);
        if (result.IsSuccess)
        {
            built._apartmentPart = result.ResultObject;
            stopPoint.PointTo = built._apartmentPart;
            if (pointer + 1 != parts.Length)
            {
                return new List<ValidationError>() { new ValidationError(nameof(AddressModel), "Адрес содержит слишком много частей") };
            }
            return new List<ValidationError>();
        }
        else
        {
            return result.Errors;
        }
    }
    // секция методов, восстанавливающих адрес из списка
    private static void ProcessSubject(IEnumerable<AddressRecord> records, AddressModel toMap)
    {
        var found = records.Where(rec => rec.AddressLevelCode == FederalSubject.ADDRESS_LEVEL);
        if (found.Any())
        {
            var foundSingle = found.First();
            toMap._subjectPart = FederalSubject.Create(foundSingle);
            ProcessDistrict(records, toMap);
        }
    }
    private static void ProcessDistrict(IEnumerable<AddressRecord> records, AddressModel toMap)
    {
        var found = records.Where(rec => rec.AddressLevelCode == District.ADDRESS_LEVEL);
        if (found.Any())
        {
            var foundSingle = found.First();
            toMap._districtPart = District.Create(foundSingle, toMap._subjectPart);
            ProcessSettlementArea(records, toMap);
            ProcessSettlement(records, toMap);
        }
    }
    private static void ProcessSettlementArea(IEnumerable<AddressRecord> records, AddressModel toMap)
    {
        var found = records.Where(rec => rec.AddressLevelCode == SettlementArea.ADDRESS_LEVEL);
        if (found.Any())
        {
            var foundSingle = found.First();
            toMap._settlementAreaPart = SettlementArea.Create(foundSingle, toMap._districtPart);
        }
    }
    private static void ProcessSettlement(IEnumerable<AddressRecord> records, AddressModel toMap)
    {
        var found = records.Where(rec => rec.AddressLevelCode == Settlement.ADDRESS_LEVEL);
        if (found.Any())
        {
            var foundSingle = found.First();
            if (toMap._settlementAreaPart is not null)
            {
                toMap._settlementPart = Settlement.Create(foundSingle, toMap._settlementAreaPart);
            }
            else
            {
                toMap._settlementPart = Settlement.Create(foundSingle, toMap._districtPart);
            }
            ProcessStreet(records, toMap);
        }
    }
    private static void ProcessStreet(IEnumerable<AddressRecord> records, AddressModel toMap)
    {
        var found = records.Where(rec => rec.AddressLevelCode == Street.ADDRESS_LEVEL);
        if (found.Any())
        {
            var foundSingle = found.First();
            toMap._streetPart = Street.Create(foundSingle, toMap._settlementPart);
            ProcessBuilding(records, toMap);
        }
    }
    private static void ProcessBuilding(IEnumerable<AddressRecord> records, AddressModel toMap)
    {
        var found = records.Where(rec => rec.AddressLevelCode == Building.ADDRESS_LEVEL);
        if (found.Any())
        {
            var foundSingle = found.First();
            toMap._buildingPart = Building.Create(foundSingle, toMap._streetPart);
        }
    }
    private static void ProcessApartment(IEnumerable<AddressRecord> records, AddressModel toMap)
    {
        var found = records.Where(rec => rec.AddressLevelCode == Apartment.ADDRESS_LEVEL);
        if (found.Any())
        {
            var foundSingle = found.First();
            toMap._apartmentPart = Apartment.Create(foundSingle, toMap._buildingPart);
        }
    }

    public bool Contains(IAddressPart? part){
        if (part is null){
            return false;
        }
        IAddressPart?[] parts = {
                _apartmentPart,
                _buildingPart,
                _streetPart,
                _settlementPart,
                _settlementAreaPart,
                _districtPart,
                _subjectPart
        };
        return parts.Any(x => part.Equals(x));
    }

}
internal class AddressPartPointer
{
    public IAddressPart? PointTo { get; set; }
    internal AddressPartPointer()
    {
        PointTo = null;
    }
}
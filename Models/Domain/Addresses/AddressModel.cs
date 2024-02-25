namespace StudentTracking.Models.Domain.Address;

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.JSON;
using StudentTracking.SQL;
using System.Data;
using Utilities;
using Utilities.Validation;

public class AddressModel
{

    private int _id;
    private FederalSubject _subjectPart;
    private District _districtPart;
    private SettlementArea? _settlementAreaPart;
    private Settlement _settlementPart;
    private Street _streetPart;
    private Building _buildingPart;
    private Apartment? _apartmentPart;


    public int Id
    {
        get => _id;
    }

    private string? SubjectPart
    {
        get => _subjectPart == null ? "" : _subjectPart.LongTypedName;
        set
        {
            if (PerformValidation(
                () =>
                {
                    var parsed = FederalSubject.BuildByName(value);
                    return !(parsed == null || parsed.CheckErrorsExist());
                }, new ValidationError(nameof(SubjectPart), "Субъект не указан или указан не верно")
            ))
            {
                _subjectPart = FederalSubject.BuildByName(value);
            }
        }
    }
    private string? DistrictPart
    {
        get => _districtPart == null ? "" : _districtPart.LongTypedName;
        set
        {
            if (PerformValidation(
                () =>
                {
                    if (_subjectPart == null)
                    {
                        return false;
                    }
                    var parsed = District.BuildByName(value);
                    return !(parsed == null || parsed.CheckErrorsExist());
                }, new ValidationError(nameof(DistrictPart), "Район не указан, указан неверно либо не соотнесен с субъектом")
            ))
            {
                if (_subjectPart != null)
                {
                    _districtPart = District.BuildByName(value);
                }
            }
        }
    }
    private string? SettlementAreaPart
    {
        get => _settlementAreaPart == null ? "" : _settlementAreaPart.LongTypedName;
        set
        {
            if (PerformValidation(
                () => _districtPart != null,
                new ValidationError(nameof(SettlementAreaPart), "Не указан верхний муниципалитет")
            ))
            {
                if (PerformValidation(
                () =>
                {
                    var parsed = SettlementArea.BuildByName(value);
                    return !(parsed == null || parsed.CheckErrorsExist());
                }, new ValidationError(nameof(SettlementAreaPart), "Поселение указано неверно (возможно, нужно ввести населенный пункт)")))
                {
                    _settlementAreaPart = SettlementArea.BuildByName(value);
                    return;
                }
                if (PerformValidation(
                () =>
                {
                    return value == null;
                }, new ValidationError(nameof(SettlementAreaPart), "Поселение указано неверно (возможно, нужно ввести населенный пункт)")))
                {
                    _settlementAreaPart = null;
                }

            }
        }
    }
    private string SettlementPart
    {
        get => _settlementPart == null ? "" : _settlementPart.LongTypedName;
        set
        {

            if (PerformValidation(
                () =>
                {
                    if (_districtPart == null)
                    {
                        return false;
                    }
                    var parsed = Settlement.BuildByName(value);
                    return !(parsed == null || parsed.CheckErrorsExist());
                }, new ValidationError(nameof(SettlementPart), "Населенный пункт не указан, указан неверно либо не определен предшествующий адрес")
            ))
            {
                _settlementPart = Settlement.BuildByName(value);
            }
        }
    }
    private string StreetPart
    {
        get => _streetPart == null ? "" : _streetPart.LongTypedName;
        set
        {
            if (PerformValidation(
                () =>
                {
                    if (_settlementPart == null)
                    {
                        return false;
                    }
                    else
                    {
                        var parsed = Street.BuildByName(value);
                        return !(parsed == null || parsed.CheckErrorsExist());
                    }
                }, new ValidationError(nameof(StreetPart), "Улица не указана, указана неверно либо не определен предшествующий адрес")
            ))
            {
                _streetPart = Street.BuildByName(value);
            }
        }
    }
    private string BuildingPart
    {
        get => _buildingPart == null ? "" : _buildingPart.LongTypedName;
        set
        {
            if (PerformValidation(
                () =>
                {
                    if (_streetPart == null)
                    {
                        return false;
                    }
                    else
                    {
                        var parsed = Building.BuildByName(value);
                        return !(parsed == null || parsed.CheckErrorsExist());
                    }
                }, new ValidationError(nameof(BuildingPart), "Дом не указан, указан неверно либо не определен предшествующий адрес")
            ))
            {
                _buildingPart = Building.BuildByName(value);
            }
        }
    }
    private string? ApartmentPart
    {
        get => _apartmentPart == null ? "" : _apartmentPart.LongTypedName;
        set
        {
            if (PerformValidation(
                () =>
                {
                    if (_buildingPart == null)
                    {
                        return false;
                    }
                    if (value == null)
                    {
                        return true;
                    }
                    else
                    {
                        var parsed = Apartment.BuildByName(value);
                        return parsed != null && !parsed.CheckErrorsExist();
                    }
                }, new ValidationError(nameof(ApartmentPart), "Квартира указана неверно либо не определен предшествующий адрес")
            ))
            {
                _apartmentPart = Apartment.BuildByName(value);
            }
        }
    }

    public async Task<string> GetAddressInfo()
    {
        async Task<string> FormatTag(DbValidatedObject? obj)
        {
            string state = "";
            if (obj == null)
            {
                state = "Не распознан";
            }
            else
            {
                switch (await obj.GetCurrentState(null))
                {
                    case RelationTypes.Bound:
                    case RelationTypes.Pending:
                        state = "Распознан";
                        break;
                    case RelationTypes.Invalid:
                    case RelationTypes.UnboundInvalid:
                        state = "Не найден/Не указан";
                        break;
                    case RelationTypes.Modified:
                        state = "???";
                        break;
                }
            }
            state = "[" + state + "]";
            return state + string.Join("", Enumerable.Repeat(" ", 30 - state.Length));
        }


        List<string> accumulator = new List<string>();
        accumulator.Add(await FormatTag(_subjectPart) + "Субъект: " + (_subjectPart == null ? "Не указан/Не обнаружен" : _subjectPart.LongTypedName));
        accumulator.Add(await FormatTag(_districtPart) + "Муниципалитет верхнего уровня: " + (_districtPart == null ? "Не указан/Не обнаружен" : _districtPart.LongTypedName));
        accumulator.Add(await FormatTag(_settlementAreaPart) + "Муниципальное поселение: " + (_settlementAreaPart == null ? "Не указано/Не обнаружено" : _settlementAreaPart.LongTypedName));
        accumulator.Add(await FormatTag(_settlementPart) + "Населенный пункт: " + (_settlementPart == null ? "Не указан/Не обнаружен" : _settlementPart.LongTypedName));
        accumulator.Add(await FormatTag(_streetPart) + "Объект дорожной инфраструктуры: " + (_streetPart == null ? "Не указан/Не обнаружен" : _streetPart.LongTypedName));
        accumulator.Add(await FormatTag(_buildingPart) + "Дом: " + (_buildingPart == null ? "Не указан/Не обнаружен" : _buildingPart.LongTypedName));
        accumulator.Add(await FormatTag(_apartmentPart) + "Квартира: " + (_apartmentPart == null ? "Не указана/Не обнаружена" : _apartmentPart.LongTypedName));
        return string.Join("<br/>", accumulator);
    }


    public string FullAddressString
    {
        get
        {
            if (CheckErrorsExist())
            {
                return "";
            }
            else
            {
                return CreateAddressString(
                    _subjectPart,
                    _districtPart,
                    _settlementAreaPart,
                    _settlementPart,
                    _streetPart,
                    _buildingPart,
                    _apartmentPart);
            }
        }
    }
    public AddressModel()
    {
        RegisterProperty(nameof(SubjectPart));
        RegisterProperty(nameof(DistrictPart));
        RegisterProperty(nameof(SettlementAreaPart));
        RegisterProperty(nameof(SettlementPart));
        RegisterProperty(nameof(StreetPart));
        RegisterProperty(nameof(BuildingPart));
        RegisterProperty(nameof(ApartmentPart));

        _id = Utils.INVALID_ID;
    }

    private AddressModel(int id) : base(RelationTypes.Bound)
    {
        _id = id;
    }
    public static string CreateAddressString(FederalSubject? f, District? d, SettlementArea? sa, Settlement? s, Street? st, Building? b, Apartment? a)
    {
        List<string> parts = new List<string>();
        if (f != null)
        {   
            parts.Add(f.Code+ " " + f.LongTypedName);
        }
        if (d != null)
        {
            parts.Add(d.LongTypedName);
        }
        if (sa != null)
        {
            parts.Add(sa.LongTypedName);
        }
        if (s != null)
        {
            parts.Add(s.LongTypedName);
        }
        if (st != null)
        {
            parts.Add(st.LongTypedName);
        }
        if (b != null)
        {
            parts.Add(b.LongTypedName);
        }
        if (a != null)
        {
            parts.Add(a.LongTypedName);
        }
        return string.Join(", ", parts);
    }

    public static async Task<AddressModel?> GetAddressById(int id)
    {
        await using var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT * FROM addresses WHERE id = @p1";
        await using var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!reader.HasRows)
        {
            return null;
        }        
        await reader.ReadAsync();
        AddressModel built = new AddressModel(id);
        if (reader["apartment"].GetType() != typeof(DBNull))
        {
            built._apartmentPart = await Apartment.GetById((int)reader["apartment"], null);
            if (built._apartmentPart != null)
            {
                built._buildingPart = await Building.GetById(built._apartmentPart.ParentBuildingId, null);
            }
        }
        built._buildingPart = await Building.GetById((int)reader["building"], null);
        if (built._buildingPart != null)
        {
            built._streetPart = await Street.GetById(built._buildingPart.StreetParentId, null);
        }
        if (built._streetPart != null)
        {
            built._settlementPart = await Settlement.GetById(built._streetPart.SettlementParentId, null);
        }
        if (built._settlementPart != null)
        {
            if (built._settlementPart.SettlementAreaParentId != null)
            {
                built._settlementAreaPart = await SettlementArea.GetById((int)built._settlementPart.SettlementAreaParentId, null);
            }
            else if (built._settlementPart.DistrictParentId != null)
            {
                built._districtPart = await District.GetById((int)built._settlementPart.DistrictParentId, null);
            }
        }
        if (built._districtPart != null)
        {
            built._subjectPart = await FederalSubject.GetByCode(built._districtPart.SubjectParentId, null);
        }
        return built;
    }

    public static async Task<List<string>?> GetNextSuggestions(string request)
    {
        var builder = new AddressRestoreQueryBuilder();
        var split = request.Split(',');
        if (split.Length > 5)
        {
            return null;
        }
        var toFound = split.Last();
        int maxCount = 50;
        var suggestions = new List<string>();

        DbValidatedObject? parsed = FederalSubject.BuildByName(toFound);
        if (parsed != null)
        {
            return await builder.SelectFrom(parsed, maxCount);
        }
        parsed = District.BuildByName(toFound);
        if (parsed != null)
        {
            return await builder.SelectFrom(parsed, maxCount);
        }
        parsed = SettlementArea.BuildByName(toFound);
        if (parsed != null)
        {
            return await builder.SelectFrom(parsed, maxCount);
        }
        parsed = Settlement.BuildByName(toFound);
        if (parsed != null)
        {
            return await builder.SelectFrom(parsed, maxCount);
        }
        parsed = Street.BuildByName(toFound);
        if (parsed != null)
        {
            return await builder.SelectFrom(parsed, maxCount);
        }
        return await builder.SearchUntyped(toFound, maxCount);
    }

    public static Result<AddressModel?> Create(AddressDTO? addressDTO)
    {
        if (addressDTO is null){
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
        if (parts.Length < 4){
            return Result<AddressModel>.Failure(new ValidationError(nameof(AddressModel), "Адрес имеет недостаточное число частей"));
        }
        try{
            ProcessSubject(0);
        }
        catch (IndexOutOfRangeException e){
            errors = errors.Append(new ValidationError(nameof(AddressModel), "Адрес содержит не все необходимые части"));
        }
        if (errors.Any()){
            return Result<AddressModel>.Failure(errors);
        }
        return Result<AddressModel>.Success(built);

        IEnumerable<ValidationError> ProcessSubject(int pointer)
        {
            var result = FederalSubject.Create(parts[pointer]);
            if (result.IsSuccess){
                built._subjectPart = result.ResultObject;
                var err = ProcessDistrict(pointer + 1);
                return err;
            }
            else{
                return result.Errors;
            }
        }

        IEnumerable<ValidationError> ProcessDistrict(int pointer)
        {
            var result = District.Create(parts[pointer], built._subjectPart);
            if (result.IsSuccess){
                built._districtPart = result.ResultObject;
                var err = ProcessSettlementArea(pointer + 1);
                if (err.Any()){
                    err = ProcessSettlement(pointer + 1);
                    return err;
                }
                return err;
            }
            else{
                return result.Errors;
            }
        }

        IEnumerable<ValidationError> ProcessSettlementArea(int pointer)
        {
            var result = SettlementArea.Create(parts[pointer], built._districtPart);
            if (result.IsSuccess){
                built._settlementAreaPart = result.ResultObject;
                var err = ProcessSettlement(pointer + 1);
                return err;
            }
            else{
                return result.Errors;
            }
        }

        IEnumerable<ValidationError> ProcessSettlement(int pointer)
        {
            var result = Settlement.Create(parts[pointer], built._districtPart);
            if (result.IsSuccess){
                built._settlementPart = result.ResultObject;
                var err = ProcessStreet(pointer + 1);
                return err;
            }
            else{
                result = Settlement.Create(parts[pointer], built._settlementAreaPart);
                if (result.IsSuccess){
                    built._settlementPart = result.ResultObject;
                    var err = ProcessStreet(pointer + 1);
                    return err;
                }
                return result.Errors;
            }
        }

        IEnumerable<ValidationError> ProcessStreet(int pointer)
        {
            var result = Street.Create(parts[pointer], built._settlementPart);
            if (result.IsSuccess){
                built._streetPart = result.ResultObject;
                var err = ProcessBuilding(pointer + 1);
                return err;
            }
            else{
                return result.Errors;
            }
        }
        IEnumerable<ValidationError> ProcessBuilding(int pointer)
        {
            var result = Building.Create(parts[pointer], built._streetPart);
            if (result.IsSuccess){
                built._buildingPart = result.ResultObject;
                var err = ProcessApartment(pointer + 1);
                return err;
            }
            else{
                return result.Errors;
            }
        }
        IEnumerable<ValidationError> ProcessApartment(int pointer)
        {
            if (pointer == parts.Length){
                return new List<ValidationError>();
            }
            var result = Apartment.Create(parts[pointer], built._buildingPart);
            if (result.IsSuccess){
                built._apartmentPart = result.ResultObject;
                var err = ProcessBuilding(pointer + 1);
                return err;
            }
            else{
                return result.Errors;
            }
        }
    }

    public async Task<ResultWithoutValue> Save(ObservableTransaction scope)
    {
        if (_apartmentPart is not null){
            await _apartmentPart.Save(scope);
            return ResultWithoutValue.Success();
        }
        else if (_buildingPart is not null){
            await _buildingPart.Save(scope);
            return ResultWithoutValue.Success();
        }
        else {
            return ResultWithoutValue.Failure(new ValidationError(nameof(AddressModel), "Адрес не может быть сохранен"));
        }
    }

    public override bool Equals(object? other)
    {
        if (other == null)
        {
            return false;
        }
        if (this.GetType() != other.GetType())
        {
            return false;
        }
        else
        {
            return _id == ((AddressModel)other)._id;
        }
    }

    public static AddressRecord? GetRecordById(int id){
        // написать рекурсивный запрос поиска 
        return new AddressRecord();
    }

    public static IEnumerable<AddressRecord> FindRecords(string name, int type, int level){
        var found = new List<AddressRecord>();
        return found;

    }
    public static IEnumerable<AddressRecord> FindRecords(int parentId, string name, int type, int level){
        var found = new List<AddressRecord>();
        return found;

    }

    public static async Task<int> SaveRecord(IAddressRecord address, ObservableTransaction? transaction = null){
        using var conn = await Utils.GetAndOpenConnectionFactory();
        string commandText = "INSERT INTO address_hierarchy (parent_id, address_level, toponym_type, address_name) " +
        "VALUES (@p1,@p2,@p3,@p4) RETURNING address_part_id";
        NpgsqlCommand cmd;
        if (transaction is null){
            cmd = new NpgsqlCommand(commandText, conn);
        } 
        else {
            cmd = new NpgsqlCommand(commandText, transaction.Connection, transaction.Transaction);
        }
        var record = address.ToAddressRecord();
        cmd.Parameters.Add(new NpgsqlParameter("p1", record.ParentId is null ? DBNull.Value : (int)record.ParentId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p2", record.AddressLevelCode));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p3", record.ToponymType));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p4", record.AddressName));
        using (cmd){
            using var reader = cmd.ExecuteReader();
            reader.Read();
            return (int)reader["address_part_id"];
        }
    } 
}



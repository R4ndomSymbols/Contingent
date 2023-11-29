namespace StudentTracking.Models.Domain.Address;

using Npgsql;
using StudentTracking.Models.JSON;
using StudentTracking.Models.SQL;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Utilities;
using Utilities.Validation;

public class AddressModel : DbValidatedObject
{

    private int _id;
    private FederalSubject? _subjectPart;
    private District? _districtPart;
    private SettlementArea? _settlementAreaPart;
    private Settlement? _settlementPart;
    private Street? _streetPart;
    private Building? _buildingPart;
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
            parts.Add(f.LongTypedName);
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

        using (var conn = Utils.GetAndOpenConnectionFactory())
        {
            // получение всех сущностей

            using (var cmd = new NpgsqlCommand($"SELECT * FROM addresses WHERE id = @p1", await conn)
            {
                Parameters = {
                    new ("p1", id)
                }
            })
            {
                using var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }

                reader.Read();
                AddressModel built = new AddressModel(id);

                if (reader["apartment"].GetType() != typeof(DBNull))
                {
                    built._apartmentPart = await Apartment.GetById((int)reader["apartment"], null);
                    if (built._apartmentPart != null)
                    {
                        built._buildingPart = await Building.GetById(built._apartmentPart.ParentBuildingId, null);
                    }
                }
                else
                {
                    built._buildingPart = await Building.GetById((int)reader["building"], null);
                }
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
                    if (built._settlementPart.DistrictParentId != null)
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
        }
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

    public static AddressModel? BuildFromString(string? address)
    {
        if (address == null)
        {
            return null;
        }
        else
        {
            AddressModel built = new AddressModel();
            string[] parts = address.Split(',').Select(x => x.Trim()).ToArray();
            ProcessSubject(0);
            return built;

            void ProcessSubject(int position)
            {
                built.SubjectPart =  position >= parts.Length ? "" : parts[position];
                if (built._subjectPart?.CheckErrorsExist() ?? true)
                {
                    return;
                }
                ProcessDistrict(++position);
            }

            void ProcessDistrict(int position)
            {

                built.DistrictPart =  position >= parts.Length ? "" : parts[position];
                if (built._districtPart?.CheckErrorsExist() ?? true)
                {
                    return;
                }
                ProcessSettlementArea(++position);
            }

            void ProcessSettlementArea(int position)
            {

                built.SettlementAreaPart =  position >= parts.Length ? "" : parts[position];
                if (built._settlementAreaPart?.CheckErrorsExist() ?? true)
                {
                    ProcessSettlement(position); 
                }
                else{
                    ProcessSettlement(++position); 
                }
            }

            void ProcessSettlement(int position)
            {

                built.SettlementPart =  position >= parts.Length ? "" : parts[position];
                if (built._settlementPart?.CheckErrorsExist() ?? true)
                {
                    built.PerformValidation(() => true, new ValidationError(nameof(SettlementPart), "Очистка статуса"));
                    if (built.FilterErrorsByName(nameof(SettlementAreaPart)).Any()){
                        built.AddError(new ValidationError(nameof(SettlementAreaPart), "Ни муниципалитет верхнего уровня, ни населенный пункт верно не указаны"));
                        return;
                    }
                }
                built.PerformValidation(() => true, new ValidationError(nameof(SettlementAreaPart), "Очистка статуса"));
                ProcessStreet(++position);
                return;
            }

            void ProcessStreet(int position)
            {

                built.StreetPart =  position >= parts.Length ? "" : parts[position];
                if (built._streetPart?.CheckErrorsExist() ?? true)
                {   
                    return;
                }
                ProcessBuilding(++position);
            }
            void ProcessBuilding(int position)
            {

                built.BuildingPart =  position >= parts.Length ? "" : parts[position];
                if (built._buildingPart?.CheckErrorsExist() ?? true)
                {
                    return;
                }
                ProcessApartment(++position);
            }
            void ProcessApartment(int position)
            {
                if (position >= parts.Length){
                    return;
                }
                built.ApartmentPart = parts[position];
                if (built._apartmentPart?.CheckErrorsExist() ?? true)
                {
                    built.AddError(new ValidationError(nameof(ApartmentPart), "Квартира указана некорректно"));
                    return;
                }
            }
        }
    }



    public async Task<bool> Save(bool commit, Action? beforeRollbackAction, Action? onFailureAction)
    {

        if (CheckErrorsExist())
        {
            onFailureAction?.Invoke();
            return false;
        }
        else
        {
            if (_subjectPart == null || _districtPart == null || _settlementPart == null ||
                _streetPart == null || _buildingPart == null)
            {
                onFailureAction?.Invoke();
                return false;
            }
            var transactConn = await Utils.GetAndOpenConnectionFactory();
            var scope = await transactConn.BeginTransactionAsync();
            ObservableTransaction wrapper = new ObservableTransaction(scope, transactConn);

            await _subjectPart.Save(wrapper);
            if (await _subjectPart.GetCurrentState(wrapper) == RelationTypes.Bound)
            {
                await _districtPart.SetSubjectParent(int.Parse(_subjectPart.Code), wrapper);
            }
            await _districtPart.Save(wrapper);
            if (await _districtPart.GetCurrentState(wrapper) == RelationTypes.Bound)
            {
                if (_settlementAreaPart != null)
                {
                    await _settlementAreaPart.SetDistrictParent(_districtPart.Id, wrapper);
                    await _settlementAreaPart.Save(wrapper);
                    if (await _settlementAreaPart.GetCurrentState(wrapper) == RelationTypes.Bound)
                    { 
                        await _settlementPart.SetSettlementAreaParent(_settlementAreaPart.Id, wrapper);
                        
                    }
                }
                else
                {
                    await _settlementPart.SetDistrictParent(_districtPart.Id, wrapper);
                }
            }
            await _settlementPart.Save(wrapper);

            if (await _settlementPart.GetCurrentState(wrapper) == RelationTypes.Bound)
            {
                await _streetPart.SetSettlementParent(_settlementPart.Id, wrapper);
            }
            await _streetPart.Save(wrapper);

            if (await _streetPart.GetCurrentState(wrapper) == RelationTypes.Bound)
            {
                await _buildingPart.SetParentStreet(_streetPart.Id, wrapper);
            }
            bool completed = false;
            await _buildingPart.Save(wrapper);
            if (await _buildingPart.GetCurrentState(wrapper) == RelationTypes.Bound)
            {
                if (_apartmentPart != null)
                {
                    await _apartmentPart.SetParentBuildingId(_buildingPart.Id, wrapper);
                    await _apartmentPart.Save(wrapper);
                    completed = await _apartmentPart.GetCurrentState(wrapper) == RelationTypes.Bound;
                }
                else
                {
                    completed = true;
                }
            }
            

            Console.WriteLine("1 " + _subjectPart?.LongTypedName + " " + _subjectPart?.GetCurrentState(wrapper)?.Result.ToString());
            Console.WriteLine("2 " + _districtPart?.LongTypedName + "|" + _districtPart?.Id + " " + _districtPart?.SubjectParentId + " " + _districtPart?.GetCurrentState(wrapper)?.Result.ToString());
            Console.WriteLine("3 " + _settlementAreaPart?.LongTypedName + "|" + _settlementAreaPart?.Id + " " + _settlementAreaPart?.DistrictParentId + " " + _settlementAreaPart?.GetCurrentState(wrapper)?.Result.ToString());
            Console.WriteLine("4 " + _settlementPart?.LongTypedName + "|" + _settlementPart?.Id + " " + _settlementPart?.DistrictParentId + " " + _settlementPart?.SettlementAreaParentId + " " + _settlementPart?.GetCurrentState(wrapper)?.Result.ToString());
            Console.WriteLine("5 " + _streetPart?.LongTypedName + " " + _streetPart?.SettlementParentId + " " + _streetPart?.GetCurrentState(wrapper)?.Result.ToString());
            Console.WriteLine("6 " + _buildingPart?.LongTypedName + " " + _buildingPart?.StreetParentId + " " + _buildingPart?.GetCurrentState(wrapper)?.Result.ToString());
            Console.WriteLine("7 " + _apartmentPart?.LongTypedName + " " + _apartmentPart?.ParentBuildingId + " " + _apartmentPart?.GetCurrentState(wrapper)?.Result.ToString());
            
            using (transactConn)
            using (scope)
            {
                if (completed)
                {
                    if (commit)
                    {
                        string insertText = "INSERT INTO addresses (building, apartment) " +
                            " VALUES (@p1, @p2) RETURNING id";
                        var saveCmd = new NpgsqlCommand(insertText, scope.Connection, scope);

                        saveCmd.Parameters.Add(new NpgsqlParameter<int>("p1", _buildingPart.Id));
                        saveCmd.Parameters.Add(new NpgsqlParameter("p2", _apartmentPart == null ? DBNull.Value : (int)_apartmentPart.Id));

                        await using (var reader = await saveCmd.ExecuteReaderAsync()){
                            await reader.ReadAsync();
                            _id = (int)reader["id"]; ;
                        };
                        await wrapper.CommitAsync();
                        NotifyStateChanged();
                        return true;

                    }
                    await wrapper.RollbackAsync();
                }
                if (beforeRollbackAction != null)
                {
                    onFailureAction?.Invoke();
                }
                return false;
            }

        }
    }

    public async static Task<bool> IsIdExists(int? id, ObservableTransaction? scope)
    {
        if (id == null)
        {
            return false;
        }
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            await using (var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT id FROM addresses WHERE id = @p1)", conn))
            {
                cmd.Parameters.Add(new NpgsqlParameter<int>("p1", (int)id));
                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                return (bool)reader["exists"];
            }
        }
    }

    public override bool Equals(IDbObjectValidated? other)
    {
        if (other == null){
            return false;
        }
        if (this.GetType() != other.GetType()){
            return false;
        }
        else {
            return _id == ((AddressModel)other)._id;
        }
    }

    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? stateWithin)
    {
        if (await IsIdExists(_id, null)){
            return this;
        }
        else{
            return null;
        }
    }
}



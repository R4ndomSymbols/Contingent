namespace StudentTracking.Models.Domain.Address;

using Npgsql;
using StudentTracking.Models.JSON;
using StudentTracking.Models.SQL;
using System.Data;
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

public class AddressModel : ValidatedObject
{

    private int _id;
    private FederalSubject? _subjectPart;
    private District? _districtPart;
    private SettlementArea? _settlementAreaPart;
    private Settlement? _settlementPart;
    private Street? _streetPart;
    private Building? _buildingPart;
    private Apartment? _apartmentPart;

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
                () =>
                {
                    if (_districtPart == null)
                    {
                        return false;
                    }
                    else if ((_districtPart.DistrictType == (int)District.Types.CityTerritory ||
                             _districtPart.DistrictType == (int)District.Types.MunicipalTerritory) && value == null)
                    {
                        return true;
                    }
                    else
                    {
                        var parsed = SettlementArea.BuildByName(value);
                        return !(parsed == null || parsed.CheckErrorsExist());
                    }
                }, new ValidationError(nameof(SettlementAreaPart), "Поселение не указано, указано неверно либо не соотнесено с районом")
            ))
            {
                if (_districtPart == null)
                {
                    return;
                }
                _settlementAreaPart = SettlementArea.BuildByName(value);
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
                    try
                    {
                        var parsed = Settlement.BuildByName(value);
                        return !(parsed == null || parsed.CheckErrorsExist());
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
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
                if (_settlementPart != null)
                {
                    _streetPart = Street.BuildByName(value);
                }
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
                if (_streetPart != null)
                {
                    _buildingPart = Building.BuildByName(value);
                }
            }
        }
    }
    private string ApartmentPart
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
                    else
                    {
                        var parsed = Apartment.BuildByName(value);
                        return !(parsed == null || parsed.CheckErrorsExist());
                    }
                }, new ValidationError(nameof(ApartmentPart), "Квартира указана неверно либо не определен предшествующий адрес")
            ))
            {
                if (_buildingPart != null)
                {
                    _apartmentPart = Apartment.BuildByName(value);
                }
            }
        }
    }
    [JsonIgnore]
    public string AboutAddress
    {
        get
        {
            List<string> accumulator = new List<string>();
            accumulator.Add("Субъект: " + (_subjectPart == null ? "Не указан/Не обнаружен" : _subjectPart.LongTypedName));
            accumulator.Add("Муниципалитет верхнего уровня: " + (_districtPart == null ? "Не указан/Не обнаружен" : _districtPart.LongTypedName));
            accumulator.Add("Муниципальное поселение: " + (_settlementAreaPart == null ? "Не указано/Не обнаружено" : _settlementAreaPart.LongTypedName));
            accumulator.Add("Населенный пункт: " + (_settlementPart == null ? "Не указан/Не обнаружен" : _settlementPart.LongTypedName));
            accumulator.Add("Объект дорожной инфраструктуры: " + (_streetPart == null ? "Не указан/Не обнаружен" : _streetPart.LongTypedName));
            accumulator.Add("Дом: " + (_buildingPart == null ? "Не указан/Не обнаружен" : _buildingPart.LongTypedName));
            accumulator.Add("Квартира: " + (_apartmentPart == null ? "Не указана/Не обнаружена" : _apartmentPart.LongTypedName));
            return string.Join("<br/>", accumulator);
        }


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
    public int Id
    {
        get => _id;
    }

    private AddressModel(FederalSubject f, District d, SettlementArea? sa, Settlement s, Street st, Building b, Apartment? a)
    {
        _subjectPart = f;
        _districtPart = d;
        _settlementAreaPart = sa;
        _settlementPart = s;
        _streetPart = st;
        _buildingPart = b;
        _apartmentPart = a;
    }
    public AddressModel()
    {
        _id = Utils.INVALID_ID;
        AddError(new ValidationError(nameof(SubjectPart), "Субъект федерации должен быть указан"));
        AddError(new ValidationError(nameof(DistrictPart), "Муниципальное образование верхнего уровня должно быть указано"));
        AddError(new ValidationError(nameof(SettlementPart), "Населенный пункт должен быть указан"));
        AddError(new ValidationError(nameof(StreetPart), "Объект дорожной инфраструктуры должен быть указан"));
        AddError(new ValidationError(nameof(BuildingPart), "Дом должен быть указан"));

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


    public static string? GetAddressNameById(int id)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            // получение всех сущностей


            using (var cmd = new NpgsqlCommand($"SELECT * FROM get_full_address_by_id({id})", conn))
            {
                var reader = cmd.ExecuteReader();
                reader.Read();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
        }
    }

    public static AddressModel? GetAddressById(int id)
    {

        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            // получение всех сущностей

            using (var cmd = new NpgsqlCommand($"SELECT * FROM addresses WHERE id = @p1", conn)
            {
                Parameters = {
                    new ("p1", id)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                reader.Read();

                FederalSubject? subject = null;
                District? district = null;
                SettlementArea? settlementArea = null;
                Settlement? settlement = null;
                Street? street = null;
                Building? building = null;
                Apartment? apartment = null;

                if (reader["apartment"].GetType() != typeof(DBNull))
                {
                    apartment = Apartment.GetById((int)reader["apartment"]);
                    if (apartment != null)
                    {
                        building = Building.GetById(apartment.ParentBuildingId);
                    }
                }
                else
                {
                    building = Building.GetById((int)reader["building"]);
                }
                if (building != null)
                {
                    street = Street.GetById(building.StreetParentId);
                }
                if (street != null)
                {
                    settlement = Settlement.GetById(street.SettlementParentId);
                }
                if (settlement != null)
                {
                    if (settlement.SettlementAreaParentId != null)
                    {
                        settlementArea = SettlementArea.GetById((int)settlement.SettlementAreaParentId);
                    }
                    if (settlement.DistrictParentId != null)
                    {
                        district = District.GetById((int)settlement.DistrictParentId);
                    }
                }
                if (district != null)
                {
                    subject = FederalSubject.GetByCode(district.SubjectParentId);
                }
                if (subject == null || district == null || settlement == null || street == null || building == null)
                {
                    return null;
                }
                else
                {
                    return new AddressModel(subject, district, settlementArea, settlement, street, building, apartment);
                }
            }
        }
    }

    public static List<string> GetNextSuggestions(string toFound)
    {
        var builder = new AddressRestoreQueryBuilder();
        int maxCount = 50;
        var suggestions = new List<string>();
        var tmp = builder.SelectFrom(FederalSubject.BuildByName(toFound), toFound, maxCount);
        if (tmp != null)
        {
            maxCount -= tmp.Count;
            suggestions.AddRange(tmp);
        }
        tmp = builder.SelectFrom(District.BuildByName(toFound), toFound, maxCount);
        if (tmp != null)
        {
            maxCount -= tmp.Count;
            suggestions.AddRange(tmp);
        }
        tmp = builder.SelectFrom(SettlementArea.BuildByName(toFound), toFound, maxCount);
        if (tmp != null)
        {
            maxCount -= tmp.Count;
            suggestions.AddRange(tmp);
        }
        tmp = builder.SelectFrom(Settlement.BuildByName(toFound), toFound, maxCount);
        if (tmp != null)
        {
            maxCount -= tmp.Count;
            suggestions.AddRange(tmp);
        }
        tmp = builder.SelectFrom(Street.BuildByName(toFound), toFound, maxCount);
        if (tmp != null)
        {
            suggestions.AddRange(tmp);
        }
        tmp = builder.SearchUntyped(toFound, maxCount);
        if (tmp != null)
        {
            suggestions.AddRange(tmp);
        }
        return suggestions;
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
            if (parts.Length < 5 || parts.Length > 7)
            {
                built.AddError(new ValidationError(nameof(AddressModel), "Неверное количество частей адреса"));
            }
            else
            {
                int offset = 0;
                built.SubjectPart = parts[0];
                built.DistrictPart = parts[1];
                built.SettlementAreaPart = parts[2];
                if (built._settlementAreaPart != null)
                {
                    offset += 1;
                }
                built.SettlementPart = parts[2 + offset];
                built.StreetPart = parts[3 + offset];
                built.BuildingPart = parts[4 + offset];
                if (parts.Length >= 6)
                {
                    built.ApartmentPart = parts[5 + offset];
                }
            }
            return built;
        }
    }


    public bool Save()
    {
        if (CheckErrorsExist())
        {
            return false;
        }
        else
        {
            if (_subjectPart == null || _districtPart == null || _settlementPart == null ||
                _streetPart == null || _buildingPart == null)
            {
                return false;
            }

            if (!_subjectPart.Save())
            {
                return false;
            }
            _districtPart.SubjectParentId = int.Parse(_subjectPart.Code);
            if (!_districtPart.Save())
            {
                return false;
            }
            int? saId = null;
            if (_settlementAreaPart != null)
            {
                _settlementAreaPart.DistrictParentId = _districtPart.Id;
                if (!_settlementAreaPart.Save())
                {
                    return false;
                }
                saId = _settlementAreaPart.Id;
            }

            if (saId != null)
            {
                _settlementPart.SettlementAreaParentId = saId;
            }
            else
            {
                _settlementPart.DistrictParentId = _districtPart.Id;
            }
            if (!_settlementPart.Save())
            {
                return false;
            }
            _streetPart.SettlementParentId = _streetPart.Id;
            if (!_streetPart.Save())
            {
                return false;
            }
            _buildingPart.StreetParentId = _streetPart.Id;
            if (!_buildingPart.Save())
            {
                return false;
            }
            if (_apartmentPart != null)
            {
                _apartmentPart.ParentBuildingId = _buildingPart.Id;
                if (!_apartmentPart.Save())
                {
                    return false;
                }
            }
            using (var conn = Utils.GetConnectionFactory())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("INSERT INTO addresses (building, apartment) VALUES (@p1, @p2) RETURNING id", conn)
                {
                    Parameters = {
                        new ("p1", _buildingPart.Id),
                        new ("p2", _apartmentPart == null ? DBNull.Value : _apartmentPart.Id)
                    }
                })
                {
                    var reader = cmd.ExecuteReader();
                    _id = (int)reader["id"];
                    return true;
                }
            }
        }
    }
}



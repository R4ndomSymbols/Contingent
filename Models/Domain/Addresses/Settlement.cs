using System.Collections.ObjectModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;

public class Settlement : IAddressPart
{
    public const int ADDRESS_LEVEL = 4;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"город"),
        new Regex(@"поселок"),
        new Regex(@"село\s"),
        new Regex(@"деревня"),
    };
    public static readonly IReadOnlyDictionary<SettlementTypes, NameFormatting> Names = new Dictionary<SettlementTypes, NameFormatting>(){
        {SettlementTypes.NotMentioned, new NameFormatting("нет", "Не указано", NameFormatting.BEFORE)},
        {SettlementTypes.City, new NameFormatting("г.", "Город", NameFormatting.BEFORE)},
        {SettlementTypes.Town, new NameFormatting("пгт.", "Поселок городского типа", NameFormatting.BEFORE)},
        {SettlementTypes.Village, new NameFormatting("с.", "Село", NameFormatting.BEFORE)},
        {SettlementTypes.SmallVillage, new NameFormatting("д.", "Деревня", NameFormatting.BEFORE)},
        {SettlementTypes.TinyVillage, new NameFormatting("п.", "Поселок", NameFormatting.BEFORE)},
    };
    public enum SettlementTypes
    {
        NotMentioned = -1,
        City = 1, // город
        Town = 2, // поселок городского типа
        Village = 3, // село 
        SmallVillage = 4, // деревня
        TinyVillage = 5, // поселок

    }

    private int _id;
    private SettlementArea? _parentSettlementArea;
    private District? _parentDistrict;
    private string _untypedName;
    private SettlementTypes _settlementType;
    public int Id
    {
        get => _id;
    }
    public SettlementArea? SettlementAreaParentId
    {
        get => _parentSettlementArea;
    }
    public District? DistrictParentId
    {
        get => _parentDistrict;
    }
    public int SettlementType
    {
        get => (int)_settlementType;
    }
    public string UntypedName
    {
        get => Utils.FormatToponymName(_untypedName);
    }
    public string LongTypedName
    {
        get
        {
            return Names[_settlementType].FormatLong(UntypedName);
        }
    }
    // валидация не проверяет типы, внутри которых возможно размещение городов, добавить

    private Settlement(){
        _id = Utils.INVALID_ID;
    } 
    

    protected Settlement(int id)
    {
        _id = id;
    }
    // проверка на тип родителя
    public static Result<Settlement?> Create(string addressPart, District scope){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<Settlement>.Failure(new ValidationError(nameof(Settlement), "Населенный пункт не указан или указан неверно"));
        }

        NameToken? foundSettlement = null;
        SettlementTypes settlementType = SettlementTypes.NotMentioned;  
        foreach (var pair in Names){
            foundSettlement = pair.Value.ExtractToken(addressPart); 
            if (foundSettlement is not null){
                settlementType = pair.Key;
                break;
            }
        }
        if (foundSettlement is null){
            return Result<Settlement>.Failure(new ValidationError(nameof(Settlement), "Населенный пункт не распознан"));
        }
        var fromDb = AddressModel.FindRecords(scope.Id, foundSettlement.Name, (int)settlementType, ADDRESS_LEVEL);
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<Settlement>.Failure(new ValidationError(nameof(Settlement), "Населенный пункт не может быть однозначно распознан"));
            }
            else{
                var first = fromDb.First();
                return Result<Settlement?>.Success(new Settlement(first.AddressPartId){
                    _parentDistrict = scope, 
                    _settlementType = (SettlementTypes)first.ToponymType,
                    _untypedName = first.AddressName
                });
            }
        }
        
        var got = new Settlement(){
            _parentDistrict = scope,
            _settlementType = settlementType,
            _untypedName = foundSettlement.Name,
        };
        return Result<Settlement?>.Success(got);
    }
    // отличия между ними в валидации иерархии, добавить потом
    public static Result<Settlement?> Create(string addressPart, SettlementArea scope){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<Settlement>.Failure(new ValidationError(nameof(Settlement), "Населенный пункт не указан или указан неверно"));
        }

        NameToken? foundSettlement = null;
        SettlementTypes settlementType = SettlementTypes.NotMentioned;  
        foreach (var pair in Names){
            foundSettlement = pair.Value.ExtractToken(addressPart); 
            if (foundSettlement is not null){
                settlementType = pair.Key;
                break;
            }
        }
        if (foundSettlement is null){
            return Result<Settlement>.Failure(new ValidationError(nameof(Settlement), "Населенный пункт не распознан"));
        }
        var fromDb = AddressModel.FindRecords(scope.Id, foundSettlement.Name, (int)settlementType, ADDRESS_LEVEL);
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<Settlement>.Failure(new ValidationError(nameof(Settlement), "Населенный пункт не может быть однозначно распознан"));
            }
            else{
                var first = fromDb.First();
                return Result<Settlement?>.Success(new Settlement(first.AddressPartId){
                    _parentSettlementArea = scope, 
                    _settlementType = (SettlementTypes)first.ToponymType,
                    _untypedName = first.AddressName
                });
            }
        }
        
        var got = new Settlement(){
            _parentSettlementArea = scope,
            _settlementType = settlementType,
            _untypedName = foundSettlement.Name,
        };
        return Result<Settlement?>.Success(got);
    }

    public static Settlement Create(AddressRecord record, District parent){
        return new Settlement(record.AddressPartId){
            _parentSettlementArea = null,
            _parentDistrict = parent,
            _settlementType = (SettlementTypes)record.ToponymType,
            _untypedName = record.AddressName
        };
    }
    public static Settlement Create(AddressRecord record, SettlementArea parent){
        return new Settlement(record.AddressPartId){
            _parentDistrict = null,
            _parentSettlementArea = parent,
            _settlementType = (SettlementTypes)record.ToponymType,
            _untypedName = record.AddressName
        };
    }


    public async Task Save(ObservableTransaction? scope = null)
    {
        if (_parentSettlementArea is not null){
            await _parentSettlementArea.Save(scope);
        }
        if (_parentDistrict is not null){
            await _parentDistrict.Save(scope);
        }
        if (_id == Utils.INVALID_ID){
            _id = await AddressModel.SaveRecord(this, scope);
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (obj.GetType() != typeof(Settlement))
        {
            return false;
        }
        var unboxed = (Settlement)obj;
        return _id == unboxed._id;
    }

    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            AddressPartId = _id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName = _untypedName,
            ParentId = _parentDistrict is null ? _parentSettlementArea.Id : _parentDistrict.Id,
            ToponymType = (int)_settlementType
        };
    }

    public IEnumerable<IAddressPart> GetDescendants()
    {
        var found = AddressModel.FindRecords(_id);
        return found.Select(rec => Street.Create(rec, this));
    }
}

using System.Data;
using System.Text.RegularExpressions;
using Utilities;
namespace StudentTracking.Models.Domain.Address;
public class Settlement : IAddressPart
{
    public const int ADDRESS_LEVEL = 4;
    
    private static List<Settlement> _duplicationBuffer;

    static Settlement(){
        _duplicationBuffer = new List<Settlement>();
    }
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"город"),
        new Regex(@"поселок"),
        new Regex(@"село\s"),
        new Regex(@"деревня"),
    };

    private static IEnumerable<Settlement> GetDuplicates(Settlement settlement){
        return _duplicationBuffer.Where(
            s => 
            s._parentSettlementArea is not null 
                ? s._parentSettlementArea.Equals(settlement._parentSettlementArea)
                : s._parentDistrict.Equals(settlement._parentDistrict)
            && s._settlementName.Equals(settlement._settlementName)
            && s._settlementType == settlement._settlementType
        );
    }
     
    public static readonly IReadOnlyDictionary<SettlementTypes, AddressNameFormatting> Names = new Dictionary<SettlementTypes, AddressNameFormatting>(){
        {SettlementTypes.NotMentioned, new AddressNameFormatting("нет", "Не указано", AddressNameFormatting.BEFORE)},
        {SettlementTypes.City, new AddressNameFormatting("г.", "Город", AddressNameFormatting.BEFORE)},
        {SettlementTypes.Town, new AddressNameFormatting("пгт.", "Поселок городского типа", AddressNameFormatting.BEFORE)},
        {SettlementTypes.Village, new AddressNameFormatting("с.", "Село", AddressNameFormatting.BEFORE)},
        {SettlementTypes.SmallVillage, new AddressNameFormatting("д.", "Деревня", AddressNameFormatting.BEFORE)},
        {SettlementTypes.TinyVillage, new AddressNameFormatting("п.", "Поселок", AddressNameFormatting.BEFORE)},
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
    private AddressNameToken _settlementName;
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

    // валидация не проверяет типы, внутри которых возможно размещение городов, добавить
    private Settlement(District? parentD, SettlementArea? parentSA, SettlementTypes type, AddressNameToken name){
        _id = Utils.INVALID_ID;
        if (parentD is null && parentSA is null
            || parentD is not null && parentSA is not null){
                throw new Exception("Неверная инициализация населенного пункта");
        }
        _parentDistrict = parentD;
        _parentSettlementArea = parentSA;
        _settlementType = type;
        _settlementName = name;
        _duplicationBuffer.Add(this);
    } 
    
    private Settlement(int id, District? parentD, SettlementArea? parentSA, SettlementTypes type, AddressNameToken name)
    {
        _id = id;
        if (parentD is null && parentSA is null
            || parentD is not null && parentSA is not null){
                throw new Exception("Неверная инициализация населенного пункта");
        }
        _parentDistrict = parentD;
        _parentSettlementArea = parentSA;
        _settlementType = type;
        _settlementName = name;
    }

    // проверка на тип родителя
    public static Result<Settlement?> Create(string addressPart, District parent, ObservableTransaction? searchScope = null){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<Settlement>.Failure(new ValidationError(nameof(Settlement), "Населенный пункт не указан или указан неверно"));
        }
        AddressNameToken? foundSettlement = null;
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
        var fromDb = AddressModel.FindRecords(parent.Id, foundSettlement.UnformattedName, (int)settlementType, ADDRESS_LEVEL, searchScope).Result;
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<Settlement>.Failure(new ValidationError(nameof(Settlement), "Населенный пункт не может быть однозначно распознан"));
            }
            else{
                var first = fromDb.First();
                return Result<Settlement?>.Success(new Settlement(
                    first.AddressPartId,
                    parent,
                    null, 
                    (SettlementTypes)first.ToponymType,
                    new AddressNameToken(first.AddressName, Names[(SettlementTypes)first.ToponymType])
                ));
            }
        }
        
        var got = new Settlement(
            parent,
            null,
            settlementType,
            foundSettlement
        );
        return Result<Settlement?>.Success(got);
    }
    // отличия между ними в валидации иерархии, добавить потом
    public static Result<Settlement?> Create(string addressPart, SettlementArea parent, ObservableTransaction? searchScope = null){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<Settlement>.Failure(new ValidationError(nameof(Settlement), "Населенный пункт не указан или указан неверно"));
        }
        AddressNameToken? foundSettlement = null;
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
        var fromDb = AddressModel.FindRecords(parent.Id, foundSettlement.UnformattedName, (int)settlementType, ADDRESS_LEVEL, searchScope).Result;
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<Settlement>.Failure(new ValidationError(nameof(Settlement), "Населенный пункт не может быть однозначно распознан"));
            }
            else{
                var first = fromDb.First();
                return Result<Settlement?>.Success(new Settlement(first.AddressPartId,
                    null,
                    parent, 
                    (SettlementTypes)first.ToponymType,
                    new AddressNameToken(first.AddressName, Names[(SettlementTypes)first.ToponymType])
                ));
            }
        }
        
        var got = new Settlement(
            null,
            parent,
            settlementType,
            foundSettlement
        );
        return Result<Settlement?>.Success(got);
    }
    public static Settlement? Create(AddressRecord record, District parent){
        if (record.AddressLevelCode != ADDRESS_LEVEL || parent is null){
            return null;
        }
        return new Settlement(
            record.AddressPartId,
            parent,
            null,
            (SettlementTypes)record.ToponymType,
            new AddressNameToken(record.AddressName, Names[(SettlementTypes)record.ToponymType])
        );
    }
    public static Settlement? Create(AddressRecord record, SettlementArea parent){
        if (record.AddressLevelCode != ADDRESS_LEVEL || parent is null){
            return null;
        }
        return new Settlement(
            record.AddressPartId,
            null,
            parent,
            (SettlementTypes)record.ToponymType,
            new AddressNameToken(record.AddressName, Names[(SettlementTypes)record.ToponymType])
        );
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
        var duplicates = GetDuplicates(this);
        foreach(var d in duplicates){
            d._id = this._id;
        }
        _duplicationBuffer.RemoveAll(d => d._id == this._id);

    }
    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            AddressPartId = _id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName = _settlementName.UnformattedName,
            ParentId = _parentDistrict is null ? _parentSettlementArea.Id : _parentDistrict.Id,
            ToponymType = (int)_settlementType
        };
    }
    public IEnumerable<IAddressPart> GetDescendants()
    {
        var found = AddressModel.FindRecords(_id).Result;
        return found.Select(rec => Street.Create(rec, this));
    }

    public override string ToString()
    {
        return _settlementName.FormattedName;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != typeof(Settlement)){
            return false;
        }
        var toCompare = (Settlement)obj;
        return toCompare._id == this._id;
    }
}

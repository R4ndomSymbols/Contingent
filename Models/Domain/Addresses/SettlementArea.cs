using System.Text.RegularExpressions;
using Utilities;
namespace StudentTracking.Models.Domain.Address;
public class SettlementArea : IAddressPart
{
    public const int ADDRESS_LEVEL = 3;

    private static List<SettlementArea> _duplicationBuffer;
    static SettlementArea(){
        _duplicationBuffer = new List<SettlementArea>();
    }
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"поселение", RegexOptions.IgnoreCase),
    };
    public static readonly IReadOnlyDictionary<SettlementAreaTypes, AddressNameFormatting> Names = new Dictionary<SettlementAreaTypes, AddressNameFormatting>(){
        {SettlementAreaTypes.CitySettlement, new AddressNameFormatting("г.п.", "Городское поселение", AddressNameFormatting.BEFORE)},
        {SettlementAreaTypes.CountysideDistrict, new AddressNameFormatting("с.п.", "Сельское поселение", AddressNameFormatting.BEFORE)},
    };
    public enum SettlementAreaTypes
    {
        NotMentioned = -1,
        CitySettlement = 1, // городской округ
        CountysideDistrict = 2, // муниципальный район
    }
    private int _id;
    private District _parentDistrict;
    private AddressNameToken _settlementAreaName;
    private SettlementAreaTypes _settlementAreaType;
    public int Id
    {
        get => _id;
    }
    public District DistrictParentId
    {
        get => _parentDistrict;
    }

    private SettlementArea(int id, District parent, SettlementAreaTypes type, AddressNameToken name)
    {
        _id = id;
        _parentDistrict = parent;
        _settlementAreaType = type;
        _settlementAreaName = name;
    }
    
    private SettlementArea(District parent, SettlementAreaTypes type, AddressNameToken name)
    {
        _id = Utils.INVALID_ID;
        _parentDistrict = parent;
        _settlementAreaType = type;
        _settlementAreaName = name;
        _duplicationBuffer.Add(this);
    }   

    private static IEnumerable<SettlementArea> GetDuplicates(SettlementArea area){
        return _duplicationBuffer.Where(
            a => a._parentDistrict.Equals(area._parentDistrict)
            && a._settlementAreaName.Equals(area._settlementAreaName)
            && a._settlementAreaType == area._settlementAreaType
        );
    }
    // добавить ограничение на создание 
    // исходя из типа родителя
    public static Result<SettlementArea?> Create(string addressPart, District parent, ObservableTransaction? searchScope = null){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<SettlementArea>.Failure(new ValidationError(nameof(SettlementArea), "Поселение указано неверно"));
        }
        AddressNameToken? foundSettlementArea = null;
        SettlementAreaTypes settlementAreaType = SettlementAreaTypes.NotMentioned;  
        foreach (var pair in Names){
            foundSettlementArea = pair.Value.ExtractToken(addressPart, Restrictions); 
            if (foundSettlementArea is not null){
                settlementAreaType = pair.Key;
                break;
            }
        }
        if (foundSettlementArea is null){
            return Result<SettlementArea>.Failure(new ValidationError(nameof(SettlementArea), "Поселение не распознано"));
        }
        var fromDb = AddressModel.FindRecords(parent.Id, foundSettlementArea.UnformattedName, (int)settlementAreaType, ADDRESS_LEVEL, searchScope).Result;
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<SettlementArea>.Failure(new ValidationError(nameof(SettlementArea), "Поселение не может быть однозначно распознано"));
            }
            else{
                var first = fromDb.First();
                return Result<SettlementArea?>.Success(new SettlementArea(first.AddressPartId,
                    parent, 
                    (SettlementAreaTypes)first.ToponymType,
                    new AddressNameToken(first.AddressName, Names[(SettlementAreaTypes)first.ToponymType])
                ));
            }
        }
        
        var got = new SettlementArea(
            parent,
            settlementAreaType,
            foundSettlementArea
        );
        return Result<SettlementArea?>.Success(got);
    }
    public static SettlementArea? Create(AddressRecord source, District parent){
        if (source.AddressLevelCode != ADDRESS_LEVEL || parent is null){
            return null;
        }
        return new SettlementArea(source.AddressPartId,
            parent,
            (SettlementAreaTypes)source.ToponymType,
            new AddressNameToken(source.AddressName, Names[(SettlementAreaTypes)source.ToponymType])
        );
    }
    public async Task Save(ObservableTransaction? scope = null)
    {   
        await _parentDistrict.Save(scope);
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
            AddressName = _settlementAreaName.UnformattedName,
            ToponymType = (int)_settlementAreaType,
            ParentId = _parentDistrict.Id
        };
    }
    public IEnumerable<IAddressPart> GetDescendants()
    {
        IEnumerable<AddressRecord> foundUntyped = AddressModel.FindRecords(_id).Result;
        return foundUntyped.Select(rec => Settlement.Create(rec, this)) ?? new List<Settlement>();
    }
    public override string ToString(){
        return _settlementAreaName.FormattedName;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != typeof(SettlementArea)){
            return false;
        }
        var toCompare = (SettlementArea)obj;
        return toCompare._id == this._id;
    }
}

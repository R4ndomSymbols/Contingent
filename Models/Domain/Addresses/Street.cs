using System.Text.RegularExpressions;
using Utilities;
namespace StudentTracking.Models.Domain.Address;
public class Street : IAddressPart
{
    public const int ADDRESS_LEVEL = 5;  
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"улица"),
        new Regex(@"набережная"),
        new Regex(@"проспект"),
        new Regex(@"тупик"),
        new Regex(@"аллея"),
        new Regex(@"площадь"),
        new Regex(@"проезд"),
        new Regex(@"шоссе"),
    };
    public static readonly IReadOnlyDictionary<StreetTypes, AddressNameFormatting> Names = new Dictionary<StreetTypes, AddressNameFormatting>(){
        {StreetTypes.NotMentioned, new AddressNameFormatting("нет", "Не указано", AddressNameFormatting.BEFORE)},
        {StreetTypes.Street, new AddressNameFormatting("ул.", "Улица", AddressNameFormatting.BEFORE)},
        {StreetTypes.Embankment, new AddressNameFormatting("наб.", "Набережная", AddressNameFormatting.BEFORE)},
        {StreetTypes.Avenue, new AddressNameFormatting("пр-кт", "Проспект", AddressNameFormatting.BEFORE)},
        {StreetTypes.DeadEnd, new AddressNameFormatting("туп.", "Тупик", AddressNameFormatting.BEFORE)},
        {StreetTypes.Alley, new AddressNameFormatting("ал.", "Аллея", AddressNameFormatting.BEFORE)},
        {StreetTypes.Square, new AddressNameFormatting("пл.", "Площадь", AddressNameFormatting.BEFORE)},
        {StreetTypes.Passage, new AddressNameFormatting("пр-д", "Проезд", AddressNameFormatting.BEFORE)},
        {StreetTypes.Highway, new AddressNameFormatting("ш.", "Шоссе", AddressNameFormatting.BEFORE)},
    };
    public enum StreetTypes
    {
        NotMentioned = -1,
        Street = 1, // улица
        Embankment = 2, // набережная
        Avenue = 3, // проспект 
        DeadEnd = 4, // тупик
        Alley = 5, // аллея
        Square = 6, // площадь
        Passage = 7, // проезд 
        Highway = 8, // шоссе 
    }
    private int _id;
    private Settlement _parentSettlement;
    private AddressNameToken _streetName;
    private StreetTypes _streetType;
    public int Id
    {
        get => _id;
    }
    public Settlement SettlementParentId
    {
        get => _parentSettlement;
    }
    protected Street(int id)
    {
        _id = id;
    }
    protected Street() : base()
    {
        _id = Utils.INVALID_ID;
    }
    public static Result<Street?> Create(string addressPart, Settlement parent, ObservableTransaction? searchScope = null){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<Street>.Failure(new ValidationError(nameof(Street), "Объект дорожной инфраструктуры не указан или указан неверно"));
        }
        AddressNameToken? foundStreet = null;
        StreetTypes streetType = StreetTypes.NotMentioned;  
        foreach (var pair in Names){
            foundStreet = pair.Value.ExtractToken(addressPart); 
            if (foundStreet is not null){
                streetType = pair.Key;
                break;
            }
        }
        if (foundStreet is null){
            return Result<Street>.Failure(new ValidationError(nameof(Street), "Объект дорожной инфраструктуры не распознан"));
        }
        var fromDb = AddressModel.FindRecords(parent.Id, foundStreet.UnformattedName, (int)streetType, ADDRESS_LEVEL, searchScope).Result;
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<Street>.Failure(new ValidationError(nameof(Street), "Объект дорожной инфраструктуры не может быть однозначно распознан"));
            }
            else{
                var first = fromDb.First();
                return Result<Street?>.Success(new Street(first.AddressPartId){
                    _parentSettlement = parent, 
                    _streetType = (StreetTypes)first.ToponymType,
                    _streetName = new AddressNameToken(first.AddressName, Names[(StreetTypes)first.ToponymType])
                });
            }
        }
        
        var got = new Street(){
            _parentSettlement = parent,
            _streetType = streetType,
            _streetName = foundStreet,
        };
        return Result<Street?>.Success(got);
    }
    public static Street Create(AddressRecord source, Settlement parent){
        return new Street(source.AddressPartId){
            _parentSettlement = parent,
            _streetType = (StreetTypes)source.ToponymType,
            _streetName = new AddressNameToken(source.AddressName, Names[(StreetTypes)source.ToponymType])
        };
    }
    public async Task Save(ObservableTransaction? scope = null)
    {   
        await _parentSettlement.Save(scope);
        if (_id == Utils.INVALID_ID){
            _id = await AddressModel.SaveRecord(this, scope); 
        }
    }
    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            AddressPartId = _id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName = _streetName.UnformattedName,
            ToponymType = (int)_streetType,
            ParentId = _parentSettlement.Id
        };
    }
    public IEnumerable<IAddressPart> GetDescendants()
    {
        var found = AddressModel.FindRecords(_id).Result;
        return found.Select(rec => Building.Create(rec, this));
    }
    public override string ToString()
    {
        return _streetName.FormattedName;
    }
}

using System.Text.RegularExpressions;
using Utilities;

namespace StudentTracking.Models.Domain.Address;


public class FederalSubject : IAddressPart
{
    public const int ADDRESS_LEVEL = 1;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"республик(а|и)"),
        new Regex(@"(федеральн|город)"),
        new Regex(@"кра(й|я)"),
        new Regex(@"округ(и|а)"),
        new Regex(@"област(ь|и)")
    };

    private int _id;
    private AddressNameToken _subjectName;
    private FederalSubjectTypes _federalSubjectType;

    public int Id {
        get => _id;
    }

    private FederalSubject()
    {
        _id = Utils.INVALID_ID;
    }
    private FederalSubject(int id){
        _id = id;
    }

    public enum FederalSubjectTypes
    {
        NotMentioned = -1,
        Republic = 1,
        FederalCity = 2,
        Edge = 3, // край
        Autonomy = 4, // автономная область
        AutomomyDistrict = 5, // автономный округ
        Region = 6, // область
    }

    public static readonly IReadOnlyDictionary<FederalSubjectTypes, AddressNameFormatting> Names = new Dictionary<FederalSubjectTypes, AddressNameFormatting>(){
        {FederalSubjectTypes.NotMentioned, new AddressNameFormatting("Нет", "Не указано", AddressNameFormatting.BEFORE)},
        {FederalSubjectTypes.Republic, new AddressNameFormatting("респ.", "Республика", AddressNameFormatting.BEFORE)},
        {FederalSubjectTypes.FederalCity, new AddressNameFormatting("г.ф.з.", "Город федерального значения", AddressNameFormatting.BEFORE)},
        {FederalSubjectTypes.Edge, new AddressNameFormatting("край", "Край", AddressNameFormatting.BEFORE)},
        {FederalSubjectTypes.Autonomy, new AddressNameFormatting("а.обл.", "Автономная область", AddressNameFormatting.BEFORE)},
        {FederalSubjectTypes.AutomomyDistrict, new AddressNameFormatting("а.окр", "Автономный округ", AddressNameFormatting.BEFORE)},
        {FederalSubjectTypes.Region, new AddressNameFormatting("обл.", "Область", AddressNameFormatting.BEFORE)},
    };
    // кода у субъекта не будет
    // метод одновременно ищет и в базе адресов
    public static Result<FederalSubject?> Create(string addressPart, ObservableTransaction? searchScope = null){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<FederalSubject>.Failure(new ValidationError(nameof(FederalSubject), "Субъект федерации указан неверно"));
        }

        AddressNameToken? found = null;
        FederalSubjectTypes subjectType = FederalSubjectTypes.NotMentioned;  
        foreach (var pair in Names){
            found = pair.Value.ExtractToken(addressPart); 
            if (found is not null){
                subjectType = pair.Key;
                break;
            }
        }
        if (found is null){
            return Result<FederalSubject>.Failure(new ValidationError(nameof(FederalSubject), "Субъект федерации не распознан"));
        }
        var fromDb = AddressModel.FindRecords(null, found.UnformattedName, (int)subjectType, ADDRESS_LEVEL, searchScope).Result;
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<FederalSubject>.Failure(new ValidationError(nameof(FederalSubject), "Субъект федерации не может быть однозначно распознан"));
            }
            else{
                var first = fromDb.First();
                return Result<FederalSubject?>.Success(new FederalSubject(first.AddressPartId){
                    _federalSubjectType = (FederalSubjectTypes)first.ToponymType,
                    _subjectName = new AddressNameToken(first.AddressName, Names[(FederalSubjectTypes)first.ToponymType])
                });
            }
        }
        
        var got = new FederalSubject(){
            _federalSubjectType = subjectType,
            _subjectName = found,
        };
        return Result<FederalSubject?>.Success(got);
    }

    public static FederalSubject? Create(AddressRecord source){
        if (source.AddressLevelCode != ADDRESS_LEVEL){
            return null;
        }
        return new FederalSubject(source.AddressPartId){
            _federalSubjectType = (FederalSubjectTypes)source.ToponymType,
            _subjectName = new AddressNameToken(source.AddressName, Names[(FederalSubjectTypes)source.ToponymType])
        };
    } 

    public async Task Save(ObservableTransaction? transaction = null){
        if (_id == Utils.INVALID_ID){
            _id = await AddressModel.SaveRecord(this, transaction);
        }
        
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (obj.GetType() != typeof(FederalSubject))
        {
            return false;
        }
        var unboxed = (FederalSubject)obj;
        return 
        _id== unboxed._id;
    }

    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            ParentId = null,
            AddressPartId = _id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName =  _subjectName.UnformattedName,
            ToponymType = (int)_federalSubjectType
        };
    }

    public IEnumerable<IAddressPart> GetDescendants()
    {
        var found = AddressModel.FindRecords(_id).Result;
        return found.Select(rec => District.Create(rec, this));
    }

    public override string ToString()
    {
        return _subjectName.FormattedName;
    }
}
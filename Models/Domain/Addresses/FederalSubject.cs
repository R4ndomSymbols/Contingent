using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain.Address;


public class FederalSubject : IAddressRecord
{
    private const int _addressLevel = 1;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"республик(а|и)"),
        new Regex(@"(федеральн|город)"),
        new Regex(@"кра(й|я)"),
        new Regex(@"округ(и|а)"),
        new Regex(@"област(ь|и)")
    };

    private int _id;
    private string _subjectUntypedName;
    private Types _federalSubjectType;

    public int Id {
        get => _id;
    }
    public int SubjectType
    {
        get => (int)_federalSubjectType;
    }
    public string UntypedName
    {
        get => Utils.FormatToponymName(_subjectUntypedName);
    }
    public string LongTypedName
    {
        get
        {
            return Names[_federalSubjectType].FormatLong(UntypedName);
        }
    }

    private FederalSubject()
    {
        _id = Utils.INVALID_ID;
    }
    private FederalSubject(int id){
        _id = id;
    }

    public enum Types
    {
        NotMentioned = -1,
        Republic = 1,
        FederalCity = 2,
        Edge = 3, // край
        Autonomy = 4, // автономная область
        AutomomyDistrict = 5, // автономный округ
        Region = 6, // область
    }

    public static readonly IReadOnlyDictionary<Types, NameFormatting> Names = new Dictionary<Types, NameFormatting>(){
        {Types.NotMentioned, new NameFormatting("Нет", "Не указано", NameFormatting.BEFORE)},
        {Types.Republic, new NameFormatting("респ.", "Республика", NameFormatting.BEFORE)},
        {Types.FederalCity, new NameFormatting("г.ф.з.", "Город федерального значения", NameFormatting.BEFORE)},
        {Types.Edge, new NameFormatting("край", "Край", NameFormatting.BEFORE)},
        {Types.Autonomy, new NameFormatting("а.обл.", "Автономная область", NameFormatting.BEFORE)},
        {Types.AutomomyDistrict, new NameFormatting("а.окр", "Автономный округ", NameFormatting.BEFORE)},
        {Types.Region, new NameFormatting("обл.", "Область", NameFormatting.BEFORE)},
    };
    // кода у субъекта не будет
    // метод одновременно ищет и в базе адресов
    public static Result<FederalSubject?> Create(string addressPart){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<FederalSubject>.Failure(new ValidationError(nameof(FederalSubject), "Субъект федерации указан неверно"));
        }

        NameToken? found = null;
        Types subjectType = Types.NotMentioned;  
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
        var fromDb = AddressModel.FindRecords(found.Name, (int)subjectType, _addressLevel);
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<FederalSubject>.Failure(new ValidationError(nameof(FederalSubject), "Субъект федерации не может быть однозначно распознан"));
            }
            else{
                var first = fromDb.First();
                return Result<FederalSubject?>.Success(new FederalSubject(first.AddressPartId){
                    _federalSubjectType = (Types)first.ToponymType,
                    _subjectUntypedName = first.AddressName
                });
            }
        }
        
        var got = new FederalSubject(){
            _federalSubjectType = subjectType,
            _subjectUntypedName = found.Name,
        };
        return Result<FederalSubject?>.Success(got);
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
            AddressLevelCode = _addressLevel,
            AddressName =  _subjectUntypedName,
            ToponymType = (int)_federalSubjectType
        };
    }
}
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain.ValueObjects.Students;

public class NamePart {

    public string NameToken {get; private init;}

    private NamePart(string name)
    {
        NameToken = name;
    } 

    public static Result<NamePart> Create(string? name){
        if (!ValidatorCollection.CheckStringPattern(name, ValidatorCollection.RussianNamePart)){
            return Result<NamePart>.Failure(new ValidationError("Нарушение формата имени"));
        }
        return Result<NamePart>.Success(new NamePart(name));

    }
    public static Result<NamePart> Create(string? name, int maxLength){
        var res = Create(name);
        if (res.IsFailure){
            return res;
        }
        if (res.ResultObject.NameToken.Length < maxLength){
            return Result<NamePart>.Success(res.ResultObject);
        }
        else {
            return Result<NamePart>.Failure(new ValidationError("Нарушение ограничения длины"));
        }

    }
}

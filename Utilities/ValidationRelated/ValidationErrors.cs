using System.Text.Json.Serialization;

[Serializable]
public class ValidationError {

    private string _message;
    private string _propertyName;
    [JsonPropertyName("Err")]
    public string ErrorMessage {get => _message;}
    [JsonPropertyName("Field")]
    public string PropertyName {get => _propertyName;}
    
    public ValidationError(string propName, string exceptionMessage){
        _message = exceptionMessage;
        _propertyName = propName;
    }
    public override string ToString(){
        return "Ошибка: " + _message + "\t" + "Свойство: " + _propertyName; 
    }

    public string ToUserString(){
        return "Ошибка: " + _message;
    }

    public static bool operator == (ValidationError? left, ValidationError? right){
        if (left is null){
            return false;
        }
        if (right is null){
            return false;
        }
        return left._propertyName == right._propertyName;
    }
    public static bool operator != (ValidationError? left, ValidationError? right){
        return !(left == right);
    }
    public override bool Equals(object? obj)
    {   if (obj == null){
            return false; 
        }
        if (obj.GetType() != typeof(ValidationError)){
            return false;
        }
        return this == (ValidationError)obj;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

}

public class DbIntegrityValidationError : ValidationError
{
    public DbIntegrityValidationError(string propName, string exceptionMessage) : base( propName, exceptionMessage)
    {
        
    }
}


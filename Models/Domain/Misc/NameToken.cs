using Microsoft.AspNetCore.Authentication;

namespace StudentTracking.Models.Domain.Misc;

public class NameToken {
    public readonly string Name;
    public readonly string TypeName;

    public NameToken (string name, string typeName){
        Name = name;
        TypeName = typeName;
    }
}
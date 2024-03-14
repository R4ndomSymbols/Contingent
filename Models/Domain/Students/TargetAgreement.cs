namespace StudentTracking.Models.Domain.Misc;


public class TargetEduAgreement{

    public string RussianName {get; private init;}
    public TypesOfEducationAgreement AgreementType {get; private init;}
    private TargetEduAgreement(TypesOfEducationAgreement type, string name){
        RussianName = name;
        AgreementType = type;
    }

    public static IReadOnlyCollection<TargetEduAgreement> ListOfTypes => new List<TargetEduAgreement>(){
        new TargetEduAgreement(TypesOfEducationAgreement.NotMentioned, "Не указан"),
        new TargetEduAgreement(TypesOfEducationAgreement.WithFederalGovenrment, "С федеральным государственным органом"),
        new TargetEduAgreement(TypesOfEducationAgreement.WithRegionGovernment, "С органом государственной власти субъекта РФ"),
        new TargetEduAgreement(TypesOfEducationAgreement.WithLocalAutority, "С органом местного самоуправления"),
        new TargetEduAgreement(TypesOfEducationAgreement.WithOrganization, "С организацией")
    }; 

    public static TargetEduAgreement GetByTypeCode(int code){
        return ListOfTypes.Where(x => (int)x.AgreementType == code).First();
    }
    public static bool TryGetByTypeCode(int code){
        return ListOfTypes.Any(x => (int)x.AgreementType == code);
    }
}

public enum TypesOfEducationAgreement{
    NotMentioned  = -1,
    WithFederalGovenrment = 1, // с федеральным государственным органом
    WithRegionGovernment = 2, // с органов власти субъекта
    WithLocalAutority = 3, // с органом местного самоуправления
    WithOrganization = 4, // с негосударственной организацией
}


namespace StudentTracking.Models.Domain.Misc;


public static class TargetEduAgreement{

    public enum Types{
        NotMentioned  = -1,
        WithFederalGovenrment = 1, // с федеральным государственным органом
        WithRegionGovernment = 2, // с органов власти субъекта
        WithLocalAutority = 3, // с органом местного самоуправления
        WithOrganization = 4, // с негосударственной организацией
    }

    public static readonly Dictionary<Types, string> Names = new Dictionary<Types, string>(){

        {Types.NotMentioned, "Не указан"},
        {Types.WithFederalGovenrment, "С федеральным государственным органом"},
        {Types.NotMentioned, "С органом государственной власти субъекта РФ"},
        {Types.NotMentioned, "С органом местного самоуправления"},
        {Types.NotMentioned, "С организацией"}

    };
}

namespace StudentTracking.Models.Domain.Misc;

public static class PaidEduAgreement {

    public enum Types {
        NotMentioned = -1,
        LegalRepresentative = 1, // законный представитель
        OtherIndividual = 2, // другое физическое лицо
        Entity = 3, // юридическое лицо
    }

    public static readonly Dictionary<Types, string> Names = new Dictionary<Types, string>(){

        {Types.NotMentioned, "Не указано"},
        {Types.LegalRepresentative, "За счет средств студента или его законного представителя"},
        {Types.OtherIndividual, "За счет средств иного физического лица"},
        {Types.Entity, "За счет средств юридического лица"},

    };

}
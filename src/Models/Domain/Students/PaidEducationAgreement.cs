namespace Contingent.Models.Domain.Students;

public class PaidEduAgreement
{

    public string RussianName { get; private init; }
    public PaidEducationAgreementTypes AgreementType { get; private init; }

    private PaidEduAgreement(PaidEducationAgreementTypes type, string name)
    {
        AgreementType = type;
        RussianName = name;
    }

    public static IReadOnlyCollection<PaidEduAgreement> ListOfTypes => new List<PaidEduAgreement>(){

        new (PaidEducationAgreementTypes.NotMentioned, "Не указано"),
        new (PaidEducationAgreementTypes.LegalRepresentative, "За счет средств студента или его законного представителя"),
        new (PaidEducationAgreementTypes.OtherIndividual, "За счет средств иного физического лица"),
        new (PaidEducationAgreementTypes.Entity, "За счет средств юридического лица")

    };
    private static readonly Dictionary<string, PaidEducationAgreementTypes> _importDictionary = new() {
        {"", PaidEducationAgreementTypes.NotMentioned},
        {"нет", PaidEducationAgreementTypes.NotMentioned},
        {"да", PaidEducationAgreementTypes.LegalRepresentative},
        {"есть", PaidEducationAgreementTypes.LegalRepresentative},
        {"за счет средств иного физического лица", PaidEducationAgreementTypes.OtherIndividual},
        {"за счет средств юридического лица", PaidEducationAgreementTypes.Entity}
    };

    public static PaidEduAgreement GetByTypeCode(int code)
    {
        return ListOfTypes.Where(x => (int)x.AgreementType == code).First();
    }
    public static bool TryGetByTypeCode(int code)
    {
        return ListOfTypes.Any(x => (int)x.AgreementType == code);
    }

    public static int ImportType(string? typeName)
    {
        if (typeName is null)
        {
            return (int)PaidEducationAgreementTypes.NotMentioned;
        }
        if (_importDictionary.TryGetValue(typeName.ToLower(), out PaidEducationAgreementTypes found))
        {
            return (int)found;
        }
        return (int)PaidEducationAgreementTypes.NotMentioned;
    }

    public bool IsConcluded()
    {
        return AgreementType == PaidEducationAgreementTypes.LegalRepresentative || AgreementType == PaidEducationAgreementTypes.Entity || AgreementType == PaidEducationAgreementTypes.OtherIndividual;
    }


}


public enum PaidEducationAgreementTypes
{
    NotMentioned = -1,
    LegalRepresentative = 1, // законный представитель
    OtherIndividual = 2, // другое физическое лицо
    Entity = 3, // юридическое лицо
}
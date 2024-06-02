using Contingent.Models.Domain.Students;
using Utilities;

namespace Contingent.Controllers.DTO.Out;

[Serializable]
public sealed class StudentFullDTO
{
    public int? Id { get; private init; }
    public string Snils { get; private init; }
    public string DateOfBirth { get; private init; }
    public decimal AdmissionScore { get; private init; }
    public string GradeBookNumber { get; private init; }
    public int Gender { get; private init; }
    public string GenderName { get; private init; }
    public int TargetAgreementType { get; private init; }
    public int? GiaMark { get; private init; }
    public int? GiaDemoExamMark { get; private init; }
    public int PaidAgreementType { get; private init; }
    public RussianCitizenshipDTO Citizenship { get; private init; }
    public AddressOutDTO ActualAddress { get; private init; }
    public EducationalLevelRecordDTO[] EducationalLevels { get; private init; }
    public StudentFullDTO(StudentModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }
        Citizenship = model.RussianCitizenship is null ? new RussianCitizenshipDTO(model.RussianCitizenshipId) : new RussianCitizenshipDTO(model.RussianCitizenship);
        Id = model.Id;
        Snils = model.Snils;
        DateOfBirth = Utils.FormatDateTime(model.DateOfBirth);
        AdmissionScore = model.AdmissionScore;
        GradeBookNumber = model.GradeBookNumber;
        Gender = (int)model.Gender;
        TargetAgreementType = (int)model.TargetAgreementType.AgreementType;
        GiaMark = model.GiaMark;
        GiaDemoExamMark = model.GiaDemoExamMark;
        PaidAgreementType = (int)model.PaidAgreement.AgreementType;
        ActualAddress = model.ActualAddress is null ? new AddressOutDTO(model.ActualAddressId) : new AddressOutDTO(model.ActualAddress);
        GenderName = Genders.Names[model.Gender];
        EducationalLevels = StudentEducationalLevelRecord.GetByOwner(model).Select(x => new EducationalLevelRecordDTO(x)).ToArray();

    }
    public StudentFullDTO()
    {
        Citizenship = new RussianCitizenshipDTO();
        Id = null;
        Snils = string.Empty;
        DateOfBirth = string.Empty;
        AdmissionScore = 0;
        GradeBookNumber = string.Empty;
        Gender = (int)Genders.GenderCodes.Undefined;
        TargetAgreementType = (int)TypesOfEducationAgreement.NotMentioned;
        GiaMark = null;
        GiaDemoExamMark = null;
        PaidAgreementType = (int)PaidEducationAgreementTypes.NotMentioned;
        Citizenship = new RussianCitizenshipDTO();
        ActualAddress = new AddressOutDTO();
        GenderName = Genders.Names[Genders.GenderCodes.Undefined];
        EducationalLevels = Array.Empty<EducationalLevelRecordDTO>();
    }


}

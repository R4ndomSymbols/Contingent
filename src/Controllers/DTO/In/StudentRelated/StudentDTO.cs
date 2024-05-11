using System.Text.Json.Serialization;
using Contingent.Controllers.DTO.Out;
using Contingent.Import;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Specialties;
using Utilities;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public class StudentInDTO : IFromCSV<StudentInDTO>
{
    public const string GradeBookNumberFieldName = "номер в поименной книге";
    public const string DateOfBirthFieldName = "дата рождения";
    public const string GenderFieldName = "пол";
    public const string SnilsFieldName = "СНИЛС";
    public const string TargetAgreementFieldName = "договор о целевом обучении";
    public const string PaidAgreementFieldName = "договор о платном обучении";
    public const string AdmissionScoreFieldName = "вступительный балл";
    public const string GiaMarkFieldName = "балл ГИА";
    public const string GiaDemoExamMarkFieldName = "балл демо-экзамена ГИА";
    public const string PhysicalAddressFieldName = "место жительства";
    public const string EducationFieldName = "образование";

    public StudentInDTO()
    {
        RusCitizenship = null;
        Id = null;
        PhysicalAddress = new AddressInDTO();
        Education = new List<StudentEducationRecordInDTO>();
        GradeBookNumber = "";
        DateOfBirth = "";
        Gender = -1;
        Snils = "";
        TargetAgreementType = -1;
        PaidAgreementType = -1;
        AdmissionScore = "";
        GiaMark = "";
        GiaDemoExamMark = "";

    }
    public int? Id { get; set; }

    [JsonRequired]
    public string GradeBookNumber { get; set; }
    [JsonRequired]
    public string DateOfBirth { get; set; }
    [JsonRequired]
    public int Gender { get; set; }
    [JsonRequired]
    public string Snils { get; set; }
    [JsonRequired]
    public int TargetAgreementType { get; set; }
    [JsonRequired]
    public int PaidAgreementType { get; set; }
    [JsonRequired]
    public string AdmissionScore { get; set; }
    [JsonRequired]
    public string GiaMark { get; set; }
    [JsonRequired]
    public string GiaDemoExamMark { get; set; }
    public RussianCitizenshipInDTO? RusCitizenship { get; set; }
    [JsonRequired]
    public AddressInDTO PhysicalAddress { get; set; }
    [JsonRequired]
    public List<StudentEducationRecordInDTO> Education { get; set; }

    public Result<StudentInDTO> MapFromCSV(CSVRow row)
    {
        GradeBookNumber = row[GradeBookNumberFieldName]!;
        DateOfBirth = row[DateOfBirthFieldName]!;
        Gender = (int)Genders.ImportGender(row[GenderFieldName]!);
        Snils = row[SnilsFieldName]!;
        TargetAgreementType = TargetEduAgreement.ImportType(row[TargetAgreementFieldName]);
        PaidAgreementType = PaidEduAgreement.ImportType(row[PaidAgreementFieldName]);
        AdmissionScore = row[AdmissionScoreFieldName]!;
        GiaMark = row[GiaMarkFieldName]!;
        GiaDemoExamMark = row[GiaDemoExamMarkFieldName]!;
        RusCitizenship = new RussianCitizenshipInDTO().MapFromCSV(row).ResultObject;
        PhysicalAddress = new AddressInDTO()
        {
            Address = row[PhysicalAddressFieldName]
        };
        var education = row[EducationFieldName];
        Education = new List<StudentEducationRecordInDTO>();
        education?.Split(";").ToList().ForEach(x => { Education.Add(new StudentEducationRecordInDTO() { Level = LevelOfEducation.ImportLevelCode(x) }); });
        return Result<StudentInDTO>.Success(this);

    }
}

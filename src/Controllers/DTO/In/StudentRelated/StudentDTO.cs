using System.Text.Json.Serialization;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Import;
using StudentTracking.Models.Domain.Students;
using StudentTracking.Models.Domain.Specialities;
using Utilities;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentInDTO : IFromCSV<StudentInDTO>
{
    public StudentInDTO()
    {
        RusCitizenship = null;
        Id = null;
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
        GradeBookNumber = row["Номер в поименной книге"]!;
        DateOfBirth = row["Дата рождения"]!;
        Gender = (int)Genders.ImportGender(row["Пол"]);
        Snils = row["СНИЛС"]!;
        TargetAgreementType = TargetEduAgreement.ImportType(row["Целевое"]);
        PaidAgreementType = PaidEduAgreement.ImportType(row["договор о платном обучении"]);
        AdmissionScore = row["вступительный балл"]!;
        GiaMark = row["Балл ГИА"]!;
        GiaDemoExamMark = row["Балл демо-экзамена ГИА"]!;
        RusCitizenship = new RussianCitizenshipInDTO().MapFromCSV(row).ResultObject;
        PhysicalAddress = new AddressInDTO()
        {
            Address = row["место жительства"]
        };
        var education = row["образование"];
        Education = new List<StudentEducationRecordInDTO>();
        education?.Split(";").ToList().ForEach(x => { Education.Add(new StudentEducationRecordInDTO() { Level = LevelOfEducation.ImportLevelCode(x) }); });
        return Result<StudentInDTO>.Success(this);

    }
}

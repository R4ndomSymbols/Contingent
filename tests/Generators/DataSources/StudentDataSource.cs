using Contingent.Controllers.DTO.In;

namespace Tests;

public static class StudentDataSource
{

    private static Random gen = new();
    public static readonly NamedList Surnames = new NamedList(
        RussianCitizenshipInDTO.SurnameFieldName,
        new List<string>(){
            "Иванов", "Петров", "Сидоров", "Михайлов", "Кузнецов", "Николаев", "Александров", "Сергеев", "Васильев", "Дмитриев", "Алексеев", "Макаров", "Фёдоров", "Григорьев", "Игорев", "Вячеславов", "Евгеньев", "Алексеев", "Сергеев", "Васильев"
        }
    );
    public static readonly NamedList Names = new NamedList(
        RussianCitizenshipInDTO.NameFieldName,
        new List<string>(){
            "Александр", "Антон", "Артем", "Владимир", "Виктор", "Дмитрий", "Евгений", "Иван", "Константин", "Леонид", "Максим", "Михаил", "Николай", "Павел", "Роман", "Сергей", "Тимофей", "Фёдор", "Юрий", "Ярослав"
        }
    );
    public static readonly NamedList Patronymics = new NamedList(
        RussianCitizenshipInDTO.PatronymicFieldName,
        new List<string>(){
            "Александровна", "Борисовна", "Васильевна", "Григорьевна", "Дмитриевна", "Евдокимовна", "Захаровна", "Ивановна", "Капитоновна", "Леоновна",
            "Максимовна", "Николаевна", "Олеговна", "Петровна", "Романовна", "Сергеевна", "Тимофеевна", "Степановна", "Федоровна", "Юрьевна",
            "Александрович", "Алексеевич", "Борисович", "Васильевич", "Григорьевич", "Дмитрийчик", "Евдокимович", "Захарович", "Иванович", "Капитонович",
            "Леоновыч", "Максимович", "Николаевич", "Олегович", "Петрович", "Романович", "Сергеевич", "Тимофеевич", "Федорович", "Юрьевич", "","","",""
    });
    public static readonly NamedField GradeBook = new(StudentInDTO.GradeBookNumberFieldName, () => gen.Next(1000, 10000).ToString());
    public static readonly NamedField DateOfBirth = new(StudentInDTO.DateOfBirthFieldName, () =>
    {
        long lower = new DateTime(1960, 1, 1).Ticks;
        long max = new DateTime(2010, 1, 1).Ticks;
        var date = new DateTime(gen.NextInt64(lower, max));
        return date.ToString("dd.MM.yyyy");
    });

    public static readonly NamedField Gender = new(StudentInDTO.GenderFieldName, () => new string[] { "м", "ж" }[gen.Next(0, 2)]);
    public static readonly NamedField TargetAgreement = new(StudentInDTO.TargetAgreementFieldName, () => gen.NextDouble() > 0.9 ? "есть" : "нет");

    public static readonly NamedField Snils = new(StudentInDTO.SnilsFieldName,
    () => string.Format("{0}-{1}-{2} {3}", gen.Next(1000, 10000).ToString()[1..], gen.Next(1000, 10000).ToString()[1..], gen.Next(1000, 10000).ToString()[1..], gen.Next(100, 1000).ToString()[1..])
    );
    public static readonly NamedField PaidAgreement = new(StudentInDTO.PaidAgreementFieldName,
    () => gen.NextDouble() > 0.85 ? "есть" : "нет");
    public static readonly NamedField AdmissionScore = new(StudentInDTO.AdmissionScoreFieldName,
    () => (3 + gen.NextDouble() * 2).ToString("N2"));
    public static readonly NamedField GiaMark = new(StudentInDTO.GiaMarkFieldName,
    () => gen.NextDouble() > 0.9 ? (3 + gen.NextDouble() * 2).ToString("N2") : "");
    public static readonly NamedField DemoExamGiaMark = new(StudentInDTO.GiaDemoExamMarkFieldName,
    () => gen.NextDouble() > 0.9 ? (3 + gen.NextDouble() * 2).ToString("N2") : "");
    public static readonly NamedField Education = new(StudentInDTO.EducationFieldName,
    () =>
    {
        var next = gen.NextDouble();
        return string.Join(";",
            new string[] { "ооо", "соо", "спо" }
            [0..(next > 0.9 ? (next > 0.99 ? 3 : 2) : 1)]);
    });





}

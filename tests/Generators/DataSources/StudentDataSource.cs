namespace Tests;

public static class StudentDataSource {

    private static Random gen = new(74097503);
    public static readonly NamedList Surnames = new NamedList(
        "фамилия",
        new List<string>(){
            "Иванов", "Петров", "Сидоров", "Михайлов", "Кузнецов", "Николаев", "Александров", "Сергеев", "Васильев", "Дмитриев", "Алексеев", "Макаров", "Фёдоров", "Григорьев", "Игорев", "Вячеславов", "Евгеньев", "Алексеев", "Сергеев", "Васильев"
        }
    );
    public static readonly NamedList Names = new NamedList(
        "имя",
        new List<string>(){
            "Александр", "Антон", "Артем", "Владимир", "Виктор", "Дмитрий", "Евгений", "Иван", "Константин", "Леонид", "Максим", "Михаил", "Николай", "Павел", "Роман", "Сергей", "Тимофей", "Фёдор", "Юрий", "Ярослав"
        }
    );
    public static readonly NamedList Patronymics = new NamedList(
        "отчество",
        new List<string>(){
            "Александровна", "Борисовна", "Васильевна", "Григорьевна", "Дмитриевна", "Евдокимовна", "Захаровна", "Ивановна", "Капитоновна", "Леоновна",
            "Максимовна", "Николаевна", "Олеговна", "Петровна", "Романовна", "Сергеевна", "Тимофеевна", "Степановна", "Федоровна", "Юрьевна",
            "Александрович", "Алексеевич", "Борисович", "Васильевич", "Григорьевич", "Дмитрийчик", "Евдокимович", "Захарович", "Иванович", "Капитонович",
            "Леоновыч", "Максимович", "Николаевич", "Олегович", "Петрович", "Романович", "Сергеевич", "Тимофеевич", "Федорович", "Юрьевич", "","","",""
    });
    public static readonly NamedField GradeBook = new("номер в поименной книге", () => gen.Next(1000,10000).ToString());
    public static readonly  NamedField DateOfBirth = new("дата рождения", () =>{
        long lower = new DateTime(1960, 1, 1).Ticks;
        long max = new DateTime(2010, 1, 1).Ticks;
        var date = new DateTime(gen.NextInt64(lower, max));
        return date.ToString("dd.MM.yyyy");
    });

    public static readonly NamedField Gender = new("пол", () => new string[]{"м", "ж"}[gen.Next(0,2)]);
    public static readonly NamedField TargetAgreement = new("целевое", () => gen.NextDouble() > 0.9 ? "есть" : "нет");

    public static readonly NamedField Snils = new("снилс", 
    () => string.Format("{0}-{1}-{2} {3}",gen.Next(1000,10000).ToString()[1..],gen.Next(1000,10000).ToString()[1..],gen.Next(1000,10000).ToString()[1..],gen.Next(100,1000).ToString()[1..])  
    );
    public static readonly NamedField PaidAgreement = new("договор о платном обучении", 
    () => gen.NextDouble() > 0.85 ? "есть" : "нет");
    public static readonly NamedField AddmissionScore = new("вступительный балл",
    () => (3 + gen.NextDouble() * 2).ToString("N2"));
    public static readonly NamedField GiaMark = new("балл ГИА",
    () => gen.NextDouble() > 0.9 ? (3 + gen.NextDouble() * 2).ToString("N2") : "");
    public static readonly NamedField DemoExamGiaMark = new("балл демэкзамена",
    () => gen.NextDouble() > 0.9 ? (3 + gen.NextDouble() * 2).ToString("N2") : "");
    public static readonly NamedField Education = new("балл демэкзамена",
    () =>{
        var next = gen.NextDouble();
        return string.Join(";", 
            new string[]{"основное общее образование","среднее общее образование", "среднее профессиональное образование"}
            [0..(next > 0.9 ? (next > 0.99 ? 3 : 2) : 1)]);
    });


       


}

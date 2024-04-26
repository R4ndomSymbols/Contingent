using StudentTracking.Models;
using StudentTracking.Models.Domain;
using StudentTracking.Models.Domain.Address;
using StudentTracking.Models.Domain.Misc;

namespace StudentTracking.Statistics.Tables.Headers;


public static class TemplateHeaders{
    public static TableRowHeader<T> GetSpecialityRowHeader<T>(
        Func<T, SpecialityModel?> getter, 
        TableColumnHeader<T> vertical, 
        RowHeaderCell<T>? root = null){
        var rootNode = root ?? new RowHeaderCell<T>();
        IEnumerable<SpecialityModel> allSpecialities = SpecialityModel.GetAll();
        foreach (var sp in allSpecialities){
            var speciality = sp;
            var child = new RowHeaderCell<T>(
                sp.ToString(),
                rootNode,
                new Filter<T>(
                    (source) => 
                        source.Where(
                            model => {
                                var got = getter.Invoke(model);
                                if (got is null){
                                    return false;
                                }
                                return got.Equals(speciality);
                            }
                        )
                )
            );
        }
        return new TableRowHeader<T>(rootNode, vertical, false);
    }

    public static RowHeaderCell<T> GetAddressRowHeader<T>(
        Func<T, AddressModel?> getter,
        IEnumerable<IAddressPart> cellAddresses,
        RowHeaderCell<T>? root = null
    ){
        var rootNode = root ?? new RowHeaderCell<T>();
        foreach (var address in cellAddresses){
            var addr = address;
            var cell = new RowHeaderCell<T>(
                address.ToString(),
                rootNode,
                new Filter<T>(
                    (source) => source.Where(
                        model => {
                            var got = getter.Invoke(model);
                            if (got is null){
                                return false;
                            }
                            return got.Contains(addr);
                        }
                    )
                ) 
            );
        }
        return rootNode;
    }

    public static ColumnHeaderCell<T> GetBaseCourseHeader<T>(
        int course,
        Func<T, StudentModel?> studentGetter,
        Func<T, GroupModel?> groupGetter,
        ColumnHeaderCell<T>? root = null  
    ){
        var rootNode = root ?? new ColumnHeaderCell<T>();
        var courseDisplayCell1 = new ColumnHeaderCell<T>(
            "Численность студентов " + course.ToString() + " курса",
            rootNode,
            new Filter<T>(
                (source) => source.Where(
                    model => {
                        var got = groupGetter.Invoke(model);
                        if (got is null){
                            return false;
                        }
                        return got.CourseOn == course;
                    }
                )
            )
        );
        var studentOnPayment2 = new ColumnHeaderCell<T>(
            "Из них на внебюджетном финансировании",
            courseDisplayCell1,
            new Filter<T>(
                (source) => source.Where(
                    model => {
                        var got = groupGetter.Invoke(model);
                        if (got is null){
                            return false;
                        }
                        return got.SponsorshipType.IsPaid();
                    }
                )
            )
        );
        var studentOnFree2 = new ColumnHeaderCell<T>(
            "Из них на бюджетном финансировании",
            courseDisplayCell1,
            new Filter<T>(
                (source) => source.Where(
                    model => {
                        var got = groupGetter.Invoke(model);
                        if (got is null){
                            return false;
                        }
                        return got.SponsorshipType.IsFree();
                    }
                )
            )
        );
        var studentFemale = new ColumnHeaderCell<T>(
            "Из них женщины",
            courseDisplayCell1,
            new Filter<T>(
                (source) => source.Where(
                    model => {
                        var got = studentGetter.Invoke(model);
                        if (got is null){
                            return false;
                        }
                        return got.Gender == Genders.GenderCodes.Female;
                    }
                )
            )
        );
        var studentMale = new ColumnHeaderCell<T>(
            "Из них мужчины",
            courseDisplayCell1,
            new Filter<T>(
                (source) => source.Where(
                    model => {
                        var got = studentGetter.Invoke(model);
                        if (got is null){
                            return false;
                        }
                        return got.Gender == Genders.GenderCodes.Male;
                    }
                )
            )
        );
        return rootNode;
    } 


}
using Npgsql;
using StudentTracking.Models.Domain.Address;
using Utilities;

namespace StudentTracking.Models.SQL;

public class AddressRestoreQueryBuilder {

    private const string _mainQuery = 
    " SELECT \n" + 
    " federal_subjects.code AS subj_id, \n" +
    " federal_subjects.subject_type AS subj_type, \n" +
    " federal_subjects.full_name AS subj_name, \n" +
    " districts.id AS dist_id, \n" +
    " districts.full_name AS dist_name, \n" +
    " districts.district_type AS dist_type, \n" +
    " settlement_areas.id AS setarea_id, \n" +
    " settlement_areas.full_name AS setarea_name, \n" +
    " settlement_areas.settlement_area_type AS setarea_type, \n" +
    " settlements.id AS set_id, \n" +
    " settlements.full_name AS set_name, \n" +
    " settlements.settlement_type AS set_type, \n" +
    " streets.id AS street_id, \n" +
    " streets.full_name AS street_name, \n" +
    " streets.street_type AS street_type \n" +
    " FROM federal_subjects \n" +
    " JOIN districts ON districts.federal_subject_code = federal_subjects.code \n" +
    " LEFT JOIN settlement_areas ON settlement_areas.district = districts.id \n" +
    " JOIN settlements ON settlements.district = districts.id OR settlement_areas.id = settlements.settlement_area \n" +
    " JOIN streets ON streets.settlement = settlements.id \n" +
    " WHERE {find_clause} \n" +
    " LIMIT {count} ";

    public AddressRestoreQueryBuilder(){

    }
    private string FormatLike(string text) {
        return "\'%" + text + "%\'"; 
    }
    public List<string>? SearchUntyped(string plainText, int count){
        string liked = FormatLike(plainText);
        string findClause = 
        $" federal_subjects.full_name LIKE {liked} " +
        $" OR districts.full_name LIKE {liked} " +
        $" OR settlement_areas.full_name LIKE {liked} " +
        $" OR settlements.full_name LIKE {liked} " +
        $" OR streets.full_name LIKE {liked} ";

        string query = _mainQuery.Replace("{find_clause}", findClause);
        query = query.Replace("{count}", count.ToString());

        Console.WriteLine(query);

        using (var conn = Utils.GetConnectionFactory()){
            conn.Open();
            using (var cmd = new NpgsqlCommand(query, conn)){
                
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows){
                    return null;
                }
                var toReturn = new List<string>();

                while (reader.Read()){
                    string found =  AddressModel.CreateAddressString(
                        FederalSubject.MakeUnsafe((int)reader["subj_id"], (string)reader["subj_name"], (int)reader["subj_type"]),
                        District.MakeUnsafe((int)reader["dist_id"], (string)reader["dist_name"], (int)reader["dist_type"]),
                        reader["setarea_id"].GetType() == typeof(DBNull) ? 
                            null :
                            SettlementArea.MakeUnsafe((int)reader["setarea_id"], (string)reader["setarea_name"], (int)reader["setarea_type"]),
                        Settlement.MakeUnsafe((int)reader["set_id"], (string)reader["set_name"], (int)reader["set_type"]),
                        Street.MakeUnsafe((int)reader["street_id"], (string)reader["street_name"], (int)reader["street_type"]),
                        null, 
                        null
                    );
                    toReturn.Add(found);
                }
                return toReturn;
            }
        }
    }

    public List<string>? SelectFrom(object? addressPart, string? findText, int count){
        if (findText == null){
            return null;
        }
        if (addressPart == null){
            return null;
        }
        if (count < 0 || count > 200){
            return null;
        }

        int addressLevel = 0; 
        string findClause = "";

        if(addressPart is FederalSubject){
            var converted = (FederalSubject)addressPart; 
            findClause = $" federal_subjects.full_name LIKE {FormatLike(converted.UntypedName)} " 
            + (converted.SubjectType == (int)FederalSubject.Types.NotMentioned ? "" 
            : " AND federal_subjects.subject_type = " + converted.SubjectType.ToString());
            addressLevel = 1;
        }
        else if(addressPart is District){
            var converted = (District)addressPart; 
            findClause = $" districts.full_name LIKE {FormatLike(converted.UntypedName)} " 
            + (converted.DistrictType == (int)District.Types.NotMentioned ? "" 
            : " AND districts.district_type = " + converted.DistrictType.ToString());
            addressLevel = 2;
        }
        else if(addressPart is SettlementArea){
            var converted = (SettlementArea)addressPart; 
            findClause = $" settlement_areas.full_name LIKE {FormatLike(converted.UntypedName)} " 
            + (converted.SettlementAreaType == (int)SettlementArea.Types.NotMentioned ? "" 
            : " AND settlement_areas.settlement_area_type = " + converted.SettlementAreaType.ToString());
            addressLevel = 3;
        }
        else if(addressPart is Settlement ){
            var converted = (Settlement)addressPart; 
            findClause = $" settlements.full_name LIKE {FormatLike(converted.UntypedName)} " 
            + (converted.SettlementType == (int)Settlement.Types.NotMentioned ? "" 
            : " AND settlements.settlement_type = " + converted.SettlementType.ToString());
            addressLevel = 4;
        }
        else if (addressPart is Street)
        {
            var converted = (Street)addressPart; 
            findClause = $" streets.full_name LIKE {FormatLike(converted.UntypedName)} "
            + (converted.StreetType == (int)Street.Types.NotMentioned ? ""
            : " AND streets.street_type = " + converted.StreetType.ToString());
            addressLevel = 5;
        }
        if (findClause == "" || addressLevel == 0){
            return null;
        }
        string query = _mainQuery.Replace("{find_clause}", findClause);
        query = query.Replace("{count}", count.ToString());
        Console.WriteLine(query);

        using (var conn = Utils.GetConnectionFactory()){
            conn.Open();
            using (var cmd = new NpgsqlCommand(query, conn)){
                
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows){
                    return null;
                }
                var toReturn = new List<string>();

                while (reader.Read()){
                    string found =  AddressModel.CreateAddressString(
                        FederalSubject.MakeUnsafe((int)reader["subj_id"], (string)reader["subj_name"], (int)reader["subj_type"]),
                        addressLevel > 0 ? District.MakeUnsafe((int)reader["dist_id"], (string)reader["dist_name"], (int)reader["dist_type"]) : null,
                        addressLevel > 1 ? (reader["setarea_id"].GetType() == typeof(DBNull) ? 
                            null :
                            SettlementArea.MakeUnsafe((int)reader["setarea_id"], (string)reader["setarea_name"], (int)reader["setarea_type"])) : null,
                        addressLevel > 2 ? Settlement.MakeUnsafe((int)reader["set_id"], (string)reader["set_name"], (int)reader["set_type"]) : null,
                        addressLevel > 3 ? Street.MakeUnsafe((int)reader["street_id"], (string)reader["street_name"], (int)reader["street_type"]) : null,
                        null, 
                        null
                    );
                    toReturn.Add(found);
                }
                return toReturn;
            }
        }
    }







}


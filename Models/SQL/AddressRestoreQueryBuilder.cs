using Npgsql;
using StudentTracking.Models.Domain.Address;
using Utilities;

namespace StudentTracking.Models.SQL;

public class AddressRestoreQueryBuilder {

    private const string _mainQuery = 
    " SELECT " + 
    " federal_subjects.code AS subj_id," +
    " federal_subjects.subject_type AS subj_type, " +
    " federal_subjects.full_name AS subj_name, " +
    " districts.id AS dist_id, " +
    " districts.full_name AS dist_name, " +
    " districts.district_type AS dist_type, " +
    " settlement_areas.id AS setarea_id, " +
    " settlement_areas.full_name AS setarea_name, " +
    " settlement_areas.settlement_area_type AS setarea_type, " +
    " settlements.id AS set_id, " +
    " settlements.full_name AS set_name, " +
    " settlements.settlement_type AS set_type, " +
    " streets.id AS street_id, " +
    " streets.full_name AS street_name, " +
    " streets.street_type AS street_type " +
    " FROM federal_subjects " +
    " JOIN districts ON districts.federal_subject_code = federal_subjects.code " +
    " LEFT JOIN settlement_areas ON settlement_areas.district = districts.id " +
    " JOIN settlements ON settlements.district = districts.id OR settlement_areas.id " +
    " JOIN streets ON streets.settlements = settlements.id " +
    " WHERE {find_clause} " +
    " LIMIT {count} ";

    public AddressRestoreQueryBuilder(){

    }
    public List<string>? SelectFrom(Type addressPart, string findText, int count){
        string query = _mainQuery;
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

        if(addressPart == typeof(FederalSubject)){
            FederalSubject? prototype = FederalSubject.BuildByName(findText);
            if (prototype == null){
                return null;
            } 
            findClause = $" subj_name LIKE %{prototype.UntypedName}% " 
            + (prototype.SubjectType == (int)FederalSubject.Types.NotMentioned ? "" 
            : " AND subj_name = " + prototype.SubjectType.ToString());
            addressLevel = 1;
        }
        if(addressPart == typeof(District)){
            District? prototype = District.BuildByName(findText);
            if (prototype == null){
                return null;
            } 
            findClause = $" dist_name LIKE %{prototype.UntypedName}% " 
            + (prototype.DistrictType == (int)District.Types.NotMentioned ? "" 
            : " AND dist_type = " + prototype.DistrictType.ToString());
            addressLevel = 2;
        }
        if(addressPart == typeof(SettlementArea)){
            SettlementArea? prototype = SettlementArea.BuildByName(findText);
            if (prototype == null){
                return null;
            } 
            findClause = $" setarea_name LIKE %{prototype.UntypedName}% " 
            + (prototype.SettlementAreaType == (int)SettlementArea.Types.NotMentioned ? "" 
            : " AND setarea_type = " + prototype.SettlementAreaType.ToString());
            addressLevel = 3;
        }
        if(addressPart == typeof(Settlement)){
            Settlement? prototype = Settlement.BuildByName(findText);
            if (prototype == null){
                return null;
            } 
            findClause = $" set_name LIKE %{prototype.UntypedName}% " 
            + (prototype.SettlementType == (int)Settlement.Types.NotMentioned ? "" 
            : " AND set_type = " + prototype.SettlementType.ToString());
            addressLevel = 4;
        }
        if(addressPart == typeof(Street)){
            Street? prototype = Street.BuildByName(findText);
            if (prototype == null){
                return null;
            } 
            findClause = $" street_name LIKE %{prototype.UntypedName}% " 
            + (prototype.StreetType == (int)Street.Types.NotMentioned ? "" 
            : " AND street_type = " + prototype.StreetType.ToString());
            addressLevel = 5;
        }
        if (findClause == "" || addressLevel == 0){
            return null;
        }
        query = _mainQuery.Replace("{find_clause}", findClause);
        query = query.Replace("{count}", count.ToString());

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


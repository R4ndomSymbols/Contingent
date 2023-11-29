using System;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Npgsql;

namespace Utilities;


public static class Utils {

    private static string? DatabaseConnectionString = null;


    public const int INVALID_ID = -1;

    public static string FormatDateTime(DateTime? date){
        var correctDate = date == null ? DateTime.Now : (DateTime)date;

        return string.Format("{0}.{1}.{2}", correctDate.Day, correctDate.Month, correctDate.Year);
    }
    public static DateTime ParseDate(string? date){
        if (date == null){
            throw new ArgumentNullException();
        }
        else{
            var parts = date.Split('.');
            if (parts.Length != 3){
                throw new ArgumentException("Неверный формат даты");
            }
            int day = Convert.ToInt32(parts[0]);
            int month = Convert.ToInt32(parts[1]);
            int year = Convert.ToInt32(parts[2]);
            return new DateTime(year, month, day);
        }
    }
    public static bool TryParseDate(string? date){
        if (date == null){
            return false;
        }
        else{
            var parts = date.Split('.');
            if (parts.Length != 3){
                return false;
            }
            if(int.TryParse(parts[0], out int day)){
                if(int.TryParse(parts[1], out int month)){
                    if(int.TryParse(parts[2], out int year)){
                        try {
                            new DateTime(year, month, day);
                            return true;
                        }
                        catch (ArgumentOutOfRangeException){
                            return false;
                        }
                    }
                    return false;
                }
                return false;
            }
            return false;
        }
    }

    public static string FormatToponymName(string name){

        if (string.IsNullOrEmpty(name)){
            return "";
        }
        string[] split = name.Split(new char[] {' ', '-'});
        if (split.Any(x => x == string.Empty)){
            throw new ArgumentException("Входной топоним не был в правильном формате");
        }
        int point = 0;
        string result = string.Empty;
        for (int i = 0; i < split.Length; i++){
            var newName = string.Concat(split[i][0..1].ToUpper(), split[i].Substring(1).ToLower());
            point+=split[i].Length;
            result+=newName + (point < name.Length ? name[point] : "");
            point++;
        }
        return result; 
    }

    public static async Task<NpgsqlConnection> GetAndOpenConnectionFactory(){
        if (DatabaseConnectionString == null){
            DatabaseConnectionString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build()["ConnectionString"];
        }
        var c = new NpgsqlConnection(DatabaseConnectionString);
        await c.OpenAsync(); 
        return c;
    }
}



const error_postfix = "_err"
const tag_select_postfix = "_tag"
const minimal_tag_id = 1 
var currentTagId = minimal_tag_id;
var addresses = [];
var selectedTags = [];
var tags = [];
var lock = false;
var timeoutPromise

$(document).ready(function () {
    $.ajax({
        type: "GET",
        url: "/students/tags",
        dataType: "JSON",
        success: function (response) {
            $.each(response, function (index, obj) { 
                tags.push(
                    {
                        label: obj["typeName"],
                        value: obj["type"]
                    }
                )
            });
        }
    });
});

function checkTimeout() {
    if (lock){
        return false;
    }
    lock = true;
    timeoutPromise = new Promise(
        (resolve) => setTimeout(
            () => {
                lock = false;
                resolve("resolved")
            }, 1000)
    )
    return true;
}

$("#ActualAddress").on("keyup", function () {
    if (!checkTimeout()){
        return;
    }
    var address = $("#ActualAddress").val();
    if (address.length > 3) {
        $.ajax({
            type: "GET",
            url: "/addresses/suggest/" + address,
            dataType: "JSON",
            success: function (response) {
                $("#ActualAddress").autocomplete({
                    source: response.map(x => address + " " + x)
                });
            }
        });
    }
});
$("#LegalAddress").on("keyup", function () {
    if (!checkTimeout()){
        return;
    }
    var address = $("#LegalAddress").val();
    if (address.length > 3) {
        $.ajax({
            type: "GET",
            url: "/addresses/suggest/" + address,
            dataType: "JSON",
            success: function (response) {
                $("#LegalAddress").autocomplete({
                    source: response.map(x => address + " " + x)
                });
            }
        });
    }
});

$("#about").click(function () {
    var address = $("#ActualAddress").val();
    $.ajax({
        type: "GET",
        url: "/addresses/explain/" + address,
        dataType: "JSON",
        success: function (response) {
            var about = document.getElementById("AboutAddress");
            about.innerHTML = "";
            if (response["addressState"] == undefined){
                errors = response["errors"]
                $.each(errors, function (indexInArray, valueOfElement) { 
                     about.innerHTML += "<br>" + valueOfElement["messageForUser"] + "</br>";
                });
            }
            else{
                about.innerHTML = response["addressState"];
            }
        }
    });
});
$("#about_legal_address").click(function () {
    var address = $("#LegalAddress").val();
    $.ajax({
        type: "GET",
        url: "/addresses/explain/" + address,
        dataType: "JSON",
        success: function (response) {
        var about = document.getElementById("legal_address_info");
            about.innerHTML = "";
        if (response["addressState"] == undefined){
            errors = response["errors"]
            $.each(errors, function (indexInArray, valueOfElement) { 
                    about.innerHTML += "<br>" + valueOfElement["messageForUser"] + "</br>";
            });
        }
        else{
            about.innerHTML = response["addressState"];
        }
    }});
});
$("#add_education_level").click(function () 
{  
    var innerSelect = "";
    var levels = document.getElementById("education_levels");
    currentTagId++
    innerSelect += 
    `
        <div class="d-flex flex-row">
        <select id = ${String(currentTagId)+tag_select_postfix}>
    
    ` 
    $.each(tags, function (index, value) { 
        innerSelect+=
        `
        <option value = "${value.value}">${value.label}</option>
        `     
    });
    innerSelect+="</select></div>";
    levels.innerHTML += innerSelect;
    
});

function getEducationLevels(){
    var resultArray = []
    for (let index = currentTagId; index > minimal_tag_id; index--) {
        resultArray.push(
            {
                Level: Number($("#" + String(index)+tag_select_postfix).val())
            }
        )
        
    }
    return resultArray;

}


$("#save").click(function () {
    var realAddress = $("#ActualAddress").val();
    var legalAddress = $("#LegalAddress").val();
    document.getElementById("ActualAddress_err").innerHTML = "";
    document.getElementById("LegalAddress_err").innerHTML = "";
    var giaMark = $("#GiaMark").val();
    var giaDemMark = $("#GiaDemoExamMark").val();
    var patr = $("#Patronymic").val();

    $.ajax({
        type: "POST",
        url: "/students/addcomplex",
        data: JSON.stringify(
        {
            
            Id: (document.getElementById("StudentId") === null) ? null : Number($("#StudentId").val()),
            GradeBookNumber: $("#GradeBookNumber").val(),
            DateOfBirth: $("#DateOfBirth").val(),
            Gender: Number($("#Gender").val()),
            Snils: $("#Snils").val(),
            Inn: $("#Inn").val(),
            TargetAgreementType: Number($("#TargetAgreementType").val()),
            PaidAgreementType: Number($("#PaidAgreementType").val()),
            AdmissionScore: $("#AdmissionScore").val(),
            GiaMark: giaMark == "" ? null : giaMark,
            GiaDemoExamMark:  giaDemMark == "" ? null : giaDemMark,
            PhysicalAddress: {
                Address : realAddress,
            },
            RusCitizenship:{
                Id: (document.getElementById("RussianCitizenshipId") === null) ? null : Number($("#RussianCitizenshipId").val()),
                Name: $("#Name").val(),
                Surname: $("#Surname").val(),
                Patronymic: patr == "" ? null : patr,
                PassportNumber: $("#PassportNumber").val(),
                PassportSeries: $("#PassportSeries").val(),
                LegalAddress: {
                    Address : legalAddress,
                },
            },
            Education: getEducationLevels()
            
        }),
        dataType: "JSON",
        success: function (response) {
            var realAddressId = response["addressId"];
            var studentId = response["studentId"];
            var legalAddressId = response["addressId"];
            var rusId = response["russianCitizenshipId"];

            if (realAddressId != undefined && studentId!=undefined && legalAddressId!=undefined && rusId != undefined) {
                
                
                $("#AddressId").val(realAddressId);
                $("#StudentId").val(studentId);
                $("#RussianLegalAddressId").val(legalAddressId);
                $("#RussianCitizenshipId").val(rusId);
            }
            else {
                var errors = response["errors"]
                $.each(errors, function (index, value) { 
                    var elem = document.getElementById(value.frontendFieldName + error_postfix);
                    if (elem != null) {
                        
                        elem.innerHTML = value.messageForUser;
                        var parentInput = document.getElementById(value.frontendFieldName);
                        if (parentInput != null) {
                            $("#" + value.frontendFieldName).on("click", function () {
                                elem.innerHTML = "";
                            })
                        }
                    }
                }); 
            }
        }
    });
});





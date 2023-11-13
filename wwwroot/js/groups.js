var specialities = [];
var groupTypes = [];
var educationForms = [];
const protoCourseNumber = [1, 2, 3, 4, 5];
var courseNumbers = [];
var validationZeroLengthIndicator = "";
const names = ["speciality", "course_number", "group_type", "education_form", "creation_year"];
// получение через ajax опций группы

function findAndSetSelectMatch(source, targetSelectName, pathValue = "", pathDisplay = "") {
    var value = $("#" + String(targetSelectName)).attr("asp-rec");
    if (pathValue == "") {
        $.each(source, function (i, item) {
            if (value == String(source[i])) {
                $("#" + String(targetSelectName)).append($(`<option value="${item}" selected>${item}</option>`));
            }
            else {
                $("#" + String(targetSelectName)).append($(`<option value="${item}">${item}</option>`));
            }
        });
    }
    else {
        $.each(source, function (i, item) {
            if (value == String(source[i][pathValue])) {
                $("#" + String(targetSelectName)).append($(`<option value="${item[pathValue]}" selected>${item[pathDisplay]}</option>`));
            }
            else {
                $("#" + String(targetSelectName)).append($(`<option value="${item[pathValue]}">${item[pathDisplay]}</option>`));
            }
        });
    }
}

function getSpecialities() {
    $.ajax({
        url: "/specialities/forgroups",
        type: "GET",
        dataType: "json",
        success: function (data) {
            for (i in data) {
                specialities.push({
                    id: data[i].id,
                    about: data[i].fgosCode + " | " + data[i].qualification,
                    prefix: data[i].mainNamePrefix,
                    postfix: data[i].qualificationPostfix,
                    courseCount: data[i].courseCount,
                });
            }
            findAndSetSelectMatch(specialities, "speciality", "id", "about");
            $("#speciality").trigger("change");
        }
    });
}
$("#speciality").on("change", function () {
    var max = specialities.find((x) => x.id == Number($("#speciality").val())).courseCount;
    courseNumbers = [];
    for (i in protoCourseNumber) {
        if (protoCourseNumber[i] <= max) {
            courseNumbers.push(protoCourseNumber[i]);
        }
    }
    document.getElementById("course_number").innerHTML = "";
    findAndSetSelectMatch(courseNumbers, "course_number");
});


$("#save").on("click", function () {
    $("#creation_year").trigger("change");
    if (validationZeroLengthIndicator.length > 0) {
        alert("Обнаружены ошибки ввода");
    }
    else {
        $.ajax({
            url: "/groups/add",
            type: "POST",
            dataType: "json",
            data: JSON.stringify(
                {
                    id: Number($("#group_id").val()),
                    specialityId: Number($("#speciality").val()),
                    courseNumber: Number($("#course_number").val()),
                    groupTypeId: Number($("#group_type").val()),
                    educationFormId: Number($("#education_form").val()),
                    groupName: document.getElementById("group_name").innerHTML,
                    creationYear: Number($("#creation_year").val()),
                }
            ),
            success: function (data) {
                alert("Группа создана");
            }
        });
    }
});

function getGroupTypes() {
    $.ajax({
        url: "/groups/types",
        type: "GET",
        dataType: "json",
        success: function (data) {
            for (i in data) {
                groupTypes.push({
                    id: data[i].id,
                    name: data[i].name,
                    postfix: data[i].postfix,
                });
            }
            findAndSetSelectMatch(groupTypes, "group_type", "id", "name");
        }
    });
}
function getGroupEduForms() {
    $.ajax({
        url: "/groups/forms",
        type: "GET",
        dataType: "json",
        success: function (data) {
            for (i in data) {
                educationForms.push(
                    {
                        id: data[i].id,
                        name: data[i].name,
                        postfix: data[i].postfix,
                    }
                );
            }
            findAndSetSelectMatch(educationForms, "education_form", "id", "name");
        }
    });
}

getSpecialities();
getGroupEduForms();
getGroupTypes();
setTimeout(function(){updateName(); addNameChangeHandlers();}, 1000)
flexValidation("creation_year", "year_err", "Недопустимый год регистрации", function (value) {
    return (validateNumber(value) && validateRangeLength(4, 4, value));
}, "a");



function addNameChangeHandlers() {
    for (i in names) {
        $("#" + names[i]).on("change", updateName);
    }
}
function updateName() {
    var specialityId = Number($("#speciality").val());
    var selectedSpec = specialities.find((x) => x.id == specialityId);
    var name = "";
    name += selectedSpec.prefix;
    name +="-";
    name += $("#course_number").val();
    
    if (String($("#creation_year").val()).length!=4){
        name+="xx";
    }
    else{
        name+=String($("#creation_year").val()).substring(2);
    }
    name += selectedSpec.postfix;
    name += groupTypes.find((x) => x.id == Number($("#group_type").val())).postfix;
    name += educationForms.find((x) => x.id == Number($("#education_form").val())).postfix;
    document.getElementById("group_name").innerHTML = name;
}





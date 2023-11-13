var specialityTypes = [];
var years = [];
var fgosNames = [];
var fgosCodes = [];
const SPECIALITY_TAGS =
        [
            "fgos_code", "fgos_name", "qualification",
            "group_name_prefix", "group_name_postfix", "course_count",
            "speciality_type"
        ]

function GetFgosCodes(autocompleteCallback) {
    $.ajax({
        url: "/specialities/fgoscodes",
        type: "GET",
        dataType: "json",
        success: function (data) {
            for (i in data) {
                fgosCodes.push(data[i]);
            }
            autocompleteCallback();
        }
    });
}


function setAutoCompleteFgosCodes() {
    $("#fgos_code").autocomplete({
        source: fgosCodes
    });
}

function GetFgosNames(autocompleteCallback) {
    $.ajax({
        url: "/specialities/fgosnames",
        type: "GET",
        dataType: "json",
        success: function (data) {
            for (i in data) {
                fgosNames.push(data[i]);
            }
            autocompleteCallback();
        }
    });
}

function setAutoCompleteFgosNames() {
    $("#fgos_name").autocomplete({
        source: fgosNames
    });
}

GetFgosCodes(setAutoCompleteFgosCodes);
GetFgosNames(setAutoCompleteFgosNames);
GetSpecialityTypes();

const courses = [1, 2, 3, 4, 5];
var selected = Number($("#course_count").attr("value_recieved"));

$.each(courses, function (i, item) {
    if (selected == item) {
        $("#course_count").append($(`<option value="${item}" selected >${item}</option>`));
    }
    else {
        $("#course_count").append($(`<option value="${item}">${item}</option>`));
    }
});

function GetSpecialityTypes() {
    $.ajax({
        url: "/specialities/types",
        type: "GET",
        dataType: "json",
        success: function (data) {
            for (i in data) {
                specialityTypes.push(
                    {
                        id: data[i].id,
                        name: data[i].name,
                    }
                );
            }
            var selectedType = Number($("#speciality_type").attr("value_recieved"));
            $.each(specialityTypes, function (i, item) {
                if (selectedType == item.id) {
                    $("#speciality_type").append($(`<option value="${item.id}" selected>${item.name}</option>`));
                }
                else {
                    $("#speciality_type").append($(`<option value="${item.id}">${item.name}</option>`));
                }
            });
        }
    });
}


$("#save").on("click", function () {
    invokeAllValidation(SPECIALITY_TAGS);
    if (validationZeroLengthIndicator.length > 0) {
        alert("Обнаружены ошибки ввода");
    }
    else {
        $.ajax({
            url: "/specialities/add",
            type: "POST",
            dataType: "json",
            data: JSON.stringify(
                {
                    id: Number($("#speciality_id").val()),
                    fgosCode: $("#fgos_code").val(),
                    fgosName: $("#fgos_name").val(),
                    qualification: $("#qualification").val(),
                    mainNamePrefix: $("#group_name_prefix").val(),
                    qualificationPostfix: $("#group_name_postfix").val(),
                    courseCount: Number($("#course_count").val()),
                    specialityTypeId: Number($("#speciality_type").val()),

                }
            ),
            success: function (data) {
                alert("Специальность создана");
            }
        });
    }
});





flexValidation("fgos_code", "code_err", "Неверно указан номер ФГОС", function (value) {
    return validateFGOS(value);
}, "a");
flexValidation("fgos_name", "name_err", "Неверно указано название ФГОС", function (value) {
    return (validateWordsOnly(value) && validateRangeLength(1, 200, value));
}, "b");
flexValidation("qualification", "qualification_err", "Неверно указана квалификация", function (value) {
    return (validateWordsOnly(value) && validateRangeLength(1, 200, value));
}, "c");
flexValidation("group_name_prefix", "prefix_err", "Неверно указан префикс", function (value) {
    return (validateLetters(value) && validateRangeLength(1, 10, value));
}, "d");
flexValidation("group_name_postfix", "postfix_err", "Неверно указан постфикс", function (value) {
    return (validateLetters(value) && validateRangeLength(1, 10, value) || function (value) { return (value == "" || value == undefined) });
}, "e");
flexValidation("course_count", "course_err", "Не выбрана длительность обучения", function (value) {
    return validateNumber(value);
}, "f");
flexValidation("speciality_type", "speciality_err", "Не выбран тип специальности", function (value) {
    return validateNumber(value);
}, "g");

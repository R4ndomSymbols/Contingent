var countryRegions = [];
var regions = [];
var localites = [];
var streets = [];
var buildings = [];
var genders = [];
const STUDENT_TAGS =
        [
            "grade_book", "p_series", "locality_id",
            "name", "snils", "street_id",
            "surname", "inn", "building_id",
            "patronymic", "average",
            "date_of_birth", "country_region_code",
            "p_number", "region_id"
        ]

$(document).ready(function () {
    getAndSelectGenders();
    updateCountryRegions(setAutocompleteCountryRegions);
    var id = $("#student_id").val();
    if (Number(id) != -1){
        updateRegions(setAutocompleteRegions);
        updateLocalities(setAutocompleteLocalities);
        updateStreets(setAutocompleteStreets);
        updateBuildings(setAutocompleteBuildings);
    }
    else{
        mainAddressInputControl(4);
    } 

    

});

flexValidation("grade_book", "grade_book_err",
    "Не менее 2 цифр", function (value) {
    return validateNumber(value) && validateRangeLength(2, 4, value);
}, "a");
flexValidation("name", "name_err",
    "Не менее 1 и не более 60 букв", function (value) {
    return validateLetters(value) && validateRangeLength(1, 60, value);
}, "b");
flexValidation("surname", "surname_err",
    "Не менее 1 и не более 100 букв", function (value) {
    return validateLetters(value) && validateRangeLength(1, 100, value);
}, "c");
flexValidation("patronymic", "patronymic_err",
    "Не менее 1 и не более 100 букв", function (value) {
    return ((validateLetters(value) && validateRangeLength(1, 100, value)) || !validateNotEmpty(value));
}, "d");
// переполнение не пофикшено
flexValidation("date_of_birth", "data_err",
    "Неверный формат даты (YYYY-MM-DD)", function (value) {
    return validateDate(value) && validateNotEmpty(4, 4, value);
}, "f");
flexValidation("p_number", "p_number_err",
    "Неверный формат номера паспорта", function (value) {
    return validateNumber(value) && validateRangeLength(4, 4, value);
}, "g");
flexValidation("p_series", "p_series_err",
    "Неверный формат серии паспорта", function (value) {
    return validateNumber(value) && validateRangeLength(6, 6, value);
}, "h");
flexValidation("snils", "snils_err",
    "Неверный формат СНИЛС", function (value) {
    return validateNumber(value) && validateRangeLength(11, 11, value);
}, "j");
flexValidation("inn", "inn_err",
    "Неверный формат ИНН", function (value) {
    return validateNumber(value) && validateRangeLength(12, 12, value);
}, "k");
flexValidation("average", "average_err",
    "Недопустимый вступительный балл", function (value) {
    return validateValue(3, 5, value);
}, "l");
flexValidation("country_region_code", "address_error",
    "Недопустимый вступительный балл", function (value) {
    return validateNonNegative(value);
}, "m");
flexValidation("region_id", "address_error",
    "Не указана либо указана неверно часть адреса", function (value) {
    return validateNonNegative(value);
}, "n");
flexValidation("locality_id", "address_error",
    "Не указана либо указана неверно часть адреса", function (value) {
    return validateNonNegative(value);
}, "o");
flexValidation("street_id", "address_error",
    "Не указана либо указана неверно часть адреса", function (value) {
    return validateNonNegative(value);;
}, "p");
flexValidation("building_id", "address_error",
    "Не указана либо указана неверно часть адреса", function (value) {
    return validateNonNegative(value);
}, "q");

$("#add_country_region").on("click", function () {
    $.ajax({
        url: "/addresses/new/federal",
        type: "POST",
        dataType: "json",
        data: JSON.stringify({
            countryRegionName: $(("#country_region")).val(),
            id: Number($("#country_region_code").val()),
        }),
        error: function (data) {
            countryRegions.push({
                name: $(("#country_region")).val(),
                id: Number($("#country_region_code").val()),
            });
            $("#country_region").trigger("change");
        }
    });
});
$("#add_region").on("click", function () {
    $.ajax({
        url: "/addresses/new/region",
        type: "POST",
        dataType: "json",
        data: JSON.stringify({
            id: -1,
            regionName: $(("#region")).val(),
            parent: Number($("#country_region_code").val()),
        }),
        success: function (data) {
            regions.push({
                name: $(("#region")).val(),
                id: Number(data.id),
            });
            $("#region").trigger("change");
        }
    });
});
$("#add_locality").on("click", function () {
    $.ajax({
        url: "/addresses/new/locality",
        type: "POST",
        dataType: "json",
        data: JSON.stringify({
            id: -1,
            parent: Number($("#region_id").val()),
            localityName: $(("#locality")).val(),
        }),
        success: function (data) {
            localites.push({
                name: $(("#locality")).val(),
                id: Number(data.id),
            });
            $("#locality").trigger("change");
        }
    });
});
$("#add_street").on("click", function () {
    $.ajax({
        url: "/addresses/new/street",
        type: "POST",
        dataType: "json",
        data: JSON.stringify({
            id: -1,
            parent: Number($("#locality_id").val()),
            streetName: $(("#street")).val(),
        }),
        success: function (data) {
            streets.push({
                name: $(("#street")).val(),
                id: Number(data.id),
            });
            $("#street").trigger("change");
        }
    });
});
$("#add_building").on("click", function () {
    $.ajax({
        url: "/addresses/new/building",
        type: "POST",
        dataType: "json",
        data: JSON.stringify({
            id: -1,
            parent: Number($("#street_id").val()),
            buildingName: $(("#building")).val(),
        }),
        success: function (data) {
            buildings.push({
                name: $(("#building")).val(),
                id: Number(data.id),
            });
            $("#building").trigger("change");
        }
    });
});

function updateCountryRegions(callback) {
    $.ajax({
        url: "/addresses/federal",
        dataType: "json",
        success: function (data) {
            countryRegions = [];
            for (i in data) {
                countryRegions.push(
                    {
                        id: data[i].id,
                        name: data[i].countryRegionName
                    });
            }
            callback();
        }
    });
};
function updateRegions(callback) {
    $.ajax({
        url: "/addresses/regions/" + String($("#country_region_code").val()),
        dataType: "json",
        success: function (data) {
            regions = [];
            for (i in data) {
                regions.push(
                    {
                        id: data[i].id,
                        name: data[i].regionName
                    });
            }
            callback();
        }
    });
};
function updateLocalities(callback) {
    $.ajax({
        url: "/addresses/localities/" + String($("#region_id").val()),
        dataType: "json",
        success: function (data) {
            localites = [];
            for (i in data) {
                localites.push(
                    {
                        id: data[i].id,
                        name: data[i].localityName
                    });
            }
            callback();
        }
    });
};
function updateStreets(callback) {
    $.ajax({
        url: "/addresses/streets/" + String($("#locality_id").val()),
        dataType: "json",
        success: function (data) {
            streets = [];
            for (i in data) {
                streets.push(
                    {
                        id: data[i].id,
                        name: data[i].streetName
                    });
            }
            callback();
        }
    });
};
function updateBuildings(callback) {
    $.ajax({
        url: "/addresses/buildings/" + String($("#street_id").val()),
        dataType: "json",
        success: function (data) {
            buildings = [];
            for (i in data) {
                buildings.push(
                    {
                        id: data[i].id,
                        name: data[i].buildingName
                    });
            }
            callback();
        }
    });
};


function mainAddressInputControl(level) {
    // дом 0
    if (level >= 0) {
        $("#building").val("");
        $("#building_id").val("");
        $("#building").prop("disabled", true);
        $("#add_building").prop("disabled", true);
    }
    // улица 1
    if (level >= 1) {
        $("#street").val("");
        $("#street_id").val("");
        $("#street").prop("disabled", true);
        $("#add_street").prop("disabled", true);
    }
    // город 2
    if (level >= 2) {
        $("#locality").val("");
        $("#locality_id").val("");
        $("#locality").prop("disabled", true);
        $("#add_locality").prop("disabled", true);
    }
    // регион 3
    if (level >= 3) {
        $("#region").val("");
        $("#region_id").val("");
        $("#region").prop("disabled", true);
        $("#add_region").prop("disabled", true);
    }
    // область
    if (level >= 4) {
        $("#country_region").val("");
        $("#country_region_code").val("");
        $("#add_country_region").prop("disabled", true);
    }
};

function setAutocompleteCountryRegions() {
    $("#country_region").autocomplete({
        source: countryRegions.map((x) => x.name)
    });
};

function setAutocompleteRegions() {
    $("#region").autocomplete({
        source: regions.map((x) => x.name)
    });
};
function setAutocompleteLocalities() {
    $("#locality").autocomplete({
        source: localites.map((x) => x.name)
    });
};
function setAutocompleteStreets() {
    $("#street").autocomplete({
        source: streets.map((x) => x.name)
    });
};
function setAutocompleteBuildings() {
    $("#building").autocomplete({
        source: buildings.map((x) => x.name)
    });
};
$("#country_region_code").on("change", function () {

    if (validateWordsOnly($("#country_region").val()) && validateNumber($("#country_region_code").val())) {
        var currentCode = Number($("#country_region_code").val());
        for (i in countryRegions) {
            if (countryRegions[i].id == currentCode) {
                $("#add_country_region").prop("disabled", true);
                return;
            }
        }
        $("#add_country_region").prop("disabled", false);
    }
});

$("#country_region").on("change", function () {
    var current = $("#country_region").val().trim();
    $("#country_region").val(current);
    mainAddressInputControl(3);
    for (i in countryRegions) {
        if (countryRegions[i].name === current) {
            $("#country_region_code").val(countryRegions[i].id);
            $("#country_region_code").prop("disabled", true);
            $("#add_country_region").prop("disabled", true);
            $("#region").prop("disabled", false);
            updateRegions(setAutocompleteRegions);
            return;
        }
    }
    if (validateWordsOnly(current) && validateNumber($("#country_region_code").val())) {
        $("#add_country_region").prop("disabled", false);
        $("#country_region_code").prop("disabled", false);
    }
});

$("#region").on("change", function () {
    var current = $("#region").val().trim();
    $("#region").val(current);
    mainAddressInputControl(2);
    for (i in regions) {
        if (regions[i].name === current) {
            $("#region_id").val(regions[i].id);
            $("#add_region").prop("disabled", true);
            $("#locality").prop("disabled", false);
            updateLocalities(setAutocompleteLocalities);
            return;
        }
    }
    if (validateWordsOnly(current)) {
        $("#add_region").prop("disabled", false);
    }
});
$("#locality").on("change", function () {
    var current = $("#locality").val().trim();
    $("#locality").val(current);
    mainAddressInputControl(1);
    for (i in localites) {
        if (localites[i].name === current) {
            $("#locality_id").val(localites[i].id);
            $("#add_locality").prop("disabled", true);
            $("#street").prop("disabled", false);
            updateStreets(setAutocompleteStreets);
            return;
        }
    }
    if (validateWordsOnly(current)) {
        $("#add_locality").prop("disabled", false);
    }

});
$("#street").on("change", function () {
    var current = $("#street").val().trim();
    $("#street").val(current);
    mainAddressInputControl(0);
    for (i in streets) {
        if (streets[i].name === current) {
            $("#street_id").val(streets[i].id);
            $("#add_street").prop("disabled", true);
            $("#building").prop("disabled", false);
            updateBuildings(setAutocompleteBuildings);
            return;
        }
    }
    if (validateWordsOnly(current)) {
        $("#add_street").prop("disabled", false);
    }
});
$("#building").on("change", function () {
    var current = $("#building").val().trim();
    $("#building").val(current);
    for (i in buildings) {
        if (buildings[i].name === current) {
            $("#building_id").val(buildings[i].id);
            $("#add_building").prop("disabled", true);
            return;
        }
    }
    if (validateWordsAndNumbersOnly(current)) {
        $("#add_building").prop("disabled", false);
    }
});

$("#gender").on("change", function () {
    var current = $("#gender").val();
    for (i in genders) {
        if (genders[i].name === current) {
            $("#gender_id").val(genders[i].id);
            return;
        }
    }
});

function getAndSelectGenders() {
    $.ajax({
        type: "GET",
        url: "/student/genders",
        dataType: "json",
        success: function (data) {
            genders = [];
            for (i in data) {
                genders.push({
                    id: data[i].id,
                    name: data[i].fullName
                });
            }
            $.each(genders, function (i, item) {
                $("#gender").append($(`<option value="${item.id}">${item.name}</option>`));
                if (item.id == $("#gender_hint").val()){
                    $("#gender").val(item.id);
                }
            });
        }
    }
    )
}


if ($("#student_id").val() == undefined || $("#student_id").val() == "") {
    $("#student_id").val("-1");
}

$("#save").click(function () {
    invokeAllValidation(STUDENT_TAGS);
    if (validationZeroLengthIndicator.length > 0) {
        alert("Обнаружены ошибки валидации: " + validationZeroLengthIndicator);
        return;
    }
    else {
        $.ajax({
            type: "POST",
            url: "/addresses/new",
            dataType: "json",
            data: JSON.stringify(
                {
                    federalCode: Number($("#country_region_code").val()),
                    regionId: Number($("#region_id").val()),
                    localityId: Number($("#locality_id").val()),
                    streetId: Number($("#street_id").val()),
                    buildingId: Number($("#building_id").val())
                }),
            success: function (data) {
                $("#address_id").val(data.addressId);
                $.ajax({
                    type: "POST",
                    url: "/students/add",
                    dataType: "json",
                    data: JSON.stringify(
                        {
                            id: Number($("#student_id").val()),
                            gradeBook: $("#grade_book").val(),
                            name: $("#name").val(),
                            surname: $("#surname").val(),
                            patronymic: $("#patronymic").val(),
                            dateOfBirth: $("#date_of_birth").val(),
                            gender: $("#gender").val(),
                            passportNumber: $("#p_number").val(),
                            passportSeries: $("#p_series").val(),
                            snils: $("#snils").val(),
                            inn: $("#inn").val(),
                            addressId: Number($("#address_id").val()),
                            admissionScore: Number(Number($("#average").val()).toFixed(2)),
                        }),
                    error: function (data) {
                        alert("Студент занесен в базу");
                    }
                });
            }
        });
    }
});




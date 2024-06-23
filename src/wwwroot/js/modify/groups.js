import { Utilities } from "../site.js";

// общее 
// индикатор создания группы / потока
let created = false;
let utils = new Utilities();

// секция для ручного ввода
let ancestor = undefined;
let availableGroups = [];
let selectedSpecialtyNoGen = undefined;

init();

function init() {
    $("#AutogenerateName").on("click", function () {
        onGenerationChange(Boolean(document.getElementById("AutogenerateName").checked));
    });
    $("#AutogenerateName").trigger("click");
}

function onGenerationChange(checkBoxValue) {
    if (checkBoxValue) {
        terminateNoGen();
        makeAutoGenReady();
    }
    else {
        terminateAutoGen();
        makeNoGenReady();
    }

}

function saveCommon(jsonObject) {

    if (created) {
        alert("Обновление не предусмотрено, обновите страницу")
        return;
    }

    $.ajax({
        type: "POST",
        url: "/groups/add",
        data: JSON.stringify(jsonObject),
        dataType: "JSON",
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            utils.notifySuccess();
            created = true
        },
        error: function (response, a, b) {
            utils.readAndSetErrors(response, undefined, utils.SELECTOR_CLASS)
        }
    });
}

function findSpecialtiesCommon(searchText, callback = undefined) {
    if (searchText.length < 2) {
        if (callback !== undefined) {
            callback([]);
        }
        return;
    }

    $.ajax({
        type: "POST",
        url: "/specialties/search/find",
        data: JSON.stringify({
            SearchString: searchText
        }),
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            if (callback !== undefined) {
                callback(response)
            }
        }
    });
}

// секция для автоматического ввода
let selectedSpecialtyAutoGen = undefined;

function makeAutoGenReady() {
    showAutoGen();
    $(".data_changer").blur(
        function () {
            utils.registerScheduledQuery(() => updateName());
        }
    );
    $(".data_changer").on("change",
        function () {
            utils.registerScheduledQuery(() => updateName());
        }
    );
    $("#specialty_input_auto_gen").on("input", function () {
        utils.registerScheduledQuery(() => findSpecialtiesAutoGen(), 2);
    })
    $("#save").on("click", function () {
        saveAutoGen();
    });
}

function terminateAutoGen() {
    $("#specialty_input_auto_gen").off("keyup");
    $("#save").off("click");
    $(".data_changer").off();
}

function showAutoGen() {
    $("#auto_gen").removeClass("hidden-element");
    $("#name_display").removeClass("hidden-element");
    $("#manual_input").addClass("hidden-element");
}
// обновление имени группы при изменении важных параметров
function updateName() {
    if (selectedSpecialtyAutoGen === undefined) {
        return;
    }
    $.ajax({
        type: "POST",
        url: "/groups/getname",
        data: JSON.stringify(createJSONAutoGen()),
        dataType: "JSON",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            document.getElementById("names_generated").innerText = response["groupName"];
        }
    });
}

function saveAutoGen() {
    saveCommon(createJSONAutoGen());
}


function findSpecialtiesAutoGen() {
    // тут специальность можно выбрать всегда
    let callback = (specialties) => $("#specialty_input_auto_gen").autocomplete({
        delay: 100,
        source: specialties.map(
            x => {
                return {
                    value: x.id,
                    label: x.fgosCode + " " + x.fgosName + " (" + x.qualificationName + ")"
                }
            }
        ),
        change: function (event, ui) {
            if (ui.item === null) {
                return;
            }
            selectedSpecialtyAutoGen = specialties.find(x => x.id == ui.item.value)
            updateName();
        },
        select: function (event, ui) {
            $(this).prop("value", ui.item.label)
            event.preventDefault();
        }
    });
    findSpecialtiesCommon($("#specialty_input_auto_gen").val(), callback);

}

function createJSONAutoGen() {
    return {
        EduProgramId: selectedSpecialtyAutoGen === undefined ? 0 : selectedSpecialtyAutoGen.id,
        EduFormatCode: Number($("#auto_gen_education_form").val()),
        SponsorshipTypeCode: Number($("#auto_gen_financing_type").val()),
        CreationYear: $("#auto_gen_creation_year").val(),
        AutogenerateName: true,
        CourseOn: 1,
    }
}







function makeNoGenReady() {
    showNoGen();
    $("#ancestor_group_input").on("input", function () {
        utils.registerScheduledQuery(
            () => findGroupsNoGen(
                changeUiAndLogicOnInputGroup
            ));
    });
    $("#save").on("click", function () {
        saveNoGen();
    });
}
function terminateNoGen() {
    ancestor = undefined;
    availableGroups = [];
    $("#ancestor_group_input").off("keyup");
    $("#save").off("click");
}



function showNoGen() {
    $("#auto_gen").addClass("hidden-element");
    $("#name_display").addClass("hidden-element");
    $("#manual_input").removeClass("hidden-element");
}

function changeUiAndLogicOnInputGroup() {

    // указана ли группа в данный момент
    let valueNow = $("#ancestor_group_input").val()
    console.log(valueNow)
    if (valueNow === undefined || valueNow == "") {
        // в случае, если групп не указана, то нужно включить эти поля
        enableInput("creation_year_no_gen")
        enableInput("specialty_input_no_gen")
        enableInput("no_gen_financing_type")
        enableInput("no_gen_education_form")
        $("#specialty_input_no_gen").on("input", function () {
            utils.registerScheduledQuery(
                function () {
                    findSpecialtiesNoGen()
                }, 3
            )
        })
        return;
    }
    else {
        $("#specialty_input_no_gen").off()
        disableInput("creation_year_no_gen")
        disableInput("specialty_input_no_gen")
        disableInput("no_gen_financing_type")
        disableInput("no_gen_education_form")
    }

    function disableInput(inputId) {
        $("#" + inputId).attr("disabled", "disabled")
    }
    function enableInput(inputId) {
        $("#" + inputId).removeAttr("disabled")
    }

}

function findGroupsNoGen(callback) {
    $.ajax({
        type: "POST",
        url: "/groups/search/find",
        data: JSON.stringify({
            GroupName: $("#ancestor_group_input").val(),
            IsActive: true
        }),
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            availableGroups = response;
            $("#ancestor_group_input").autocomplete({
                delay: 100,
                source: availableGroups.map(
                    x => {
                        return {
                            label: x.groupName,
                            value: x.groupId
                        }
                    }
                ),
                change: function (event, ui) {
                    if (ui.item === null) {
                        return;
                    }
                    ancestor = availableGroups.find(x => x.groupId == ui.item.value)
                },
                select: function (event, ui) {
                    $(this).prop("value", ui.item.label)
                    event.preventDefault();
                },
            })
        },
        complete: function (data) {
            callback();
        }
    });
}

function findSpecialtiesNoGen() {
    if (ancestor === undefined) {
        findSpecialtiesCommon(
            $("#specialty_input_no_gen").val(),
            (specialties) => {
                $("#specialty_input_no_gen").autocomplete({
                    delay: 100,
                    source: specialties.map(
                        x => {
                            return {
                                value: x.id,
                                label: x.fgosCode + " " + x.fgosName + " (" + x.qualificationName + ")"
                            }
                        }
                    ),
                    change: function (event, ui) {
                        selectedSpecialtyNoGen = specialties.find(x => x == ui.item.value)
                    },
                    select: function (event, ui) {
                        $(this).prop("value", ui.item.label)
                        event.preventDefault();
                    }
                })
            }
        )

    }
}

function saveNoGen() {
    saveCommon(createJsonNoGen())
}

function createJsonNoGen() {
    let json = undefined
    if (ancestor === undefined) {
        json = {
            EduProgramId: selectedSpecialtyNoGen === undefined ? 0 : selectedSpecialtyNoGen.id,
            EduFormatCode: $("#no_gen_education_form").val(),
            SponsorshipTypeCode: $("#no_gen_financing_type").val(),
            CreationYear: $("#creation_year_no_gen").val(),
            AutogenerateName: false,
            GroupName: $("#group_name_no_gen").val(),
            CourseOn: 1,
            PreviousGroupId: 0
        }
    }
    else {
        json = {
            EduProgramId: ancestor.specialtyId,
            EduFormatCode: ancestor.eduFormatCode,
            SponsorshipTypeCode: ancestor.sponsorshipTypeCode,
            CreationYear: String(ancestor.creationYear),
            AutogenerateName: false,
            GroupName: $("#group_name_no_gen").val(),
            CourseOn: Number(ancestor.courseOn) + 1,
            PreviousGroupId: ancestor.groupId
        }
    }
    console.log(ancestor)
    return json;
}




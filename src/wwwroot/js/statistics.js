import { Utilities } from "./site.js";
let utils = new Utilities();

init();

function init() {
    $("#load").on("click", function () {
        loadTable();
    });
    $(".error_reset").on("click", function () {
        $("#STATISTIC_ERROR_err").empty();
    });
}

function loadTable() {
    let selected = $("#statistic_type").find(":selected");
    let type = String(selected.attr("value"));
    let tableName = String(selected.attr("table_name"));
    $.ajax({
        type: "POST",
        url: "/statistics/table/" + tableName,
        data: getData(type),
        dataType: "html",
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            $("#table_view").empty();
            $("#table_view").html(response);
        },
        error: function (xhr, a, b) {
            utils.readAndSetErrors(xhr)
        }
    });
}

function getData(periodType) {
    var startDate = $("#start_date").val();
    var endDate = $("#end_date").val();
    switch (periodType) {
        case "end_only":
            return JSON.stringify(
                {
                    EndDate: endDate
                }
            );
        case "both_dates":
            return JSON.stringify(
                {
                    StartDate: startDate === "" ? null : startDate,
                    EndDate: endDate
                }
            );
    }
    return null;
}



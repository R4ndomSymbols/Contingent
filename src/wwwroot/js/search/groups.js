import { Utilities } from "../site.js";
let utils = new Utilities();
let resultTable = undefined;

init();

function init() {
    resultTable = $("#groups_found");
    $("#search_button").on("click", function () {
        resetSearch();
        $.ajax({
            type: "POST",
            url: "/groups/search/find",
            data: getSearchGroupData(),
            contentType: "application/json",
            beforeSend: utils.setAuthHeader,
            success: function (response) {
                $.each(response, function (indexInArray, valueOfElement) {
                    addCard(valueOfElement)
                });
            }
        });
    });
}

function getSearchGroupData() {
    return JSON.stringify(
        {
            GroupName: $("#name_input").val(),
            OnlyActive: $("#active_only").is(":checked")
        }
    )
}

function resetSearch() {
    resultTable.empty();
}

function addCard(groupModel) {
    resultTable.append(
        `
        <tr id = "${groupModel.groupId}">
            <td>${groupModel.groupName}</td>
            <td>${groupModel.courseOn}</td>
            <td><a href="${groupModel.linkToView}">Детально</a></td>
        </tr>
        `
    );
}
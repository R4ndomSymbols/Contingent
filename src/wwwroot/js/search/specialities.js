$(document).ready(function () {
    $("#search_button").on("click", function () {
        $.ajax({
            type: "POST",
            url: "/specialities/search/query",
            data: getSearchData(),
            contentType: "application/json",
            success: function (response) {
                $("#search_result").empty();
                $.each(response, function (indexInArray, valueOfElement) {
                    appendFound(valueOfElement)
                });
            }
        });

    })
});


function getSearchData() {
    let query = {
        SearchString: $("#search_text").val()
    }
    return query
}

function appendFound(specialty) {
    $("#search_result").append(
        `
        <tr id="${specialty.id}">
            <td>${specialty.fgosCode}</td>
            <td>${specialty.fgosName}</td>
            <td>${specialty.qualificationName}</td>
            <td>
                <div class="d-flex flex-column">
                    <a href="${specialty.linkToModify}">Изменить</a>
                    <a href="${specialty.linkToView}">Детально</a>
                </div>
            <td>
        </tr>
        `
    );
}






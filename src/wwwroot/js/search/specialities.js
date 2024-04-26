$("#search_button").on("click", function () 
{  
    $.ajax({
        type: "POST",
        url: "/specialities/search/query",
        data: JSON.stringify(getSearchData()),
        contentType: "application/json",
        success: function (response) {
            $("#search_result").empty();
            $.each(response, function (indexInArray, valueOfElement) { 
                appendFound(valueOfElement)
            });
        }
    });

})

function getSearchData(){
    let query = {
        SearchString: $("#search_text").val()
    }
    return query
}

function appendFound(speciality){
    $("#search_result").append(
        `
        <tr id="${speciality.id}">
            <td>${speciality.fgosCode}</td>
            <td>${speciality.fgosName}</td>
            <td>${speciality.qualificationName}</td>
            <td>
                <div class="d-flex flex-column">
                    <a href="${speciality.linkToModify}">Изменить</a>
                    <a href="${speciality.linkToView}">Детально</a>
                </div>
            <td>
        </tr>
        `
    );
}






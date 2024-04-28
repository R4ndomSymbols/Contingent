const error_postfix = "_err";
function setErrors(response) {
    response = response.errors;
    $.each(response, function (index, value) {
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

function setErrorsByClass(response) {
    parsedResponse = response.responseJSON.Errors;
    $.each(parsedResponse, function (index, value) {
        $("." + value.FrontendFieldName + error_postfix).append(value.MessageForUser);
        $("." +value.FrontendFieldName).one("click", function () {
            $("." + value.FrontendFieldName + error_postfix).empty()
        });
    });
}


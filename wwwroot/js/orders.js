var orderTypes = [];
const ORDER_TAGS = ["effective_date", "serial_number", "order_description"];
function getAndSetOrderTypes() {
    $.ajax({
        type: "GET",
        url: "/orders/types",
        dataType: "json",
        success: function (data) {
            for (i in data) {
                orderTypes.push({
                    id: data[i].id,
                    name: data[i].name,
                    postfix: data[i].postfix
                })
            }
            var selectedFromModel = $("#order_type").attr("asp-rec");
            for (i in orderTypes) {
                if (orderTypes[i].id == selectedFromModel) {
                    $("#order_type").append(`<option value="${orderTypes[i].id}" selected>${orderTypes[i].name}</option>`);
                }
                else {
                    $("#order_type").append(`<option value="${orderTypes[i].id}">${orderTypes[i].name}</option>`);
                }
            }
        }
    });
}
$("#save").on("click", function () {
    invokeAllValidation(ORDER_TAGS);
    if (validationZeroLengthIndicator.length > 0) {
        alert("Обнаружены ошибки валидации" + validationZeroLengthIndicator);
    }
    else {
        $.ajax({
            type: "POST",
            url: "/orders/save",
            data: JSON.stringify({
                id: Number($("#order_id").val()),
                effectiveDate: $("#effective_date").val(),
                orderName: document.getElementById("order_name").innerHTML,
                orderDescription: $("#order_description").val(),
                serialNumber: Number($("#serial_number").val()),
                orderType: Number($("#order_type").val()),
            }),
            dataType: "json",
            success: function (response) {
                alert("Приказ успешно создан");
            }
        });
    }
});


function setName() {
    if (validationZeroLengthIndicator.length == 0) {
        var name = "";
        name += String($("#effective_date").val()).substring(2, 4);
        var selectedOrderTypeId = $("#order_type").val();
        name += orderTypes.find((x) => x.id == selectedOrderTypeId).postfix;
        name += $("#serial_number").val();
        document.getElementById("order_name").innerHTML = name;
    }
}

getAndSetOrderTypes();
setTimeout(setName, 500);

flexValidation("effective_date", "effective_date_err", "Неверный формат даты", function (value) {
    return validateDate(value);
}, "a", setName);
flexValidation("serial_number", "serial_number_err", "Неверный формат серийного номера", function (value) {
    return (validateNumber(value) && validateRangeLength(1, 5, value));
}, "b", setName);
flexValidation("order_description", "order_description_err", "Использование недопустимых символов", function (value) {
    return (!validateNotEmpty(value) || validateWordsAndNumbersOnly(value));
}, "c", setName);

$("#order_type").on("change", function () {
    setName();
});
$(document).ready(function () {
    $(".identity_dependency").blur(
        function(){
            updateIdentity();
        }
    );
});

function updateIdentity(){

    $.ajax({
        type: "POST",
        url: "/orders/generateIdentity",
        data: JSON.stringify(
            {
                SpecifiedDate: $("#SpecifiedDate").val(),
                EffectiveDate: $("#EffectiveDate").val(),
                OrderType: Number($("#OrderType").val()),
                OrderDisplayedName: $("#OrderDisplayedName").val(),
                OrderDescription: $("#OrderDescription").val() == "" ? null : $("#OrderDescription").val() 
            }
        ),
        contentType: "application/json",
        success: function (response) {
            document.getElementById("OrderOrgId").innerText = response["orderOrgId"];
        },
        error: function(xhr, textStatus, errThrown){
            setErrors(JSON.parse(xhr.responseText));
        }
    });
    
}

$("#save").click(function () {
    $.ajax({
        type: "POST",
        url: "/orders/save",
        data: JSON.stringify(
        {
            SpecifiedDate: $("#SpecifiedDate").val(),
            EffectiveDate: $("#EffectiveDate").val(),
            OrderType: Number($("#OrderType").val()),
            OrderDisplayedName: $("#OrderDisplayedName").val(),
            OrderDescription: $("#OrderDescription").val() == "" ? null : $("#OrderDescription").val() 

        }),
        dataType: "JSON",
        success: function (response) {
            $("#OrderId").attr("order_id",  response["orderId"]);
            alert("Приказ успешно сохранен")
        },
        error: function(xhr, textStatus, errThrown){
            setErrors(JSON.parse(xhr.responseText));
        }
    });
});




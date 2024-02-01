$(document).ready(function () {
    specialities = [];
    $.ajax({
        type: "GET",
        url: "/specialities/suggest",
        dataType: "JSON",
        success: function (response) {
            $.each(response, function (index, valueObj) { 
                 specialities.push({
                    value: valueObj["id"],
                    label: valueObj["fullName"]
                 })
            });
            $("#edu_program_input").autocomplete({
                delay: 100,
                source: specialities,
                change: function (event, ui){
                    $("#EducationalProgramId").prop("value", ui.item.value)
                },
                select: function( event, ui ) {
                    $("#edu_program_input").prop("value", ui.item.label)
                    event.preventDefault();
                }
            })
        }
    });

    $(".data_changer").blur(
        function(){
            updateName();
        }
    );

    $("#AutogenerateName").on("click", function() {
        var generateName = Boolean(document.getElementById("AutogenerateName").checked);
        var element = document.getElementById("group_name_panel");
        if (generateName){
            element.innerHTML = "<h5 id=\"GroupName\"></h5>" 
        }
        else{
            element.innerHTML = "<input id=\"GroupName\"/>"
        }
    });

});

function updateName(){

    var generateName = Boolean(document.getElementById("AutogenerateName").checked);

    if (!generateName){
        return;
    }
    $.ajax({
        type: "POST",
        url: "/groups/getname",
        data: JSON.stringify(
            {
                EduProgramId: Number($("#EducationalProgramId").prop("value")),
                EduFormatCode: Number($("#EducationalForm").val()),
                SponsorshipTypeCode: Number($("#SponsorshipType").val()),
                CreationYear: $("#CreationYear").val(),
                AutogenerateName: true, 
            }
        ),
        dataType: "JSON",
        success: function (response) {
            document.getElementById("GroupName").innerText = response["groupName"];
        }
    });
}

$("#save").click(function () {
    $.ajax({
        type: "POST",
        url: "/groups/addsequence",
        data: JSON.stringify(
        {
            EduProgramId: Number($("#EducationalProgramId").prop("value")),
            EduFormatCode: Number($("#EducationalForm").val()),
            SponsorshipTypeCode: Number($("#SponsorshipType").val()),
            CreationYear: $("#CreationYear").val(),
            AutogenerateName: Boolean(document.getElementById("AutogenerateName").checked),
            GroupName: $("#GroupName").val(),

        }),
        dataType: "JSON",
        contentType: "application/json",
        success: function (response) {
            var groupId = response["groupId"];

            if (groupId != undefined) {
                $("#GroupId").val(groupId);
            }
            else {
                setErrors(response);
            }
        }
    });
});




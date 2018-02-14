var TransfereeIntakeService = function() {
    var route = "/api/orders/transferee/";

    var updateIntakeBlock = function (block, data, success, fail) {
        var url = route + "/intake/" + block;
        $.post(url, data).done(success).fail(fail);
    }

    var insertIntakeEntity = function(addType, orderId, success, fail) {
        var url = route + addType + "/" + orderId;
        var data = {"orderId":orderId}
        $.post(url, orderId).done(success).fail(fail);
    }

    var deleteIntakeEntity = function(delType, entityId, success, fail) {
        var url = route + delType + "/" + entityId;
        $.ajax({
            url: url,
            type: 'DELETE'
        }).done(success).fail(fail);
    }

    var insertIntakeEntityWithType = function(addType, orderId, typeId, success, fail) {
        var url = route + addType + "/" + orderId + "/" + typeId;
        $.post(url).done(success).fail(fail);
    }

    var inviteTransferee = function(orderId, success, fail) {
        var url = route + "invite";
        var data = { "OrderId": orderId };
        $.ajax({
            url: url,
            type: 'POST',
            data: data
        }).done(success).fail(fail);
    }

    return{
        updateIntakeBlock: updateIntakeBlock,
        insertIntakeEntity: insertIntakeEntity,
        deleteIntakeEntity: deleteIntakeEntity,
        insertIntakeEntityWithType: insertIntakeEntityWithType,
        inviteTransferee: inviteTransferee
    }

}();

var TransfereeIntakeController = function (transfereeIntakeService) {

    var intakeBlocks;
    var orderId;

    var childTemplate =
        '<div class="row intake-row collapse in" data-entity-collection="children" data-entity-id="#GUID#"> <div class="col-xs-6 col-sm-4 col-md-3 col-lg-3 intake-col"> <label>Name:</label> <span class="intake-span"></span> <input type="text" class="form-control intake-input" name="Name"/> <input type="hidden" name="Name" class="intake-hidden"/> </div> <div class="col-xs-6 col-sm-4 col-md-3 col-lg-3 intake-col"> <label>Age:</label> <span class="intake-span"></span> <input type="number" step="1" min="0" class="form-control intake-input" name="Age"/> <input type="hidden" name="Age" class="intake-hidden"/> </div> <div class="col-xs-6 col-sm-4 col-md-3 col-lg-3 intake-col"> <label>Grade:</label> <span class="intake-span"></span> <input type="number" step="1" min="0" class="form-control intake-input" name="Grade"/> <input type="hidden" name="Grade" class="intake-hidden"/> </div> <div class="col-xs-6 col-sm-4 col-md-3 col-lg-3 intake-col"> <span class="intake-del sectionDelete"> x Delete </span> </div> </div>';

    var petTemplate =
        '<div class="row intake-row collapse in" data-entity-id="#GUID#" data-entity-collection="pets"> <div class="col-xs-6 col-sm-4 col-md-3 col-lg-3 intake-col"> <label>Pet Type:</label> <span class="intake-span"></span> <input type="text" class="form-control intake-input" name="Type" /> <input type="hidden" class="intake-hidden" name="Type" /> </div> <div class="col-xs-6 col-sm-4 col-md-3 col-lg-3 intake-col"> <label>Breed:</label> <span class="intake-span"></span> <input type="text" class="form-control intake-input" name="Breed" /> <input type="hidden" class="intake-hidden" name="Breed" /> </div> <div class="col-xs-6 col-sm-4 col-md-3 col-lg-3 intake-col"> <label>Weight/Size:</label> <span class="intake-span"></span> <input type="text" class="form-control intake-input" name="Size" /> <input type="hidden" class="intake-hidden" name="Size" /> </div> <div class="col-xs-6 col-sm-4 col-md-3 col-lg-3 intake-col"> <span class="intake-del sectionDelete"> x Delete </span> </div></div>';

    var init = function () {

        var pnlIntake = $("div#intake");

        //Init Variables
        intakeBlocks = pnlIntake.find(".intake-block");
        orderId = pnlIntake.attr("data-order-id");

        //default to collapsed sections
        intakeBlocks.each(function () {
            var intakeBlock = $(this);
            if (intakeBlock.hasClass("expanded") == false)
                collapse(intakeBlock); 
        });

        //Init Dates
        intakeBlocks.find(".date").datetimepicker({
            format: "DD-MMM-YYYY",
            useCurrent: true,
            ignoreReadonly: true,
            allowInputToggle: true,
            showClear: true,
            keepOpen: false,
            icons: { clear: 'glyphicon glyphicon-remove' }
        }).css("display","none");

        //Bind Events
        intakeBlocks.on("click", ".intake-edit", editSaveBlock);
        intakeBlocks.on("click", ".intake-cancel", cancelEditBlock);
        intakeBlocks.on("click", ".intake-add", addRowToBlock);
        intakeBlocks.on("click", ".intake-collapse", toggleCollapse);
        intakeBlocks.on("click", ".intake-del", deleteRowFromBlock);
        intakeBlocks.on("click", ".intake-save", saveBlock);
        intakeBlocks.on("change", "[type=checkbox]", saveChecked);

        var btnInviteTransferee = $("#btnInviteTransferee");
        btnInviteTransferee.on("click", inviteTransferee);
    };

    //Event Callbacks

    var inviteTransferee = function () {
        transfereeIntakeService.inviteTransferee(orderId, inviteSuccess, inviteFailed);
    }

    var inviteSuccess = function () {
        toast("Transferee has been invited", "success");
        $('#lblInviteStatus').html("Invited");
    }

    var inviteFailed = function () {
        toast("Uh oh!  Something went wrong!", "danger");
    }

    var editSaveBlock = function (e) {

        //Local Variables
        var spnEditSave = $(e.target);
        var intakeBlock = spnEditSave.parents(".intake-block");
        var rows = intakeBlock.find(".intake-row");
        var spans = rows.find(".intake-span");
        var addSpans = rows.find(".intake-add");
        var delSpans = rows.find(".intake-del");
        var inputs = rows.find(".intake-input");
        var dates = rows.find(".date");

        //Local Functions
        var edit = function () {
            //Toggle Text
            spnEditSave.text("- Save");
            spnEditSave.next(".intake-cancel").css("display", "inline");

            //Bind hidden values to inputs
            inputs.css("display", "inline").each(function() {
                var hidden = $(this).next(".intake-hidden");
                $(this).val(hidden.val());
            });

            //Bind hidden values to inputs
            dates.css("display", "table").each(function () {
                var hidden = $(this).next(".intake-hidden");
                $(this).find("input").val(hidden.val());
            });

            spans.css("display", "none");
            addSpans.css("display", "none");
            delSpans.css("display", "none");
            
        }

        var save = function() {
            //Prepare post url and data
            var block = intakeBlock.attr("data-block");
            var data = { "Id": orderId };

            rows.each(function () {
                var row = $(this);
                var rowInputs = row.find(".intake-input, .date");

                //If a collection that can have added values
                if (hasAttr(row, 'data-entity-collection')) {
                    
                    var collectionKey = row.attr("data-entity-collection");
                    var collectionData = { "Id": row.attr("data-entity-id") };

                    fillPostData(collectionData, rowInputs);

                    if (!(collectionKey in data)) {
                        data[collectionKey] = [];
                    }
                    data[collectionKey].push(collectionData);
                    
                } else {
                    fillPostData(data, rowInputs);
                }
            });
            
            //Do save
            transfereeIntakeService.updateIntakeBlock(block, data, saveSuccess, saveFail);
        }

        var saveSuccess = function (data) {
            //Revert Text
            spnEditSave.text("+ Edit");
            spnEditSave.next(".intake-cancel").css("display", "none");

            //Update spans to new values and toggle.
            inputs.each(function () {
                var input = $(this);
                var span = input.prev("span");
                var hidden = input.next(".intake-hidden");
                if (input.is('select')) {
                    if (input.val() === null) {
                        input.find("option:selected").removeAttr("selected");
                        span.text("");
                        hidden.val("");
                    } else {
                        var selText = input.find("option:selected").text();
                        span.text(selText);
                        hidden.val(input.val());
                    }
                } else {
                    input.prev("span").text($(this).val());
                    input.next(".intake-hidden").val($(this).val());
                }
            });

            dates.each(function () {
                var innerInput = $(this).find("input");
                $(this).prev("span").text(innerInput.val());
                $(this).next(".intake-hidden").val(innerInput.val());
            });

            dates.css("display", "none");
            inputs.css("display", "none");
            spans.css("display", "inline");
            addSpans.css("display", "inline");
            delSpans.css("display", "inline");

            toast("Save Successful!","success");
        }

        var saveFail = function() {
            toast("Uh oh!  Something went wrong!", "danger");
        }
        
        //Function Execution
        expand(intakeBlock);
        if (contains(spnEditSave.text(), "Edit")) {
            edit();
        } else {           
            save();
        }
    }

    var cancelEditBlock = function (e) {
        var intakeBlock = $(e.target).parents(".intake-block");
        cancel(intakeBlock);
    }

    var addRowToBlock = function (e) {

        var spnAdd = $(e.target);
        var addType = spnAdd.attr("data-entity-add");
        var template = '';
        switch(addType) {
            case 'children':
                template = childTemplate;
                break;
            case 'pets':
                template = petTemplate;
                break;
            default:
                break;
        }

        var insertSuccess = function (guid) {
            var row = spnAdd.parents(".intake-row");
            template = template.replace("#GUID#", guid);
            row.after(template);
            row.parents(".intake-block").find(".intake-edit").click();
            toast("Added!", "success");
        }

        var insertFail = function() {
            toast("Uh oh!  Something went wrong!", "danger");
        }

        //Function Execution
        TransfereeIntakeService.insertIntakeEntity(addType, orderId, insertSuccess, insertFail);
    }

    var deleteRowFromBlock = function (e) {
        var spnDel = $(e.target);
        var row = spnDel.parents(".intake-row");
        var delType = row.attr("data-entity-collection");
        var entityId = row.attr("data-entity-id");

        var deleteSuccess = function () {
            row.remove();
            toast("Deleted!", "success");
        }

        var deleteFail = function() {
            toast("Uh oh!  Something went wrong!", "danger");
        }

        transfereeIntakeService.deleteIntakeEntity(delType, entityId, deleteSuccess, deleteFail);
    }

    var toggleCollapse = function (e) {
        var intakeBlock = $(e.target).parents(".intake-block");
        if (intakeBlock.find(".intake-collapse-img").is(":visible")) {
            collapse(intakeBlock);
            if (intakeBlock.find(".intake-cancel").length > 0) {
                cancel(intakeBlock);
            }
        } else {
            expand(intakeBlock);
        }
        
    }

    var saveBlock = function(e) {
        var intakeBlock = $(e.target).parents(".intake-block");
        var block = intakeBlock.attr("data-block");
        var rows = intakeBlock.find(".intake-row[data-entity-id]");

        var saveSuccess = function() {
            toast("Save Successful!","success");
        }
        var saveFail = function () {
            toast("Uh oh!  Something went wrong!", "danger");
        }

        var data = { "Id": orderId };

        rows.each(function () {
            var row = $(this);
            var rowInputs = row.find(".intake-input, .date");

            //If a collection that can have added values
            if (hasAttr(row, 'data-entity-collection')) {

                var collectionKey = row.attr("data-entity-collection");
                var collectionData = { "Id": row.attr("data-entity-id") };

                fillPostData(collectionData, rowInputs);

                if (!(collectionKey in data)) {
                    data[collectionKey] = [];
                }
                data[collectionKey].push(collectionData);
                
            } else {
                fillPostData(data, rowInputs);
            }
        });

        transfereeIntakeService.updateIntakeBlock(block, data, saveSuccess, saveFail);
    }

    var saveChecked = function (e) {

        var checkbox = $(e.target);
        var row = checkbox.parents(".intake-row");
        var rowInputs = row.find(".intake-input");

        var saveSuccess = function(data) {
            row.attr("data-entity-id", data);
            toast("Added!", "success");
        }

        var saveFail = function () {
            rowInputs.not("[type=checkbox]").prop("disabled", true);
            toast("Uh oh!  Something went wrong!", "danger");
        }

        if (checkbox.prop('checked')) {
            rowInputs.prop("disabled", false);
            if (!hasAttr(row, "data-entity-id")) {

                var addType = row.attr("data-entity-collection");
                var typeId = row.attr("data-entity-type-id");
                
                //create new service
                transfereeIntakeService.insertIntakeEntityWithType(addType, orderId, typeId, saveSuccess, saveFail);
                
            }
        } else {
            rowInputs.not("[type=checkbox]").prop("disabled", true);
        }
    }

    //Utilities

    var fillPostData = function(data, inputs){
        inputs.each(function () {
            var elt = $(this);
            if (elt.hasClass("date")) {
                var innerInput = elt.find("input");
                data[innerInput.attr("name")] = innerInput.val();
            } else if (elt.is(":checkbox")) {
                data[elt.attr("name")] = elt.prop('checked');
            } else {
                data[elt.attr("name")] = elt.val();
            }
        });
    }

    var hasAttr = function (obj, attrName)
    {
        var attr = obj.attr(attrName);
        return typeof attr !== typeof undefined && attr !== false && attr !== "" && attr !== null;
    }

    var contains = function(value, searchFor)
    {
        return (value || '').indexOf(searchFor) > -1;
    };

    var expand = function (intakeBlock) {
        var colImg = intakeBlock.find(".intake-collapse-img");
        var expImg = intakeBlock.find(".intake-expand-img");

        var intakeRows = intakeBlock.find(".intake-row");
        intakeBlock.find(".intake-row").collapse("show");
        expImg.css("display", "none");
        colImg.css("display", "inline");
        
    }

    var collapse = function(intakeBlock) {
        var colImg = intakeBlock.find(".intake-collapse-img");
        var expImg = intakeBlock.find(".intake-expand-img");
      
        var intakeRows = intakeBlock.find(".intake-row");
        intakeBlock.find(".intake-row").collapse("hide");
        colImg.css("display", "none");
        expImg.css("display", "inline");
    }

    var cancel = function (intakeBlock) {
        spnCancel = intakeBlock.find(".intake-cancel");
        var cols = intakeBlock.find(".intake-row > .intake-col");
        spnCancel.css("display", "none");
        spnCancel.prev(".intake-edit").text("+ Edit");
        cols.find(".intake-input").css("display", "none").val("");
        cols.find(".date").css("display", "none").find("input").val("");
        cols.find(".intake-span").css("display", "inline");
        cols.find(".intake-add").css("display", "inline");
        cols.find(".intake-del").css("display", "inline");
    }

    var toast = function (message, type) {
        $.notify({
            message: message
        }, {
            delay: 2000,
            type: type,
            placement: {
                from: "bottom",
                align: "center"
            },
            animate: {
                enter: 'animated fadeInUp',
                exit: 'animated fadeOutDown'
            }
        });
    }

    return {
        init: init
    };
}(TransfereeIntakeService);

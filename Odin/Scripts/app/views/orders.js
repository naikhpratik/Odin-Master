var OrdersPageController = function () {

    var _data;
    var _sortAsc = true;
    var _sortCurr = "";

    var init = function () {
        sizePage();
        _data = getData();
        initSearch();
        initSort();
        initPmDropDown();

        $(window).resize(function () {
            sizePage();
        });
    };

    var sizePage = function () {

        var marginalWidth = 0;
        marginalWidth = ($(window).innerWidth() - 1440) / 2;

        if (window.innerWidth > 1440) {
            $('#primaryNav').css('left', marginalWidth);

        } else if (window.innerWidth >= 768) {
            $('#primaryNav').css('left', 0);

        }
    };

    var initSearch = function () {
        // constructs the suggestion engine

        var engine = new Bloodhound({
            datumTokenizer: function (datum) {
                return Bloodhound.tokenizers.whitespace(datum.eeName);
            },
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            local: _data,
            identify: function (obj) { return obj.id }
        });

        $('#searchbox').typeahead({
            hint: true,
            highlight: true,
            minLength: 1
        },
            {
                name: 'data',
                source: engine,
                displayKey: 'eeName'
            }
        ).on('typeahead:selected',
            function (e, suggestion, name) {

                goToOrder(suggestion.id);
            }
            );

        // row selection handler
        $('.clickable').click(function () {
            goToOrder($(this).attr("data-order-id"));
        });
    }

    var initPmDropDown = function () {

        $('.clickablepm').click(function () {
            goToOrderofManagers($(this).attr("data-order-id"));
        });

    }

    var initSort = function () {
        $("#eeNameSort").click(function () {
            sortCol($(this), "eeLastName", function (a) { return a.toUpperCase() });
        });

        $("#pmNameSort").click(function () {
            sortCol($(this), "pmLastName", function (a) { return a.toUpperCase() });
        });

        $("#preTripDateSort").click(function () {
            sortCol($(this), "preTripDate", function (a) { return getDate(a) });
        });

        $("#estimatedArrivalDateSort").click(function () {
            sortCol($(this), "estimatedArrivalDate", function (a) { return getDate(a) });
        });

        $("#notificationsSort").click(function () {
            sortCol($(this), "notifications", function (a) { return parseInt(a.replace("*", "")) });
        });


        //Default sort. Move to server?
        sortCol($("#estimatedArrivalDateSort"), "estimatedArrivalDate", function (a) { return getDate(a) });
    }

    var sortCol = function (headerElt, dataName, sortFunc) {
        resetSortHeaders();
        headerElt.css("font-weight", "Bold").css("color", "black");

        var ascElt = headerElt.find(".sortPlus");
        var descElt = headerElt.find(".sortMinus");

        var sortId = headerElt.attr('id');
        if (_sortCurr === sortId) {
            _sortAsc = !_sortAsc;
        } else {
            _sortCurr = sortId;
            _sortAsc = true;
        }

        if (_sortAsc) {
            ascElt.css("display", "inline-block");
            descElt.css("display", "none");

        } else {
            descElt.css("display", "inline-block");
            ascElt.css("display", "none");
        }

        _data.sort(sortBy(dataName, _sortAsc, sortFunc));
        bindData();
    }

    var resetSortHeaders = function () {
        $(".sortLabel, .sortLabel").css("font-weight", "normal").css("color", "#858585");
        $(".sortPlus,.sortMinus").css("display", "none");

    }

    var bindData = function () {
        var rows = $(".orderRow");
        rows.each(function (i, element) {

            var elt = $(element);

            elt.attr("data-order-id", _data[i]["id"]);
            elt.find(".eeName").text(_data[i]["eeName"]);
            elt.find(".eeName").data("last-name", _data[i]["eeLastName"]);
            elt.find(".rmcName").text(_data[i]["rmcName"]);
            elt.find(".clientName").text(_data[i]["clientName"]);
            elt.find(".pmName").text(_data[i]["pmName"]);
            elt.find(".pmName").data("last-name", _data[i]["pmLastName"]);
            elt.find(".pmPhone").text(_data[i]["pmPhone"]);
            elt.find(".preTripDate").text(_data[i]["preTripDate"]);
            elt.find(".estimatedArrivalDate").text(_data[i]["estimatedArrivalDate"]);
            elt.find(".notifications").text(_data[i]["notifications"]);

            elt.find(".scheduledCaption").html(_data[i]["scheduledCaption"]);
            elt.find(".completedCaption").html(_data[i]["completedCaption"]);

            var pb = elt.find(".progress");
            pb.attr("data-comp-percent", _data[i]["compPercent"]);
            pb.attr("data-sched-percent", _data[i]["schedPercent"]);
            loadProgressBar(pb);
        });
    }

    var getData = function () {

        var rows = $(".orderRow");
        var data = new Array();

        rows.each(function (i, element) {

            var elt = $(element);
            var id = elt.attr("data-order-id");
            var eeName = elt.find(".eeName").text();
            var eeLastName = elt.find(".eeName").attr("data-last-name");
            var rmcName = elt.find(".rmcName").text();
            var clientName = elt.find(".clientName").text();
            var pmName = elt.find(".pmName").text();
            var pmLastName = elt.find(".pmName").attr("data-last-name");
            var pmPhone = elt.find(".pmPhone").text();
            var preTripDate = elt.find(".preTripDate").text();
            var estimatedArrivalDate = elt.find(".estimatedArrivalDate").text();
            var notifications = elt.find(".notifications").text();

            var pb = elt.find(".progress");
            var schedPercent = pb.attr("data-sched-percent");
            var compPercent = pb.attr("data-comp-percent");

            var scheduledCaption = elt.find(".scheduledCaption").html();
            var completedCaption = elt.find(".completedCaption").html();

            data.push({
                id: id,
                eeName: eeName,
                eeLastName: eeLastName,
                rmcName: rmcName,
                clientName: clientName,
                pmName: pmName,
                pmLastName: pmLastName,
                pmPhone: pmPhone,
                preTripDate: preTripDate,
                estimatedArrivalDate: estimatedArrivalDate,
                notifications: notifications,
                schedPercent: schedPercent,
                compPercent: compPercent,
                scheduledCaption: scheduledCaption,
                completedCaption: completedCaption
            });
        });

        return data;
    }

    //https://stackoverflow.com/questions/979256/sorting-an-array-of-javascript-objects
    var sortBy = function (field, asc, primer) {
        var key = primer ?
            function (x) { return primer(x[field]) } :
            function (x) { return x[field] };

        var reverse = asc ? 1 : -1;

        return function (a, b) {
            return a = key(a), b = key(b), reverse * ((a > b) - (b > a));
        }
    }

    var goToOrder = function (id) {
        //alert("goToOrder");
        window.location.href = "/Orders/Transferee/" + id;
    }

    var goToOrderofManagers = function (id) {
        //alert("goToOrderofManagers");
        window.location.href = "/Orders/Index/" + id;
    }

    var loadProgressBar = function (pbElt) {
        pbElt.find(".progressBar__sched").width(pbElt.attr("data-sched-percent"));
        pbElt.find(".progressBar__comp").width(pbElt.attr("data-comp-percent"));
    }

    //https://stackoverflow.com/questions/23532270/date-format-conversion-from-dd-mmm-yyyy-to-dd-mm-yyyy-in-javascript?noredirect=1&lq=1
    var getDate = function (str) {
        if (str) {
            var arr = str.split("-");
            var months = ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"];

            var month = months.indexOf(arr[1].toLowerCase());

            return new Date(parseInt(arr[2]), month, parseInt(arr[0]));
        } else {
            return new Date();
        }
    }

    return {
        init: init
    };
}();
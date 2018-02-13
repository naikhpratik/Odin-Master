javascript: (function () {
    
    //Super Hacky
    //If string not replaced by CI use localhost.
    var bmDomain = "#{BOOKMARKLET_DOMAIN}#";
    var bmUrl = bmDomain.toLowerCase().indexOf("bookmarklet_domain") >= 0
        ? "https://localhost:44312/BookMarklet?url="
        : bmDomain + "/BookMarklet?url=";
    var bmGifUrl = bmDomain.toLowerCase().indexOf("bookmarklet_domain") >= 0
        ? "https://localhost:44312/Content/Images/bm-loader.gif"
        : bmDomain + "/Images/bm-loader.gif";
    
    var getComputedStyle = function (elt, style) {
        var result = null;
        if (window.getComputedStyle) {
            result = document.defaultView.getComputedStyle(elt, null).getPropertyValue(style);
        }
        else if (x.currentStyle) {
            result = elt.currentStyle[style];
        }
        return result;
    }

    var getMaxZIndex = function () {
        var index_highest = 0;
        Array.prototype.forEach.call(document.querySelectorAll('*'),
            function (item) {
                var index_current = parseInt(getComputedStyle(item, "z-index"), 10);
                if (index_current > index_highest)
                    index_highest = index_current;
            });
        return index_highest;
    }

    var initBookMarkletSize = function () {
        var bm = document.getElementById("divOdinBookMarklet");
        if (window.innerWidth < 500) {
            bm.style.width = '100%';
            bm.style.right = 0;
        } else {
            bm.style.width = '450px';
            bm.style.right = "50px";
        }
    }

    var removeBookMarklet = function () {
        var elt = document.getElementById("divOdinBookMarklet");
        if (elt !== null) {
            document.body.removeChild(elt);
            elt.removeEventListener("resize", initBookMarkletSize);
        }
    }

    var init = function () {

        //Clean remove existing book marklet if already on page.
        removeBookMarklet();

        var maxZ = getMaxZIndex() + 1;
        var encodedUrl = encodeURIComponent(document.location.href);

        var bmTag = document.createElement("div");
        bmTag.setAttribute("id", "divOdinBookMarklet");

        var bmCss = 'display: block; position: fixed; top: 0; right: 50px; width: 450px; height: 420px; z-index:' + maxZ + ';';
        bmTag.setAttribute('style', bmCss);

        var contentCss = 'box-shadow: 0 4px 8px 0 rgba(0, 0, 0, 0.2), 0 6px 20px 0 rgba(0, 0, 0, 0.19); height: inherit; font-family: sans-serif; background-image:url(\'' + bmGifUrl +'\'); background-repeat: no-repeat; background-position: center center; background-color:#fff;';
        var closeCss = 'float: right; font-weight: bold; margin: 10px 20px 0px 0px; cursor: pointer';
        var headerCss = 'color: #fff; background-color:#a7ce39; font-size: 20px; margin-bottom:20px;';
        var titleCss = 'padding: 20px 40px 20px 40px;';

        var contentHtml =
            '<div id="divOdinBookMarkletContent" style="' + contentCss + '">' +
            '<div style="' + headerCss + '">' +
            '<div id="divOdinBookMarkletClose" style="' + closeCss + '">&times;</div>' +
            '<div style="' + titleCss + '">I Like This Property</div>' +
            '</div>' +
            '<iframe height="320px" width="100%" id="odinIFrame" frameBorder="0" src="'+bmUrl+encodedUrl + '"></iframe>' +
            '</div>';

        bmTag.innerHTML = contentHtml;
        document.body.appendChild(bmTag);

        var btnClose = document.getElementById('divOdinBookMarkletClose');
        btnClose.addEventListener("click", removeBookMarklet);

        initBookMarkletSize();
        window.addEventListener("resize", initBookMarkletSize);
    }

    //Execution
    init();

})();
var map, iw;
var results = [];
var autocomplete;
var unseenCount = 0;

function initialize() {
    // For statistics page only:
    $('.ddl').change(function () {
        $('#heatMap').attr('src', '/Statistics/HeatMap/' + $(this).val());
    });
    
    // For the google maps stuff
    var hamilton = new google.maps.LatLng(43.261837, -79.93721);
    var options = {
        zoom: 12,
        center: hamilton,
        mapTypeId: google.maps.MapTypeId.ROADMAP
    };

    map = new google.maps.Map(document.getElementById('map_canvas'), options);
    google.maps.event.addListenerOnce(map, 'idle', loadData);

}

function loadData() {
    $.getJSON('/Home/Fetch', function (data) {
        results = data;
        search();
        updateDeets();
    });
}

function search() {
    for (var i = 0; i < results.length; i++) {
        createMarker(i);
    }
}

function updateDeets() {
    var units = 0;
    for (var i = 0; i < results.length; i++) {
        units += results[i].Units;
    }
    var deets = 'Currently ' + results.length + ' events using ' + units + ' units.';
    if (unseenCount > 0)
        deets += ' <a href="#" onclick="$(\'#hiddenResults\').toggle()">' + unseenCount + ' not displayed on the map</a>.';
    $('#deets').html(deets);
}

function createMarker(i) {
    if (results[i].Lat == 0 || results[i].Long == 0) {
        unseenCount++;
        $("#hiddenResults").append("<li>" + results[i].TweetText + "</li>");
    } else {
        //var pinColor = "FE7569";
        // var dot = "%E2%80%A2"
        // Place pin
        var pinImage = new google.maps.MarkerImage("http://chart.apis.google.com/chart?chst=d_map_pin_letter&chld=" + results[i].Units + "|" + results[i].Color,
            null, null, null,
            new google.maps.Size(21 * results[i].Scale, 34 * results[i].Scale)
        );
        // Place the pin's shadow
        var pinShadow = new google.maps.MarkerImage("http://chart.apis.google.com/chart?chst=d_map_pin_shadow",
            null, null,
            new google.maps.Point(12, 35));

        results[i].marker = new google.maps.Marker({
            position: new google.maps.LatLng(results[i].Lat, results[i].Long),
            animation: google.maps.Animation.DROP,
            icon: pinImage,
            shadow: pinShadow
        });

        google.maps.event.addListener(results[i].marker, 'click', showInfoWindow(i));
        setTimeout(dropMarker(i), i * 100);
    }
}

function dropMarker(i) {
    return function () {
        results[i].marker.setMap(map);
    };
}

function showInfoWindow(i) {
    return function() {
        if (iw) {
            iw.close();
            iw = null;
        }

        iw = new google.maps.InfoWindow({
            content: results[i].TweetText
        });
        iw.open(map, results[i].marker);
    };
}

$(document).ready(initialize);

// SignalR.EventManager methods
$(function () {
    // Start the connection
    $.connection.hub.start().pipe(init);

    var manager = $.connection.eventManager; // the generated client-side hub proxy

    function init() {
        return manager.server.start();
    }

    // Add client-side hub methods that the server will call
    $.extend(manager.client, {
        updateEvents: function (event) {
            // This method is called when an event is pushed from the server
            var resultId = -1;
            for (var i = 0; i < results.length; i++) {
                if (results[i].Id == event.Id) {
                    // Marker alread found, remove it!
                    results[i].marker.setMap(null);
                    results[i].marker = null;
                    if (results[i].Lat == 0 || results[i].Long == 0)
                        unseenCount--;
                    resultId = i;
                }
            }

            if (resultId == -1) {
                // New marker!
                resultId = results.length;
            }
            
            results[resultId] = event;

            createMarker(resultId);
            updateDeets();
        }
    });
});
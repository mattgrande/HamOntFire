using System;
using System.Net;
using System.Text;
using HamOntFire.Core.Domain;
using Newtonsoft.Json.Linq;
using log4net;

namespace HamOntFire.Core
{
    public class GeoCoder
    {
        // Set the geographical bounds of Hamilton
        internal const Decimal WesternEdge = -80.25444m;
        internal const Decimal EasternEdge = -79.61998m;
        internal const Decimal NorthernEdge = 43.473851m;
        internal const Decimal SouthernEdge = 43.049823m;
        private readonly ILog _logger;

        public GeoCoder()
        {
            _logger = LogManager.GetLogger(typeof(GeoCoder));
        }

        public void GeoCode(Event @event)
        {
            // Get the list of searchable addresses
            var addresses = @event.GetAddressList();

            // Iterate the list until an acceptable match is found
            foreach (string address in addresses)
            {
                var url = BuildUrl(address);
                
                _logger.DebugFormat("URL: {0}", url);

                string json;
                using (var c = new WebClient())
                {
                    json = c.DownloadString(url);
                }
                _logger.DebugFormat("Response: {0}", json);
                JObject jObject = JObject.Parse(json);

                var status = jObject["status"].ToString();
                if (status == "ZERO_RESULTS" || status == "OVER_QUERY_LIMIT")
                    continue;

                var results = (JArray) jObject["results"];
                if (results.Count == 0)
                    continue;

                var geometry = (JObject) results[0]["geometry"];
                var location = (JObject) geometry["location"];

                var lat = (Decimal)location["lat"];
                var lng = (Decimal)location["lng"];

                @event.Lat = lat;
                @event.Long = lng;
                break;
            }
        }

        private static string BuildUrl(string address)
        {
            address = address.Replace(' ', '+');
            address = address.Replace("&", "and");

            var sb = new StringBuilder();
            sb.Append("http://maps.googleapis.com/maps/api/geocode/json");
            sb.AppendFormat("?address={0}", address);
            sb.AppendFormat("&bounds={0},{1}|{2},{3}", SouthernEdge, WesternEdge, NorthernEdge, EasternEdge);
            sb.Append("&sensor=false");

            return sb.ToString();
        }

        // http://en.wikipedia.org/wiki/Jordan_curve_theorem
        // If the number of points that cross the polygon is even, the point provided is outside of the polygon.
        // Otherwise, it is inside the polygon
        // Thanks to http://singelabs.blogspot.ca/2011/11/fast-concave-polygon-based-collision.html
        public bool EventIsInWard(Location[] verts, Event test)
        {
            int npoints = verts.Length;
            int i, j=npoints-1, c = 0;
            for (i = 0; i < npoints; j=i++)
            {
                if (((verts[i].Lat > test.Lat) != (verts[j].Lat > test.Lat)) &&
                 (test.Long < (verts[j].Lng - verts[i].Lng) * (test.Lat - verts[i].Lat) / (verts[j].Lat - verts[i].Lat) + verts[i].Lng))
                    c++;
            }
            return c%2!=0;
        }
    }

    public class Location
    {
        public Location(decimal lat, decimal lng)
        {
            Lat = lat;
            Lng = lng;
        }
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
    }
}

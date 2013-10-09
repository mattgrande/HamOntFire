using System;
using System.Collections.Generic;

namespace HamOntFire.Core.Domain
{
    public class Event
    {
        public string Id { get; set; }
        public long TweetId { get; set; }

        public string TweetText { get; set; }
        public string AdditionalInfo { get; set; }
        public string City { get; set; }
        public string CommonName { get; set; }
        public string Intersection { get; set; }
        public string Location { get; set; }
        public string StreetAddress { get; set; }
        public string Type { get; set; }
        public short Units { get; set; }

        public decimal Lat { get; set; }
        public decimal Long { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private List<String> _addresses;

        internal void ClearLocationInfo()
        {
            _addresses = null;
            City = null;
            CommonName = null;
            Intersection = null;
            Location = null;
            StreetAddress = null;
        }

        public List<String> GetAddressList()
        {
            if (_addresses == null)
                _addresses = new List<string>();
            if (_addresses.Count > 0)
                return _addresses;

            GetAddressList( City );
            if (City != "Hamilton")
                GetAddressList( "Hamilton" );

            return _addresses;
        }

        private void GetAddressList(string city)
        {
            /* Priority:
             * 1. Common Name near Intersection, City
             * 2. Common Name near Address, City
             * 3. Intersection, City
             * 4. Address, City
             * (If City != 'Hamilton,' repeat the above with 'Hamilton' in place of the former municipality's name)
             * 
             * Note that Intersection is preferable to address because it's not the "true" street address.
             * Rather, it's just "900 Block Ferguson Ave" or something similar.
            */

            //if (!string.IsNullOrEmpty(CommonName))
            //{
            //    // 1. Common Name near Intersection, City
            //    if (!string.IsNullOrEmpty(Intersection))
            //        _addresses.Add(string.Format("{0} near {1}, {2}, ON", CommonName, Intersection, city));

            //    // 2. Common Name near Address, City
            //    if (!string.IsNullOrEmpty(StreetAddress))
            //        _addresses.Add(string.Format("{0} near {1}, {2}, ON", CommonName, StreetAddress, city));
            //}

            // 3. Intersection, City
            if (!string.IsNullOrEmpty(Intersection) && !Intersection.Contains("PRIVATE RD"))
                _addresses.Add(string.Format("{0}, {1}, ON", Intersection, city));

            // 4. Address, City
            if (!string.IsNullOrEmpty(StreetAddress))
                _addresses.Add(string.Format("{0}, {1}, ON", StreetAddress, city));
        }

        //public string ToJson()
        //{
        //    string tweetText = TweetText.Replace(Environment.NewLine, "<br/>");
        //    decimal scale = Units + ((Units - 1)*0.2m);
        //    return string.Format("{{\"TweetText\": \"{0}\", \"location\": {{\"lat\": {1},\"lng\": {2}}}, \"Scale\": {3}}}",
        //                      tweetText, Lat, Long, scale);
        //}

        public dynamic ToViewModel()
        {
            return new {
                    TweetText = TweetText.Replace(Environment.NewLine, "<br/>"),
                    Lat,
                    Long,
                    Id,
                    Units,
                    Scale = 1 + ((Units - 1)*0.1),
                    Color = GetColor()
                };
        }

        private string GetColor()
        {
            string color = "FE7569";    // Default google maps pin colour.
            switch (Type)
            {
                case "MEDICAL":
                    color = "03F";
                    break;
                case "VEHICLE ACC":
                    color = "CF0";
                    break;
                case "ALARM CONDITIONS":
                    color = "FFF";
                    break;
                case "CO DETECTOR":
                    color = "AAA";
                    break;
                case "RUBBISH FIRE":
                    color = "C63";
                    break;
                case "FALSE ALARM":
                    color = "0F0";
                    break;
                case "SMOKE DETECTOR":
                    color = "333";
                    break;
                case "STRUCTURE FIRE":
                    color = "F00";
                    break;
            }

            return color;
        }
    }
}

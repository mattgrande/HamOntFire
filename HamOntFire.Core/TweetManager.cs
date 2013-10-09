using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using HamOntFire.Core.Domain;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Raven.Client;
using log4net;

namespace HamOntFire.Core
{
    public class TweetManager : IDisposable
    {
        private static readonly Regex LocationRegex;
        private static readonly Regex HighwayExitRegex;
        private static readonly char[] SingleSpace;
        private static readonly ILog Logger;
        private readonly IDocumentSession _session;

        private Dictionary<string, Event> _loadedEvents; 

        #region ctor/dtor/Dispose

        static TweetManager()
        {
            LocationRegex = new Regex(@"(?:Loc: )?(\d+) Block");
            HighwayExitRegex = new Regex(@"EXIT \d*");
            SingleSpace = new[] {' '};
            Logger = LogManager.GetLogger(typeof (TweetManager));
            Logger.Debug("TweetManager Static Init");
        }

        public TweetManager(IDocumentSession session)
        {
            _session = session;
            Logger.Debug("TweetManager Init");
        }

        ~TweetManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_session != null)
                    _session.Dispose();
            }
        }

        #endregion

        #region ORM

        public void Save(Event e)
        {
            bool isNew = false;
            if (e.CreatedAt == DateTime.MinValue)
            {
                e.CreatedAt = DateTime.UtcNow;
                isNew = true;
            }
            e.UpdatedAt = DateTime.UtcNow;

            if (isNew)
                _session.Store(e);
        }

        public Event Load(String id)
        {
            if (_loadedEvents != null && _loadedEvents.ContainsKey(id))
            {
                return _loadedEvents[id];
            }

            // Not in the dict, load it from the DB.
            try
            {
                //var @event = _session.Query<Event>().First(e => e.Id == id);
                var @event = _session.Load<Event>(id);
                return @event;
            }
            catch (InvalidOperationException ioex)
            {
                Logger.ErrorFormat("Invalid Operation Exception when loading Tweet {0}. Msg: {1}, Stack: {2}", id,
                                   ioex.Message, ioex.StackTrace);
                return null;
            }
        }

        public List<Event> GetLatest()
        {
            const Int32 hours = 3;
            var @events = _session.Query<Event>().Where(e => e.UpdatedAt >= DateTime.UtcNow.AddHours(hours * -1)).ToList();
            return @events;
        }

        public List<Event> GetEvents(string type, int ward)
        {
            // Build WHERE
            var where = BuildWhere(type);

            IEnumerable<Event> @events = _session.Query<Event>().Take(1000);
            if (where != null)
                @events = @events.Where( where );
            return @events.ToList();
        }

        private static Func<Event, bool> BuildWhere(string type)
        {
            if (type == null)
                return null;
            return e => e.Type == type;
        }

        public void SaveChanges()
        {
            _session.SaveChanges();
        }

        public long GetGreatestTweetId()
        {
            var @event = _session.Query<Event>("Events/ByTweetIdSortByTweetId").OrderByDescending(e => e.TweetId).FirstOrDefault();
            long tweetsSince = 0;
            if (@event != null)
                tweetsSince = @event.TweetId;
            return tweetsSince;
        }

        #endregion

        /// <summary>
        /// Downloads the most recent tweets from HFS_Incidents
        /// </summary>
        /// <param name="since">The current, most recent tweet that we have in the system</param>
        /// <returns>A dictionary. Key=Tweet ID, Value=Tweet Text</returns>
        public virtual IList<Event> DownloadTweets(Int64 since=0)
        {
            _loadedEvents = new Dictionary<string, Event>();

            Logger.Info("Downloading Tweets...");

            var url = GetUrl(since);
            Logger.DebugFormat("URL: {0}", url);

            var geocoder = new GeoCoder();

            string json;
            using (var c = new WebClient())
            {
                json = c.DownloadString(url);
            }
            Logger.DebugFormat("Response: {0}", json);
            JArray jArray = JArray.Parse(json);
            var events = new List<Event>(jArray.Count);

            // Run through the array in reverse order so NEW events are added before their UPDATEs.
            for (int index = jArray.Count-1; index >= 0; index--)
            {
                JToken jToken = jArray[index];
                var id = (Int64) jToken["id"];
                var text = (String) jToken["text"];

                var @event = Parse(text, id);
                if (@event == null)
                    continue;
                @event.TweetId = id;

                geocoder.GeoCode(@event);

                events.Add(@event);
            }

            SaveChanges();
            return events;
        }

        public virtual Event Parse(String value, Int64 tweetId=0)
        {
            if (_loadedEvents == null)
                _loadedEvents = new Dictionary<string, Event>();

            using (ThreadContext.Stacks["NDC"].Push(value))
            {
                if (value.Contains("Read more at"))
                {
                    // There's more to this tweet on twittercounter.com! Let's get it!
                    value = GetFullTweet(value);
                }

                Event e = null;
                var parts = value.Split('|');
                if (parts[0].Trim() == "NEW")
                {
                    e = ParseNewEvent(parts);
                    e.TweetText = value;
                    _loadedEvents.Add(e.Id, e);
                    Save(e);
                }
                else if (parts[0].Trim() == "UPDATE")
                {
                    e = ParseUpdatedEvent(parts);
                    if (e == null)
                        return null;
                    e.TweetText = String.Concat(e.TweetText, Environment.NewLine, value);
                }

                return e;
            }
        }

        private static string GetFullTweet(string value)
        {
            Logger.Info("Getting the full tweet...");
            const string readMoreAt = "Read more at ";
            int urlIndex = value.IndexOf(readMoreAt, StringComparison.InvariantCulture);
            urlIndex += readMoreAt.Length;
            var url = value.Substring(urlIndex);

            if (!url.StartsWith("http"))
                url = string.Format("http://{0}", url);
            Logger.DebugFormat("Tweet URL: {0}", url);


            var c = new WebClient();
            var fullTweetHtml = c.DownloadString( url );
            const string tweetStart = "<span style=\"font-size:1.9em;\">";
            const string tweetEnd = "</span>";

            int start = fullTweetHtml.IndexOf(tweetStart, StringComparison.InvariantCulture);
            start += tweetStart.Length;
            int end = fullTweetHtml.IndexOf(tweetEnd, start);

            string fullTweet = fullTweetHtml.Substring(start, end - start);

            // Downloaded tweets come with extra \r's, \n's, and double spaces.
            // Clear 'em out!
            fullTweet = fullTweet.Trim();
            fullTweet = fullTweet.Replace("  ", " ");

            Logger.InfoFormat("Full tweet: {0}", fullTweet);
            return fullTweet;
        }

        private static Event ParseNewEvent(IList<string> parts)
        {
            var e = new Event {Id = parts[1].Trim(), Type = parts[2].Trim()};

            String location = parts[3].Trim();
            ParseLocation(e, location);

            e.Units = GetUnits(parts[4]);
            return e;
        }

        private Event ParseUpdatedEvent(IList<string> parts)
        {
            String id = parts[1].Trim();
            Event @event = Load(id);
            if (@event == null)
            {
                Logger.ErrorFormat("No previous Tweet could be found for Event {0}.", id);
                return null;
            }

            String update = parts[2];

            // Split the update data on a colon.
            String[] updateParts = update.Split(new[] {':'}, 2);
            var updateType = updateParts[0].Trim();
            var data = updateParts[1].Trim();

            Logger.DebugFormat("Updating {0} with {1}: {2}", id, updateType, data);

            switch (updateType)
            {
                case "Add Info":
                    // eg. VSA, Police Requested
                    @event.AdditionalInfo = data;
                    break;
                case "Incident Type":
                    @event.Type = data;
                    break;
                case "Loc":
                    @event.ClearLocationInfo();
                    ParseLocation(@event, data);
                    break;
                case "Units":
                    @event.Units = GetUnits(data);
                    break;
                default:
                    Logger.ErrorFormat("Unknown update type {0} with data {1}.", updateType, data);
                    break;
            }

            return @event;
        }

        private static short GetUnits(string strUnits)
        {
            Logger.DebugFormat("Parsing Units: {0}", strUnits);
            strUnits = strUnits.Replace("Units:", String.Empty);
            try
            {
                Int16 units = Convert.ToInt16(strUnits.Trim());
                return units;
            }
            catch (FormatException fex)
            {
                Logger.ErrorFormat("Could not parse a value for units: '{0}.' Exception: {1}. Returning 0.", strUnits, fex.Message);
                return 0;
            }
        }

        public static void ParseLocation(Event e, string location)
        {
            Logger.DebugFormat("Parsing location {0}.", location);
            e.Location = location;

            // Google Maps does not recognize 50 RD for Fifty Road.
            if (location.Contains("50 RD"))
                location = location.Replace("50 RD", "Fifty Road");

            // Parse Common Name from location
            Int32 indexOfCommonName = location.IndexOf("/CN:", StringComparison.Ordinal);
            if (indexOfCommonName > -1)
            {
                var cn = location.Substring(indexOfCommonName);
                location = location.Remove(indexOfCommonName).Trim();

                cn = cn.Replace("/CN:", String.Empty).Trim();
                Logger.DebugFormat("Common Name: {0}", cn);
                e.CommonName = cn;

                if (HighwayExitRegex.IsMatch(cn))
                {
                    // This is a highway exit; let's treat it like an intersection.
                    // eg, EXIT 78 QEW E/B TO Fifty Road -> QEW & Fifty Road
                    //
                    // 1. Remove Exit #
                    var intersection = HighwayExitRegex.Replace(cn, string.Empty);
                    // 2. Remove E/B or W/B
                    intersection = intersection.Replace("E/B", string.Empty);
                    intersection = intersection.Replace("W/B", string.Empty);
                    intersection = intersection.Replace("EASTBOUND", string.Empty);
                    intersection = intersection.Replace("WESTBOUND", string.Empty);
                    // 3. Change TO to &
                    intersection = intersection.Replace(" TO ", " & ");
                    intersection = intersection.Replace(" @ ", " & ");
                    e.Intersection = intersection.Trim();
                }
                else if (cn.StartsWith("RHVP"))
                {
                    // EG: RHVP @ KING ST. TO GREENHILL
                    var intersection = cn.Replace("RHVP", "Red Hill Valley Parkway");
                    if (intersection.Contains("@") && intersection.Contains(" TO ")) ;
                    {
                        // Remove the " TO GREENHILL" part
                        int i = intersection.IndexOf(" TO ", StringComparison.Ordinal);
                        if (i >= 0)
                            intersection = intersection.Remove(i);
                    }

                    intersection = intersection.Replace(" TO ", " & ");
                    intersection = intersection.Replace(" @ ", " & ");
                    e.Intersection = intersection.Trim();
                }
            }

            if (location.Contains(" /PRIVATE RD"))
                location = location.Replace(" /PRIVATE RD", string.Empty);

            if (location.Contains(" @ PRIVATE RD"))
                location = location.Replace(" @ PRIVATE RD", string.Empty);

            // Parse the City from the location
            String city;
            if (location.Contains("@"))
            {
                var firstPart = location.Split('@')[0];
                city = GetCity(firstPart);
                Logger.DebugFormat("City: {0}", city);

                // Parse the Intersection from the location
                e.Intersection = GetIntersection(location);
                Logger.DebugFormat("Intersection: {0}", e.Intersection);

                // Parse Street Address from location
                e.StreetAddress = GetStreetAddress(firstPart);
                Logger.DebugFormat("StreetAddress: {0}", e.StreetAddress);
            }
            else
            {
                city = GetCity(location);
                Logger.DebugFormat("City: {0}", city);

                // Parse Street Address from location
                e.StreetAddress = GetStreetAddress(location);
                Logger.DebugFormat("StreetAddress: {0}", e.StreetAddress);
            }
            e.City = city;
        }

        /// <summary>
        /// The city code is the last part of the location
        /// </summary>
        private static String GetCity(String location)
        {
            // Split by space.
            var parts = SplitBySpace(location);

            if (parts.Length == 0)
            {
                Logger.ErrorFormat("Unable to parse city from location {0}. Defaulting to Hamilton.", location);
                return "Hamilton";
            }

            var cityCode = parts[parts.Length - 1];

            String city;
            switch (cityCode)
            {
                case "HAM":
                    city = "Hamilton";
                    break;
                case "GL":
                    city = "Glanbrook";
                    break;
                case "FL":
                    city = "Flamborough";
                    break;
                case "SC":
                    city = "Stoney Creek";
                    break;
                case "AN":
                    city = "Ancaster";
                    break;
                case "DU":
                    city = "Dundas";
                    break;
                default:
                    city = "Hamilton";
                    Logger.ErrorFormat("Unknown city {0}. Defaulting to Hamilton.", cityCode);
                    break;
            }

            return city;
        }

        /// <summary>
        /// Change a String like "Loc: 0 Block CANNON ST E HAM @ HUGHSON ST N" to "CANNON ST E & HUGHSON ST N"
        /// </summary>
        private static String GetIntersection(String location)
        {
            // Remove the address part
            location = LocationRegex.Replace(location, String.Empty);
            
            // Remove the city from the end of the first street
            var streets = location.Split('@');
            var firstStreetParts = SplitBySpace(streets[0]).ToList();
            firstStreetParts.RemoveAt(firstStreetParts.Count - 1);

            String intersection = String.Format("{0} & {1}", JoinWithSpace(firstStreetParts), streets[1].Trim());
            return intersection;
        }

        /// <summary>
        /// Change a String like "Loc: 0 Block CANNON ST E HAM" to "1 CANNON ST E"
        /// </summary>
        private static String GetStreetAddress(String location)
        {
            // Get the street number
            var matches = LocationRegex.Matches(location);
            if (matches.Count == 0)
            {
                location = location.Replace("Loc:", string.Empty);
                return location.Trim();
            }
            
            Int32 streetNumber = Convert.ToInt32( matches[0].Groups[1].ToString() );
            if (streetNumber == 0)
                streetNumber++;

            // Remove the street number
            location = LocationRegex.Replace(location, String.Empty);

            // Remove the city
            var parts = SplitBySpace(location).ToList();
            parts.RemoveAt(parts.Count - 1);

            String address = String.Format("{0} {1}", streetNumber, JoinWithSpace(parts));
            return address;
        }

        private static String GetUrl(Int64 since)
        {
            // This should probably be in a config file.
            const String twitterAccount = "HFS_Incidents";

            var urlBuilder = new StringBuilder("https://api.twitter.com/1/statuses/user_timeline.json");
            urlBuilder.AppendFormat("?screen_name={0}", twitterAccount);
            // Hide excess user data.
            urlBuilder.Append("&trim_user=true");
            // Hide entities
            urlBuilder.Append("&include_entities=false");

            if (since != 0)
                urlBuilder.AppendFormat("&since_id={0}", since);
            return urlBuilder.ToString();
        }
    
        private static String[] SplitBySpace(String s)
        {
            return s.Split(SingleSpace, StringSplitOptions.RemoveEmptyEntries);
        }

        private static String JoinWithSpace(IEnumerable<String> s)
        {
            return String.Join(" ", s);
        }
    }
}

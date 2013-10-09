using HamOntFire.Core;
using HamOntFire.Core.Domain;
using NUnit.Framework;

namespace HamOntFire.Test
{
    [TestFixture]
    public class GeoCoderTests
    {
        private Location[] _wardTwoBoundries = new[]
                {
                    new Location(43.244312m, -79.852087m),
                    new Location(43.246172m, -79.861443m),
                    new Location(43.244984m, -79.884489m),
                    new Location(43.269737m, -79.874618m),
                    new Location(43.278158m, -79.861507m),
                    new Location(43.275518m, -79.849856m),
                    new Location(43.252736m, -79.859598m),
                    new Location(43.25022m, -79.849813m),
                };

        [Test]
        public void GetLocation()
        {
            var e = EntityHelper.EventAtGiantTiger();
            var geocoder = new GeoCoder();
            geocoder.GeoCode(e);

            Assert.AreEqual(43.2604215, e.Lat);
            Assert.AreEqual(-79.8659571, e.Long);
        }

        [Test]
        public void GetHighwayExit()
        {
            var e = EntityHelper.EventAtHighwayExit();
            var geocoder = new GeoCoder();
            geocoder.GeoCode(e);

            Assert.AreEqual(43.2186209, e.Lat);
            Assert.AreEqual(-79.6419776, e.Long);
        }

        [Test]
        public void InWard_AnEventInStinson_ShouldReturnTrue()
        {
            var e = new Event {Lat = 43.247039m, Long = -79.855413m};

            var geocoder = new GeoCoder();
            var result = geocoder.EventIsInWard(_wardTwoBoundries, e);
            Assert.IsTrue(result);
        }

        [Test]
        public void InWard_AnEventInDurand_ShouldReturnTrue()
        {
            var e = new Event { Lat = 43.249626m, Long = -79.873835m };

            var geocoder = new GeoCoder();
            var result = geocoder.EventIsInWard(_wardTwoBoundries, e);
            Assert.IsTrue(result);
        }

        [Test]
        public void InWard_AnEventInLandsdale_ShouldReturnFalse()
        {
            var e = new Event { Lat = 43.252752m, Long = -79.854341m };

            var geocoder = new GeoCoder();
            var result = geocoder.EventIsInWard(_wardTwoBoundries, e);
            Assert.IsFalse(result);
        }
    }
}

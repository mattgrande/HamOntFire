using HamOntFire.Core;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;

namespace HamOntFire.Test
{
    [TestFixture]
    public class MapGeneratorTests
    {
        private MapGenerator _generator;
        IDocumentStore _store;
        private IDocumentSession _session;

        [SetUp]
        public void InitialiseTest()
        {
            _store = new EmbeddableDocumentStore
                {
                    RunInMemory = true
                };
            _store.Initialize();

            _session = _store.OpenSession();
            _generator = new MapGenerator();
        }

        [Test]
        public void LocationInFlamboroughShouldHaveLowXLowY()
        {
            var lat = 43.3315696m;
            var lng = -79.909654m;
            int x = _generator.LongitudeToX(lng);
            int y = _generator.LatitudeToY(lat);

            Assert.AreEqual(498, x);
            Assert.AreEqual(283, y);
        }

        [Test]
        public void LocationInAncasterShouldHaveLowXHighY()
        {
            var lat = 43.1930519m;
            var lng = -80.0232868m;
            int x = _generator.LongitudeToX(lng);
            int y = _generator.LatitudeToY(lat);

            Assert.AreEqual(334, x);
            Assert.AreEqual(559, y);
        }

        [Test]
        public void LocationInGlanbrookShouldHaveHighXHighY()
        {
            var lat = 43.1774448m;
            var lng = -79.808948m;
            int x = _generator.LongitudeToX(lng);
            int y = _generator.LatitudeToY(lat);

            Assert.AreEqual(643, x);
            Assert.AreEqual(590, y);
        }

        [Test]
        public void LocationOnSafariRd()
        {
            var lat = 43.3399592m;
            var lng = -80.1516106m;
            int x = _generator.LongitudeToX(lng);
            int y = _generator.LatitudeToY(lat);

            Assert.AreEqual(148, x);
            Assert.AreEqual(267, y);
        }
    }
}
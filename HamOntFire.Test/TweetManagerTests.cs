using System;
using HamOntFire.Core;
using HamOntFire.Core.Domain;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;

namespace HamOntFire.Test
{
    [TestFixture]
    public class TweetManagerTests
    {
        private TweetManager _manager;
        IDocumentStore _store;
        private IDocumentSession _session;

        [SetUp]
        public void InitialiseTest()
        {
            _store = new EmbeddableDocumentStore {
                RunInMemory = true
            };
            _store.Initialize();

            _session = _store.OpenSession();
            _manager = new TweetManager(_session);
        }

        [TearDown]
        public void TearDown()
        {
            _session.Dispose();
        }

        [Test]
        public void Parse_NewEvent_PopulatesEventObject()
        {
            const string input = "NEW | F12036977 | MEDICAL | Loc: 0 Block CANNON ST E HAM @ HUGHSON ST N /CN:GIANT TIGER | Units: 1 | 12/23/12 13:29";
            var @event = _manager.Parse(input);

            Assert.AreEqual("F12036977", @event.Id);
            Assert.AreEqual("Loc: 0 Block CANNON ST E HAM @ HUGHSON ST N /CN:GIANT TIGER", @event.Location);
            Assert.AreEqual("MEDICAL", @event.Type);
            Assert.AreEqual(1, @event.Units);
        }

        [Test]
        public void Parse_NewEvent_PopulatesCommonName()
        {
            const string input = "NEW | F12036977 | MEDICAL | Loc: 0 Block CANNON ST E HAM @ HUGHSON ST N /CN:GIANT TIGER | Units: 1 | 12/23/12 13:29";
            var @event = _manager.Parse(input);

            Assert.AreEqual("GIANT TIGER", @event.CommonName);
        }

        [Test]
        public void Parse_NewEvent_PopulatesCity()
        {
            const string input = "NEW | F12036977 | MEDICAL | Loc: 0 Block CANNON ST E HAM @ HUGHSON ST N /CN:GIANT TIGER | Units: 1 | 12/23/12 13:29";
            var @event = _manager.Parse(input);

            Assert.AreEqual("Hamilton", @event.City);
        }

        [Test]
        public void Parse_NewEvent_PopulatesIntersection()
        {
            const string input = "NEW | F12036977 | MEDICAL | Loc: 0 Block CANNON ST E HAM @ HUGHSON ST N /CN:GIANT TIGER | Units: 1 | 12/23/12 13:29";
            var @event = _manager.Parse(input);

            Assert.AreEqual("CANNON ST E & HUGHSON ST N", @event.Intersection);
        }

        [Test]
        public void Parse_NewEvent_PopulatesStreetAddress()
        {
            const string input = "NEW | F12036977 | MEDICAL | Loc: 0 Block CANNON ST E HAM @ HUGHSON ST N /CN:GIANT TIGER | Units: 1 | 12/23/12 13:29";
            var @event = _manager.Parse(input);

            Assert.AreEqual("1 CANNON ST E", @event.StreetAddress);
        }

        [Test]
        public void Parse_NewEventWithReadMore_PopulatesData()
        {
            const string input = "NEW | F13001080 | MEDICAL | Loc: 300 Block HIGHLAND RD W SC @ PRIVATE RD /PRIVATE RD /CN:SEN... Read more at http://bit.ly/ZxwzHh";
            var @event = _manager.Parse(input);

            Assert.AreEqual("NEW | F13001080 | MEDICAL | Loc: 300 Block HIGHLAND RD W SC @ PRIVATE RD /PRIVATE RD /CN:SENIOR RIDGEVIEW LONG TERM CARE CENTRE | Units: 1 | 1/7/13 10:43", @event.TweetText);
        }

        [Test]
        public void Parse_UpdatedEvent_PopulatesAdditionalInfo()
        {
            // Import a new event
            const string input = "NEW | F12036985 | MEDICAL | Loc: 0 Block SANFORD AV S HAM @ KING ST E /CN:SENIOR CITIZENS APTS | Units: 1 | 12/23/12 16:57";
            var @event = _manager.Parse(input);
            _manager.Save( @event );
            _manager.SaveChanges();

            // Update it
            const string updatedInput = "UPDATE | F12036985 | Add Info: VSA";
            @event = _manager.Parse(updatedInput);
            Assert.AreEqual("VSA", @event.AdditionalInfo);
        }

        [Test]
        public void Parse_UpdatedEvent_PopulatesIncidentType()
        {
            // Import a new event
            const string input = "NEW | F12036967 | ALARM CONDITIONS | Loc: 100 Block DUKE ST HAM | Units: 1 | 12/23/12 11:06";
            var @event = _manager.Parse(input);
            _manager.Save( @event );
            _manager.SaveChanges();

            // Update it
            const string updatedInput = "UPDATE | F12036967 | Incident Type: FALSE ALARM";
            @event = _manager.Parse(updatedInput);
            Assert.AreEqual("FALSE ALARM", @event.Type);
        }

        [Test]
        public void Parse_UpdatedEvent_PopulatesLocation()
        {
            // Import a new event
            const string input = "NEW | F12036992 | VEHICLE ACC | Loc: RYMAL RD E @ RYMAL RD E | Units: 1 | 12/23/12 18:51";
            var @event = _manager.Parse(input);
            _manager.Save( @event );
            _manager.SaveChanges();

            // Update it
            const string updatedInput = "UPDATE | F12036992 | Loc: 900 Block RYMAL RD E HAM /CN:PIONEER";
            @event = _manager.Parse(updatedInput);
            Assert.AreEqual("900 RYMAL RD E", @event.StreetAddress);
            Assert.AreEqual("Hamilton", @event.City);
            Assert.AreEqual("PIONEER", @event.CommonName);
            Assert.IsNull(@event.Intersection);
        }

        [Test]
        public void Parse_UpdatedEvent_PopulatesUnits()
        {
            // Import a new event
            const string input = "NEW | F12036974 | STRUCTURE FIRE | Loc: 1200 Block GOLF CLUB RD GL | Units: 12 | 12/23/12 13:01";
            var @event = _manager.Parse(input);
            _manager.Save(@event);
            _manager.SaveChanges();

            // Update it
            const string updatedInput = "UPDATE | F12036974 | Units: 13";
            @event = _manager.Parse(updatedInput);
            Assert.AreEqual(13, @event.Units);
        }

        [Test]
        public void Parse_HighwayExit_PopulateLocationBasedOnIntersection()
        {
            const string input = "NEW | F12036944 | VEHICLE FIRE | Loc: /CN:EXIT 78 QEW E/B TO 50 RD | Units: 2 | 12/23/12 04:02";
            var @event = _manager.Parse(input);

            Assert.AreEqual("QEW  & Fifty Road", @event.Intersection);
        }

        [Test]
        public void Parse_NoLocationAvailable_PopulatesLocationsAsNull()
        {
            const string input = "NEW | F12036990 | VEHICLE ACC | Location Not Available | Units: 1 | 12/23/12 17:50";
            var @event = _manager.Parse(input);

            Assert.IsNull(@event.CommonName);
            Assert.IsNull(@event.Intersection);
            Assert.AreEqual("Hamilton", @event.City);
        }

        [Test]
        public void Save_Event_SetsCreatedAt()
        {
            var e = EntityHelper.Event("SomeId");
            _manager.Save(e);
            Assert.AreNotEqual(DateTime.MinValue, e.CreatedAt);
        }

        [Test]
        public void Save_Event_SetsUpdatedAt()
        {
            var e = EntityHelper.Event("SomeId");
            _manager.Save(e);
            Assert.AreNotEqual(DateTime.MinValue, e.UpdatedAt);
        }

        [Test]
        public void Save_EventWithCreationDate_DoesNotChangeCreatedAt()
        {
            var e = EntityHelper.Event("SomeId");
            e.CreatedAt = new DateTime(2012, 12, 20);

            _manager.Save(e);
            Assert.AreEqual(new DateTime(2012, 12, 20), e.CreatedAt);
        }

        [Test]
        public void Load_AnExistingId_FindsTheExistingEvent()
        {
            var e = EntityHelper.Event("SomeId");
            _manager.Save(e);
            _manager.SaveChanges();
            var e2 = _manager.Load("SomeId");
            Assert.AreEqual("Hamilton", e2.City);
        }

        [Test]
        public void GetLatest_ReturnsEventsWithinTheLastThreeHours()
        {
            var e1 = EntityHelper.Event("SomeId1");
            _manager.Save(e1);
            var e2 = EntityHelper.Event("SomeId2");
            _manager.Save(e2);
            _manager.SaveChanges();

            var e3 = EntityHelper.Event("SomeId3");
            e3.UpdatedAt = DateTime.UtcNow.AddHours(-5);
            // Save without overriding DateUpdated
            using (var s = _store.OpenSession())
            {
                s.Store(e3);
                s.SaveChanges();
            }

            var results = _manager.GetLatest();
            Assert.AreEqual(2, results.Count);
        }
    }

    public static class EntityHelper
    {
        public static Event Event(string id = "SomeWackyId")
        {
            var e = new Event
                {
                    City = "Hamilton",
                    Id = id,
                    CommonName = "Cool Arcade",
                    Intersection = "John & King",
                    StreetAddress = "123 King St",
                    Type = "MEDICAL",
                    Units = 2
                };
            return e;
        }

        public static Event EventAtGiantTiger()
        {
            var e = new Event
            {
                StreetAddress = "1 CANNON ST E",
                Intersection = "CANNON ST E & HUGHSON ST N",
                City = "Hamilton",
                CommonName = "GIANT TIGER"
            };
            return e;
        }

        public static Event EventAtHighwayExit()
        {
            var e = new Event
            {
                StreetAddress = "",
                Intersection = "QEW & Fifty RD",
                City = "Hamilton",
                CommonName = "EXIT 78 QEW E/B TO 50 RD"
            };
            return e;
        }
    }
}

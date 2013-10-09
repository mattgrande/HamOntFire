using HamOntFire.Core.Domain;
using NUnit.Framework;

namespace HamOntFire.Test.DomainTests
{
    [TestFixture]
    public class EventTests
    {
        // 0 Block CANNON ST E HAM @ HUGHSON ST N /CN:GIANT TIGER
        [Test]
        public void BuildAddressList_LocationInHamilton_BuildsTwoAddresses()
        {
            var e = EntityHelper.EventAtGiantTiger();
            var addresses = e.GetAddressList();
            Assert.AreEqual(2, addresses.Count);

            Assert.AreEqual("CANNON ST E & HUGHSON ST N, Hamilton, ON", addresses[0]);
            Assert.AreEqual("1 CANNON ST E, Hamilton, ON", addresses[1]);
        }

        // 0 Block STONE CHURCH RD AN @ HARROGATE DR
        [Test]
        public void BuildAddressList_LocationInAncaster_BuildsFourAddresses()
        {
            var e = new Event()
                {
                    StreetAddress = "1 STONE CHURCH RD",
                    Intersection = "STONE CHURCH RD & HARROGATE DR",
                    City = "Ancaster",
                };
            var addresses = e.GetAddressList();
            Assert.AreEqual(4, addresses.Count);

            Assert.AreEqual("STONE CHURCH RD & HARROGATE DR, Ancaster, ON", addresses[0]);
            Assert.AreEqual("1 STONE CHURCH RD, Ancaster, ON", addresses[1]);
            Assert.AreEqual("STONE CHURCH RD & HARROGATE DR, Hamilton, ON", addresses[2]);
            Assert.AreEqual("1 STONE CHURCH RD, Hamilton, ON", addresses[3]);
        }

        // 0 Block LOCKTON CR HAM @ PRIVATE RD
        [Test]
        public void BuildAddressList_LocationOnAPrivateRd_BuildsOneAddress()
        {
            var e = new Event
                {
                    StreetAddress = "1 LOCKTON CR",
                    Intersection = "LOCKTON CR & PRIVATE RD",
                    City = "Hamilton",
                };
            var addresses = e.GetAddressList();
            Assert.AreEqual(1, addresses.Count);

            Assert.AreEqual("1 LOCKTON CR, Hamilton, ON", addresses[0]);
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using dnsclient;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dnsclient.Tests
{
    [TestClass()]
    public class DnsClientTests
    {
        [TestMethod()]
        public void ResolveNameAsync_Unknown()
        {

            DnsClient.defaultDNSAddress = "192.168.0.200";

            var domainname = new DomainName("somewellunknown.google.com");

            var task = DnsClient.ResolveNameAsync(domainname, RecordType.A, false, 53, null);

            var result = task.Result;

            Assert.IsTrue(result.Answers.Count == 0);
        }

        [TestMethod()]
        public void ResolveNameAsync_Insecure()
        {

            DnsClient.defaultDNSAddress = "192.168.0.200";

            var domainname = new DomainName("google.com");

            var task = DnsClient.ResolveNameAsync(domainname, RecordType.All, false, 53, null);

            var result = task.Result;

            Assert.IsTrue(result.Answers.Count > 0);
        }

        [TestMethod()]
        public void ResolveNameAsync_RawAlter()
        {

            List<DnsAlterMap> alters = new List<DnsAlterMap> {
                new DnsAlterMap
                {
                    domainName = "google.com",
                    ipAddress = new byte[] { 127, 0, 0, 1 }
                }
            };

            var domain = new DomainName("google.com");

            var acheck = DnsClient.ResolveNameAsync(domain, RecordType.A, false, 53, alters).Result;

            if (acheck.Answers == null)
            {
                Assert.Fail("No answers received");
            }

            Assert.IsTrue(acheck.Answers[0].Address.SequenceEqual(new byte[] { 127, 0, 0, 1 }));
        }


        [TestMethod()]
        public void ResolveNameAsync_MX()
        {

            var domain = new DomainName("google.com");

            var acheck = DnsClient.ResolveNameAsync(domain, RecordType.MX, false, 53).Result;

            Assert.IsTrue(acheck.Answers.Count > 0);
        }

        [TestMethod()]
        public void ResolveNameAsync_Secure()
        {

            var domain = new DomainName("google.com");

            DnsClient.VerifyCert = false;
            var task = DnsClient.ResolveNameAsync(domain, RecordType.A, true, 53, null);

            var result = task.Result;

            Assert.IsTrue(result.Answers.Count > 0);
            DnsClient.VerifyCert = true;
        }
    }
}
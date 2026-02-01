using Desafio.Umbler.Controllers;
using Desafio.Umbler.Models;
using Desafio.Umbler.Services.Dns;
using DnsClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;


namespace Desafio.Umbler.Test
{
    [TestClass]
    public class ControllersTest
    {
        [TestMethod]
        public void Home_Index_returns_View()
        {
            //arrange 
            var controller = new HomeController();

            //act
            var response = controller.Index();
            var result = response as ViewResult;

            //assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Home_Error_returns_View_With_Model()
        {
            //arrange 
            var controller = new HomeController();
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            //act
            var response = controller.Error();
            var result = response as ViewResult;
            var model = result.Model as ErrorViewModel;

            //assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(model);
        }
        
        [TestMethod]
        public void Domain_In_Database()
        {
            //arrange 
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: "Find_searches_url")
                .Options;

            var domain = new Domain { Id = 1, Ip = "192.168.0.1", Name = "test.com", UpdatedAt = DateTime.Now, HostedAt = "umbler.corp", Ttl = 60, WhoIs = "Ns.umbler.com" };

            // Insert seed data into the database using one instance of the context
            using (var db = new DatabaseContext(options))
            {
                db.Domains.Add(domain);
                db.SaveChanges();
            }

            // Use a clean instance of the context to run the test
            using (var db = new DatabaseContext(options))
            {
                var whois = new Mock<Desafio.Umbler.Services.Whois.IWhoisClient>();
                var dns = new Mock<Desafio.Umbler.Services.Dns.IDnsLookupClient>();

                var controller = new DomainController(db, whois.Object, dns.Object);


                //act
                var response = controller.Get("test.com");
                var result = response.Result as OkObjectResult;
                var obj = result.Value as Domain;
                Assert.AreEqual(obj.Id, domain.Id);
                Assert.AreEqual(obj.Ip, domain.Ip);
                Assert.AreEqual(obj.Name, domain.Name);
            }
        }

        [TestMethod]
        public void Domain_Not_In_Database()
        {
            //arrange 
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: "Find_searches_url")
                .Options;

            // Use a clean instance of the context to run the test
            using (var db = new DatabaseContext(options))
            {
                var whois = new Mock<Desafio.Umbler.Services.Whois.IWhoisClient>();
                var dns = new Mock<Desafio.Umbler.Services.Dns.IDnsLookupClient>();

                // 1) WHOIS do domínio
                whois.Setup(x => x.QueryAsync("test.com"))
                     .ReturnsAsync(new Desafio.Umbler.Services.Whois.WhoisResult
                     {
                         Raw = "whois-domain",
                         OrganizationName = "Org"
                     });

                // 2) DNS do domínio
                dns.Setup(x => x.LookupAsync("test.com"))
                   .ReturnsAsync(new Desafio.Umbler.Services.Dns.DnsLookupResult
                   {
                       Ip = "1.2.3.4",
                       Ttl = 60
                   });

                // 3) WHOIS do IP (HostedAt)
                whois.Setup(x => x.QueryAsync("1.2.3.4"))
                     .ReturnsAsync(new Desafio.Umbler.Services.Whois.WhoisResult
                     {
                         Raw = "whois-ip",
                         OrganizationName = "Umbler"
                     });

                var controller = new DomainController(db, whois.Object, dns.Object);

                //act
                var response = controller.Get("test.com");
                var result = response.Result as OkObjectResult;
                var obj = result.Value as Domain;

                Assert.IsNotNull(obj);
            }
        }

        [TestMethod]
        public void Domain_Moking_LookupClient()
        {
            // arrange
            var domainName = "test.com";

            var whoisClient = new Mock<Desafio.Umbler.Services.Whois.IWhoisClient>();
            var dnsClient = new Mock<Desafio.Umbler.Services.Dns.IDnsLookupClient>();

            // WHOIS do domínio
            whoisClient
                .Setup(w => w.QueryAsync(domainName))
                .ReturnsAsync(new Desafio.Umbler.Services.Whois.WhoisResult
                {
                    Raw = "raw-whois-domain",
                    OrganizationName = "Example Org"
                });

            // DNS do domínio
            dnsClient
                .Setup(d => d.LookupAsync(domainName))
                .ReturnsAsync(new Desafio.Umbler.Services.Dns.DnsLookupResult
                {
                    Ip = "1.2.3.4",
                    Ttl = 60
                });

            // WHOIS do IP (HostedAt)
            whoisClient
                .Setup(w => w.QueryAsync("1.2.3.4"))
                .ReturnsAsync(new Desafio.Umbler.Services.Whois.WhoisResult
                {
                    Raw = "raw-whois-ip",
                    OrganizationName = "Umbler"
                });

            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var db = new DatabaseContext(options))
            {
                var controller = new DomainController(db, whoisClient.Object, dnsClient.Object);

                // act
                var response = controller.Get(domainName);
                var result = response.Result as OkObjectResult;
                var obj = result.Value as Domain;

                // assert
                Assert.IsNotNull(obj);
                Assert.AreEqual(domainName, obj.Name);
                Assert.AreEqual("1.2.3.4", obj.Ip);
                Assert.AreEqual(60, obj.Ttl);
            }
        }


        [TestMethod]
        public void Domain_Moking_WhoisClient()
        {
            //arrange
            var domainName = "test.com";

            var whoisClient = new Mock<Desafio.Umbler.Services.Whois.IWhoisClient>();
            var dnsClient = new Mock<Desafio.Umbler.Services.Dns.IDnsLookupClient>();

            whoisClient.Setup(x => x.QueryAsync(domainName))
                .ReturnsAsync(new Desafio.Umbler.Services.Whois.WhoisResult
                {
                    Raw = "raw-domain",
                    OrganizationName = "Example Org"
                });

            dnsClient.Setup(x => x.LookupAsync(domainName))
                .ReturnsAsync(new Desafio.Umbler.Services.Dns.DnsLookupResult
                {
                    Ip = "1.2.3.4",
                    Ttl = 60
                });

            whoisClient.Setup(x => x.QueryAsync("1.2.3.4"))
                .ReturnsAsync(new Desafio.Umbler.Services.Whois.WhoisResult
                {
                    Raw = "raw-ip",
                    OrganizationName = "Umbler"
                });

            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: "Find_searches_url")
                .Options;


            //// Use a clean instance of the context to run the test
            using (var db = new DatabaseContext(options))
            {
                    //inject IWhoisClient in controller's constructor
                var whois = new Mock<Desafio.Umbler.Services.Whois.IWhoisClient>();
                var dns = new Mock<Desafio.Umbler.Services.Dns.IDnsLookupClient>();

                var controller = new DomainController(db, whois.Object, dns.Object);


                    //act
                    var response = controller.Get("test.com");
                    var result = response.Result as OkObjectResult;
                    var obj = result.Value as Domain;
                    Assert.IsNotNull(obj);
                }
            }
        }
}
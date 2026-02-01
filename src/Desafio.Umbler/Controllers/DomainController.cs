using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Desafio.Umbler.Models;
using Whois.NET;
using Microsoft.EntityFrameworkCore;
using DnsClient;
using Desafio.Umbler.Services.Dns;
using Desafio.Umbler.Services.Whois;


namespace Desafio.Umbler.Controllers
{
    [Route("api")]
    public class DomainController : Controller
    {
        private readonly DatabaseContext _db;
        private readonly IWhoisClient _whois;
        private readonly IDnsLookupClient _dns;

        public DomainController(DatabaseContext db, IWhoisClient whois, IDnsLookupClient dns)
        {
            _db = db;
            _whois = whois;
            _dns = dns;
        }


        [HttpGet, Route("domain/{domainName}")]
        public async Task<IActionResult> Get(string domainName)
        {
            // Validar domínio sem .com
            domainName = (domainName ?? "").Trim().ToLowerInvariant();

            domainName = domainName
                .Replace("http://", "")
                .Replace("https://", "")
                .Split('/')[0];

            if (string.IsNullOrWhiteSpace(domainName) || !domainName.Contains("."))
                return BadRequest("Domínio inválido. Ex: google.com");


            var domain = await _db.Domains.FirstOrDefaultAsync(d => d.Name == domainName);

            if (domain == null)
            {
                var response = await _whois.QueryAsync(domainName);

                var dnsResult = await _dns.LookupAsync(domainName);
                var ip = dnsResult.Ip;

                var hostResponse = ip == null ? null : await _whois.QueryAsync(ip);

                domain = new Domain
                {
                    Name = domainName,
                    Ip = ip,
                    UpdatedAt = DateTime.Now,
                    WhoIs = response.Raw,
                    Ttl = dnsResult.Ttl,
                    HostedAt = hostResponse?.OrganizationName
                };

                _db.Domains.Add(domain);
            }

            if (DateTime.Now.Subtract(domain.UpdatedAt).TotalMinutes > domain.Ttl)
            {
                var response = await _whois.QueryAsync(domainName);

                var dnsResult = await _dns.LookupAsync(domainName);
                var ip = dnsResult.Ip;

                var hostResponse = ip == null ? null : await _whois.QueryAsync(ip);

                domain.Name = domainName;
                domain.Ip = ip;
                domain.UpdatedAt = DateTime.Now;
                domain.WhoIs = response.Raw;
                domain.Ttl = dnsResult.Ttl;
                domain.HostedAt = hostResponse?.OrganizationName;
            }

            await _db.SaveChangesAsync();

            return Ok(domain);
        }
    }
}

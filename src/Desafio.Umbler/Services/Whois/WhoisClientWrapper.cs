using System.Threading.Tasks;
using Whois.NET;

namespace Desafio.Umbler.Services.Whois
{
    public class WhoisClientWrapper : IWhoisClient
    {
        public async Task<WhoisResult> QueryAsync(string query)
        {
            var resp = await WhoisClient.QueryAsync(query);

            return new WhoisResult
            {
                Raw = resp.Raw,
                OrganizationName = resp.OrganizationName
            };
        }
    }
}

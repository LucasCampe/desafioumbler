using System.Threading.Tasks;

namespace Desafio.Umbler.Services.Whois
{
    public class WhoisResult
    {
        public string Raw { get; set; }
        public string OrganizationName { get; set; }
    }

    public interface IWhoisClient
    {
        Task<WhoisResult> QueryAsync(string query);
    }
}

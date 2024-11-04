using System.Threading.Tasks;
using WalletSystem.Models;
using WalletSystem.Protocol.Requests;

namespace WalletSystem.Services
{
    public interface IWalletService
    {
        Task CreateWalletAsync(CreateWalletRequest request);
        Task AddBalanceAsync(AddBalanceRequest request);
        Task DeductBalanceAsync(DeductBalanceRequest request);
        Task<decimal> GetBalanceAsync(GetBalanceRequest request);
    }
}

using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WalletSystem.Protocol.Requests;
using WalletSystem.Services;

namespace WalletSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpPost("createWallet")]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request)
        {
            try
            {
                await _walletService.CreateWalletAsync(request);
                return Ok(new { message = "Wallet created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("addBalance")]
        public async Task<IActionResult> AddBalance([FromBody] AddBalanceRequest request)
        {
            try
            {
                await _walletService.AddBalanceAsync(request);
                return Ok(new { message = "Balance added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("deductBalance")]
        public async Task<IActionResult> DeductBalance([FromBody] DeductBalanceRequest request)
        {
            try
            {
                await _walletService.DeductBalanceAsync(request);
                return Ok(new { message = "Balance deducted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("getBalance")]
        public async Task<IActionResult> GetBalance([FromQuery] GetBalanceRequest request)
        {
            try
            {
                var balance = await _walletService.GetBalanceAsync(request);
                return Ok(new { balance });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}

namespace WalletSystemClient.Protocol.Requests
{
    public class AddBalanceRequest
    {
        public Guid PlayerId { get; set; }
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; }
    }
}

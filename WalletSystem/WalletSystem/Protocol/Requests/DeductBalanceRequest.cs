﻿namespace WalletSystem.Protocol.Requests
{
    public class DeductBalanceRequest
    {
        public Guid PlayerId { get; set; }
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; }
    }
}
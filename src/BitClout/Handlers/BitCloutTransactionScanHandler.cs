using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Contracts;
    using Models;
    using Models.Scanner;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class BitCloutTransactionScanHandler : IRequestHandler<BitCloutTransactionScanRequest, List<IBitCloutFundingTransaction>>
    {
        private readonly IBitCloutRestFactory _factory;
        private readonly ILog _logger;

        public BitCloutTransactionScanHandler(IBitCloutRestFactory factory, ILog logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public Task<List<IBitCloutFundingTransaction>> Handle(BitCloutTransactionScanRequest request, CancellationToken cancellationToken)
        {
            var wallet = request.WalletPublicKey;

            var client = _factory.CreateClient(BitCloutEndPoints.MoneyScan);
            var policy = _factory.CreatePolicy<Application>(5);
            var body = new { PublicKeyBase58Check = wallet };
            var root = _factory.Execute(client, policy, body);

            var transactions = root?.Transactions ?? new List<Transactions>();
            _logger.Info($"Found {transactions.Count} transactions");

            var result = Incoming(transactions, wallet)
                .Union(Outgoing(transactions, wallet))
                .ToList();

            _logger.Info($"Found {result.Count} matching transactions");

            return Task.FromResult(result);
        }

        private IEnumerable<IBitCloutFundingTransaction> Incoming(IEnumerable<Transactions> source, string wallet) =>
            source
                .Where(t =>
                    t.TransactionType != null && t.Is(BitCloutTransactionTypes.BASIC_TRANSFER) &&
                    t.TransactionMetadata != null &&
                    t.TransactionMetadata.IsNotTransactor(wallet) && // Initiator of Transaction Is the External User
                    t.Outputs != null &&
                    t.Outputs.Any(x => x.PublicKeyBase58Check.Equals(wallet)))
                .Select(tx => new BitCloutIncomingFunds
                {
                    Amount =
                        (long)(tx.Outputs.FirstOrDefault(x => x.PublicKeyBase58Check.Equals(wallet))?.AmountNanos ?? 0),
                    EvidencePostHex = tx.TransactionIDBase58Check,
                    UserPublicKey = tx.TransactionMetadata.TransactorPublicKeyBase58Check
                });

        private IEnumerable<IBitCloutFundingTransaction> Outgoing(IEnumerable<Transactions> source, string wallet) =>
            source
                .Where(t =>
                    t.TransactionType != null && t.Is(BitCloutTransactionTypes.BASIC_TRANSFER) &&
                    t.TransactionMetadata != null && t.TransactionMetadata.IsTransactor(wallet) && // Initiator of Transaction IS the wallet
                    t.Outputs != null &&
                    t.Outputs.Count > 1 &&
                    t.Outputs[1].PublicKeyBase58Check.Equals(wallet))
                .Select(tx => new BitCloutOutGoingFunds
                {
                    Amount = (long) tx.Outputs[0].AmountNanos,
                    EvidencePostHex = tx.TransactionIDBase58Check,
                    UserPublicKey = tx.Outputs[0].PublicKeyBase58Check
                });
    }
}
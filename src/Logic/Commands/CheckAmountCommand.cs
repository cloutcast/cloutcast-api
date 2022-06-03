using System;

namespace CloutCast.Commands
{
    using Models;
    
    public interface ICheckAmountCommand : IDapperCommand
    {
        ICheckAmountCommand Ledger(GeneralLedgerType ledger);
        ICheckAmountCommand AccountOwner(GeneralLedgerAccountType type, long id);
        ICheckAmountCommand Amount(long amount);
        ICheckAmountCommand ErrorMessage(string message);
    }
    public class CheckAmountCommand : DapperCommand, ICheckAmountCommand
    {
        private readonly IFetchBalanceForUserCommand _fetchBalance;
        public CheckAmountCommand(IFetchBalanceForUserCommand fetchBalance) => _fetchBalance = fetchBalance;

        private long _amount;
        private GeneralLedgerType _ledger;
        private string _message;
        private GLAccountOwnerModel _owner;
        
        public ICheckAmountCommand AccountOwner(GeneralLedgerAccountType type, long id) => this.Fluent(x => _owner = new GLAccountOwnerModel {Id = id, Type = type});
        public ICheckAmountCommand Amount(long amount) => this.Fluent(x => _amount = amount);
        public ICheckAmountCommand Ledger(GeneralLedgerType ledger) => this.Fluent(x => _ledger = ledger);
        public ICheckAmountCommand ErrorMessage(string message) => this.Fluent(x => _message = message);

        public override void Build(IStatementBuilder builder)
        {
            _fetchBalance
                .AsOf(DateTimeOffset.UtcNow)
                .AccountOwner(_owner)
                .TotalFor(_ledger, $"Total{_ledger}")
                .Build(builder);

            builder
                .Param("ErrorMessage", $"{_message}; Available {_ledger} = ")
                .Add($@"
IF (@Total{_ledger} < {_amount} ) BEGIN
  SET @ErrorMessage = concat(@ErrorMessage, trim(str(ABS(@Total{_ledger}))));
  THROW 50402, @ErrorMessage, 1
END");
        }
    }
}
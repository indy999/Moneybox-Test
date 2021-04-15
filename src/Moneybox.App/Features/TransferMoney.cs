namespace Moneybox.App.Features
{
    using Moneybox.App.DataAccess;
    using Moneybox.App.Domain.Services;
    using System;

    public class TransferMoney
    {
        private readonly IAccountRepository _accountRepository;
        private readonly INotificationService _notificationService;

        public TransferMoney(IAccountRepository accountRepository, INotificationService notificationService)
        {
            this._accountRepository = accountRepository;
            this._notificationService = notificationService;
        }

        public void Execute(Guid fromAccountId, Guid toAccountId, decimal amount)
        {
            var sourceAccount = _accountRepository.GetAccountById(fromAccountId);
            var destinationAccount = _accountRepository.GetAccountById(toAccountId);

            sourceAccount.Withdraw(amount);
            destinationAccount.Deposit(amount);

            if (sourceAccount.HasLowFunds())
            {
                _notificationService.NotifyFundsLow(sourceAccount.User.Email);
            }

            if (destinationAccount.IsApproachingPayInLimit())
            {
                _notificationService.NotifyApproachingPayInLimit(destinationAccount.User.Email);
            }

            _accountRepository.Update(sourceAccount);
            _accountRepository.Update(destinationAccount);
        }
    }
}

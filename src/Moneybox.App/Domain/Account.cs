namespace Moneybox.App.Domain
{
    using System;

    public class Account
    {
        public const decimal PayInLimit = 4000m;
        
        public Account(Guid id, User user, decimal balance, decimal withdrawn, decimal paidIn)
        {
            Id = id;
            User = user;
            Balance = balance;
            Withdrawn = withdrawn;
            PaidIn = paidIn;
        }

        public Guid Id { get; private set; }

        public User User { get; private set; }

        public decimal Balance { get; private set; }

        public decimal Withdrawn { get; private set; }

        public decimal PaidIn { get; private set; }

        public void Withdraw(decimal amount)
        {
            var updatedBalance = Balance - amount;
            if (updatedBalance < 0m)
            {
                throw new InvalidOperationException("Insufficient funds to make transfer");
            }

            Balance = updatedBalance;
            Withdrawn -= amount;
        }

        public void Deposit(decimal amount)
        {
            var updatedDeposit = PaidIn + amount;
            if (updatedDeposit > PayInLimit)
            {
                throw new InvalidOperationException("Account pay in limit reached");
            }

            Balance += amount;
            PaidIn = updatedDeposit;
        }

        public bool HasLowFunds()
        {
            return Balance < 500m;
        }

        public bool IsApproachingPayInLimit()
        {
            return PayInLimit - PaidIn < 500m;
        }
    }
}

namespace Moneybox.IntegrationTests
{
    using App;
    using Moneybox.App.DataAccess;
    using System;
    using System.Collections.Concurrent;
    using App.Domain;

    public class TestAccountRepository : IAccountRepository
    {
        private readonly ConcurrentDictionary<Guid, Account> _testAccounts = new();

        
        public void Update(Account account)
        {
            _testAccounts.AddOrUpdate(account.Id, account, (key, value) => account);
        }

        public Account GetAccountById(Guid accountId)
        {
            return _testAccounts[accountId];
        }
        
    }
}

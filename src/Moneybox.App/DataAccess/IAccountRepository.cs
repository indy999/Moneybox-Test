namespace Moneybox.App.DataAccess
{
    using Domain;
    using System;

    public interface IAccountRepository
    {
        Account GetAccountById(Guid accountId);

        void Update(Account account);
    }
}

namespace Moneybox.UnitTests
{
    using App.DataAccess;
    using App.Domain;
    using App.Domain.Services;
    using App.Features;
    using FluentAssertions;
    using Moq;
    using System;
    using Xunit;


    public class WithdrawMoneyShould
    {
        private readonly Mock<IAccountRepository> _accountRepositoryMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        private readonly User _sourceUser;
        private readonly Guid _sourceAccountGuid;
        private Account _sourceAccount;

        private WithdrawMoney withdrawMoneyService;

        public WithdrawMoneyShould()
        {
            _sourceUser = new User(Guid.NewGuid(), "Andy Smith", "andy.smith@test.com");

            _sourceAccountGuid = Guid.NewGuid();

            _accountRepositoryMock = new Mock<IAccountRepository>();
            _notificationServiceMock = new Mock<INotificationService>();
            withdrawMoneyService = new WithdrawMoney(_accountRepositoryMock.Object, _notificationServiceMock.Object);
        }

        [Fact]
        public void SuccessfullyWithdrawMoneyFromAccount()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);

            _accountRepositoryMock.Setup(x => x.GetAccountById(_sourceAccountGuid))
                .Returns(_sourceAccount);

            _accountRepositoryMock.Setup(x => x.Update(_sourceAccount)).Verifiable();

            withdrawMoneyService.Execute(_sourceAccountGuid, 200);

            _accountRepositoryMock.Verify(x => x.Update(_sourceAccount), Times.Once);
        }

        [Fact]
        public void NotifyAccountUserIfFundsAreLow()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);

            _accountRepositoryMock.Setup(x => x.GetAccountById(_sourceAccountGuid))
                .Returns(_sourceAccount);

            _notificationServiceMock.Setup(x => x.NotifyFundsLow(_sourceUser.Email)).Verifiable();

            _accountRepositoryMock.Setup(x => x.Update(_sourceAccount)).Verifiable();

            withdrawMoneyService.Execute(_sourceAccountGuid,  400);

            _notificationServiceMock.Verify(x => x.NotifyFundsLow(_sourceUser.Email), Times.Once);
            _accountRepositoryMock.Verify(x => x.Update(_sourceAccount), Times.Once);
        }

        [Fact]
        public void ThrowInvalidOperationExceptionWhenAccountHasInsufficientFunds()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);

            _accountRepositoryMock.Setup(x => x.GetAccountById(_sourceAccountGuid))
                .Returns(_sourceAccount);

            var exception = Assert.Throws<InvalidOperationException>(() => withdrawMoneyService.Execute(_sourceAccountGuid,
                 1000));

            _accountRepositoryMock.Verify(x => x.Update(_sourceAccount), Times.Never);
            exception.Message.Should().Be("Insufficient funds to make transfer");
        }
    }
}

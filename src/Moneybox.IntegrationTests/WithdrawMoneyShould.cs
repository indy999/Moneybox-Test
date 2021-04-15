namespace Moneybox.IntegrationTests
{
    using App.Domain;
    using App.Domain.Services;
    using App.Features;
    using FluentAssertions;
    using Moq;
    using System;
    using Xunit;

    public class WithdrawMoneyShould
    {
        private readonly TestAccountRepository _testAccountRepository;
        private readonly Mock<INotificationService> _notificationServiceMock;

        private readonly User _sourceUser;
        private readonly Guid _sourceAccountGuid;
        private Account _sourceAccount;

        private readonly WithdrawMoney withdrawMoneyService;

        public WithdrawMoneyShould()
        {
            _sourceUser = new User(Guid.NewGuid(), "Andy Smith", "andy.smith@test.com");

            _sourceAccountGuid = Guid.NewGuid();

            _testAccountRepository = new TestAccountRepository();
            _notificationServiceMock = new Mock<INotificationService>();
            withdrawMoneyService = new WithdrawMoney(_testAccountRepository, _notificationServiceMock.Object);
        }

        [Fact]
        public void SuccessfullyWithdrawMoneyFromAccount()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);
            SetupTestAccount();
            withdrawMoneyService.Execute(_sourceAccountGuid, 200);

            var fromAccount = _testAccountRepository.GetAccountById(_sourceAccountGuid);
            fromAccount.Balance.Should().Be(650);
        }

        [Fact]
        public void NotifyAccountUserIfFundsAreLow()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);

            SetupTestAccount();
            _notificationServiceMock.Setup(x => x.NotifyFundsLow(_sourceUser.Email)).Verifiable();

            withdrawMoneyService.Execute(_sourceAccountGuid,  400);

            _notificationServiceMock.Verify(x => x.NotifyFundsLow(_sourceUser.Email), Times.Once);
            
            var fromAccount = _testAccountRepository.GetAccountById(_sourceAccountGuid);
            fromAccount.Balance.Should().Be(450);
        }

        [Fact]
        public void ThrowInvalidOperationExceptionWhenAccountHasInsufficientFunds()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);

            SetupTestAccount();

            var exception = Assert.Throws<InvalidOperationException>(() => withdrawMoneyService.Execute(_sourceAccountGuid,
                 1000));
            exception.Message.Should().Be("Insufficient funds to make transfer");

            var fromAccount = _testAccountRepository.GetAccountById(_sourceAccountGuid);
            fromAccount.Balance.Should().Be(850);
        }

        private void SetupTestAccount()
        {
            _testAccountRepository.Update(_sourceAccount);
        }
    }
}

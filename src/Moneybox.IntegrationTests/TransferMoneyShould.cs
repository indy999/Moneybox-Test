namespace Moneybox.IntegrationTests
{
    using App.Domain;
    using App.Domain.Services;
    using App.Features;
    using FluentAssertions;
    using Moq;
    using System;
    using Xunit;

    public class TransferMoneyShould
    {
        private readonly TestAccountRepository _testAccountRepository;
        private readonly Mock<INotificationService> _notificationServiceMock;

        private User _sourceUser;
        private User _destinationUser;
        private Guid _sourceAccountGuid;
        private Account _sourceAccount;
        private Guid _destinationAccountGuid;
        private Account _destinationAccount;

        private readonly TransferMoney transformMoneyService;

        public TransferMoneyShould()
        {
            SetupTestUsersAndAccounts();

            _testAccountRepository = new TestAccountRepository();
            _notificationServiceMock = new Mock<INotificationService>();
            transformMoneyService = new TransferMoney(_testAccountRepository, _notificationServiceMock.Object);
        }

       

        [Fact]
        public void SuccessfullyTransferMoneyBetweenTwoAccounts()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);
            _destinationAccount = new Account(_destinationAccountGuid, _destinationUser, 2000, 300, 2300);
            
            SetupTestAccounts();

            transformMoneyService.Execute(_sourceAccountGuid, _destinationAccountGuid, 200);

            var fromAccount = _testAccountRepository.GetAccountById(_sourceAccountGuid);
            var toAccount = _testAccountRepository.GetAccountById(_destinationAccountGuid);

            fromAccount.Balance.Should().Be(650);
            toAccount.Balance.Should().Be(2200);
        }

        [Fact]
        public void NotifySourceAccountUserIfFundsAreLow()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);
            _destinationAccount = new Account(_destinationAccountGuid, _destinationUser, 2000, 300, 2300);

            SetupTestAccounts();

            _notificationServiceMock.Setup(x => x.NotifyFundsLow(_sourceUser.Email)).Verifiable();

            transformMoneyService.Execute(_sourceAccountGuid, _destinationAccountGuid, 400);

            _notificationServiceMock.Verify(x=>x.NotifyFundsLow(_sourceUser.Email),Times.Once);

            var fromAccount = _testAccountRepository.GetAccountById(_sourceAccountGuid);
            var toAccount = _testAccountRepository.GetAccountById(_destinationAccountGuid);

            fromAccount.Balance.Should().Be(450);
            toAccount.Balance.Should().Be(2400);
        }

        [Fact]
        public void NotifyDestinationAccountUserIfApproachingPayInLimit()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 2000, 200, 2200);
            _destinationAccount = new Account(_destinationAccountGuid, _destinationUser, 2000, 300, 2300);

            SetupTestAccounts();

            _notificationServiceMock.Setup(x => x.NotifyFundsLow(_sourceUser.Email)).Verifiable();

            transformMoneyService.Execute(_sourceAccountGuid, _destinationAccountGuid, 1300);

            _notificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(_destinationUser.Email), Times.Once);

            var fromAccount = _testAccountRepository.GetAccountById(_sourceAccountGuid);
            var toAccount = _testAccountRepository.GetAccountById(_destinationAccountGuid);

            fromAccount.Balance.Should().Be(700);
            toAccount.Balance.Should().Be(3300);
        }

        [Fact]
        public void ThrowInvalidOperationExceptionWhenSourceAccountHasInsufficientFunds()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);
            _destinationAccount = new Account(_destinationAccountGuid, _destinationUser, 2000, 300, 2300);
            
            SetupTestAccounts();

            var exception = Assert.Throws<InvalidOperationException>(()=> transformMoneyService.Execute(_sourceAccountGuid,
                _destinationAccountGuid, 1000));

            exception.Message.Should().Be("Insufficient funds to make transfer");

            var fromAccount = _testAccountRepository.GetAccountById(_sourceAccountGuid);
            var toAccount = _testAccountRepository.GetAccountById(_destinationAccountGuid);

            fromAccount.Balance.Should().Be(850);
            toAccount.Balance.Should().Be(2000);
        }

        [Fact]
        public void ThrowInvalidOperationExceptionWhenDestinationAccountReachesPayInLimit()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 2000, 200, 2200);
            _destinationAccount = new Account(_destinationAccountGuid, _destinationUser, 2000, 300, 2300);

            SetupTestAccounts();

            _notificationServiceMock.Setup(x => x.NotifyFundsLow(_sourceUser.Email)).Verifiable();

            var exception = Assert.Throws<InvalidOperationException>(() => transformMoneyService.Execute(_sourceAccountGuid,
                _destinationAccountGuid, 1800));

            exception.Message.Should().Be("Account pay in limit reached");

            var fromAccount = _testAccountRepository.GetAccountById(_sourceAccountGuid);
            var toAccount = _testAccountRepository.GetAccountById(_destinationAccountGuid);

            fromAccount.Balance.Should().Be(200);
            toAccount.Balance.Should().Be(2000);
        }

        private void SetupTestAccounts()
        {
            _testAccountRepository.Update(_sourceAccount);
            _testAccountRepository.Update(_destinationAccount);
        }

        private void SetupTestUsersAndAccounts()
        {
            _sourceUser = new User(Guid.NewGuid(), "Andy Smith", "andy.smith@test.com");
            _destinationUser = new User(Guid.NewGuid(), "Sarah Jones", "sarah.jones@test.com");

            _sourceAccountGuid = Guid.NewGuid();
            _destinationAccountGuid = Guid.NewGuid();
        }
    }
}

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

    public class TransferMoneyShould
    {
        private readonly Mock<IAccountRepository> _accountRepositoryMock;
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

            _accountRepositoryMock = new Mock<IAccountRepository>();
            _notificationServiceMock = new Mock<INotificationService>();
            transformMoneyService = new TransferMoney(_accountRepositoryMock.Object, _notificationServiceMock.Object);
        }

        [Fact]
        public void SuccessfullyTransferMoneyBetweenTwoAccounts()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);
            _destinationAccount = new Account(_destinationAccountGuid, _destinationUser, 2000, 300, 2300);

            SetupAccountRepository();

            transformMoneyService.Execute(_sourceAccountGuid, _destinationAccountGuid, 200);

            _accountRepositoryMock.Verify(x=>x.Update(_sourceAccount),Times.Once);
            _accountRepositoryMock.Verify(x => x.Update(_destinationAccount), Times.Once);
        }

       [Fact]
        public void NotifySourceAccountUserIfFundsAreLow()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);
            _destinationAccount = new Account(_destinationAccountGuid, _destinationUser, 2000, 300, 2300);

            SetupAccountRepository();

            _notificationServiceMock.Setup(x => x.NotifyFundsLow(_sourceUser.Email)).Verifiable();
            
            transformMoneyService.Execute(_sourceAccountGuid, _destinationAccountGuid, 400);

            _accountRepositoryMock.Verify(x => x.Update(_sourceAccount), Times.Once);
            _accountRepositoryMock.Verify(x => x.Update(_destinationAccount), Times.Once);

            _notificationServiceMock.Verify(x=>x.NotifyFundsLow(_sourceUser.Email),Times.Once);
        }

        [Fact]
        public void NotifyDestinationAccountUserIfApproachingPayInLimit()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 2000, 200, 2200);
            _destinationAccount = new Account(_destinationAccountGuid, _destinationUser, 2000, 300, 2300);

            SetupAccountRepository();

            _notificationServiceMock.Setup(x => x.NotifyFundsLow(_sourceUser.Email)).Verifiable();

            transformMoneyService.Execute(_sourceAccountGuid, _destinationAccountGuid, 1300);

            _accountRepositoryMock.Verify(x => x.Update(_sourceAccount), Times.Once);
            _accountRepositoryMock.Verify(x => x.Update(_destinationAccount), Times.Once);

            _notificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(_destinationUser.Email), Times.Once);
        }

        [Fact]
        public void ThrowInvalidOperationExceptionWhenSourceAccountHasInsufficientFunds()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 850, 150, 1000);
            _destinationAccount = new Account(_destinationAccountGuid, _destinationUser, 2000, 300, 2300);

            _accountRepositoryMock.Setup(x => x.GetAccountById(_sourceAccountGuid))
                .Returns(_sourceAccount);

            _accountRepositoryMock.Setup(x => x.GetAccountById(_destinationAccountGuid))
                .Returns(_destinationAccount);

            var exception = Assert.Throws<InvalidOperationException>(()=> transformMoneyService.Execute(_sourceAccountGuid,
                _destinationAccountGuid, 1000));

            _accountRepositoryMock.Verify(x => x.Update(_sourceAccount), Times.Never);
            _accountRepositoryMock.Verify(x => x.Update(_destinationAccount), Times.Never);
            exception.Message.Should().Be("Insufficient funds to make transfer");
        }

        [Fact]
        public void ThrowInvalidOperationExceptionWhenDestinationAccountReachesPayInLimit()
        {
            _sourceAccount = new Account(_sourceAccountGuid, _sourceUser, 2000, 200, 2200);
            _destinationAccount = new Account(_destinationAccountGuid, _destinationUser, 2000, 300, 2300);

            _accountRepositoryMock.Setup(x => x.GetAccountById(_sourceAccountGuid))
                .Returns(_sourceAccount);

            _accountRepositoryMock.Setup(x => x.GetAccountById(_destinationAccountGuid))
                .Returns(_destinationAccount);

            _notificationServiceMock.Setup(x => x.NotifyFundsLow(_sourceUser.Email)).Verifiable();

            var exception = Assert.Throws<InvalidOperationException>(() => transformMoneyService.Execute(_sourceAccountGuid,
                _destinationAccountGuid, 1800));

            _accountRepositoryMock.Verify(x => x.Update(_sourceAccount), Times.Never);
            _accountRepositoryMock.Verify(x => x.Update(_destinationAccount), Times.Never);
            exception.Message.Should().Be("Account pay in limit reached");
        }

        private void SetupTestUsersAndAccounts()
        {
            _sourceUser = new User(Guid.NewGuid(), "Andy Smith", "andy.smoth@test.com");
            _destinationUser = new User(Guid.NewGuid(), "Sarah Jones", "sarah.jones@test.com");

            _sourceAccountGuid = Guid.NewGuid();
            _destinationAccountGuid = Guid.NewGuid();
        }

        private void SetupAccountRepository()
        {
            _accountRepositoryMock.Setup(x => x.GetAccountById(_sourceAccountGuid))
                .Returns(_sourceAccount);

            _accountRepositoryMock.Setup(x => x.GetAccountById(_destinationAccountGuid))
                .Returns(_destinationAccount);

            _accountRepositoryMock.Setup(x => x.Update(_sourceAccount)).Verifiable();
            _accountRepositoryMock.Setup(x => x.Update(_destinationAccount)).Verifiable();
        }
    }
}

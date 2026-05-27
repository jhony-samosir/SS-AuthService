using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SS.AuthService.Application.Auth.Commands;
using SS.AuthService.Application.Auth.DTOs;
using SS.AuthService.Application.Auth.Handlers;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using Xunit;

namespace SS.AuthService.Application.Tests.Auth.Handlers;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenHasher> _tokenHasherMock;
    private readonly Mock<IEmailQueue> _emailQueueMock;
    private readonly Mock<IOutboxRepository> _outboxRepositoryMock;
    private readonly Mock<ILogger<RegisterUserCommandHandler>> _loggerMock;
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _emailVerificationRepositoryMock = new Mock<IEmailVerificationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenHasherMock = new Mock<ITokenHasher>();
        _emailQueueMock = new Mock<IEmailQueue>();
        _outboxRepositoryMock = new Mock<IOutboxRepository>();
        _loggerMock = new Mock<ILogger<RegisterUserCommandHandler>>();

        _handler = new RegisterUserCommandHandler(
            _userRepositoryMock.Object,
            _emailVerificationRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _tokenHasherMock.Object,
            _emailQueueMock.Object,
            _outboxRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_QueueVerificationEmail_When_RegisteringNewUser()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Test User",
            "newuser@example.com",
            "Password123!",
            true,
            true);

        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(command.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock.Setup(x => x.GetDefaultCustomerRoleIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _passwordHasherMock.Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _tokenHasherMock.Setup(x => x.Generate())
            .Returns("verification_token");
        _tokenHasherMock.Setup(x => x.Hash("verification_token"))
            .Returns("hashed_token");

        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        _emailQueueMock.Verify(x => x.QueueEmailAsync(
                It.Is<EmailTask>(et => 
                    et.To == command.Email.ToLowerInvariant() && 
                    et.Token == "verification_token" && 
                    et.Type == EmailType.Verification)),
            Times.Once);
    }
}
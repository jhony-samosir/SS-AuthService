using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Common.Behaviors;
using Xunit;

namespace SS.AuthService.Application.Tests.Common.Behaviors;

public class LoggingBehaviorTests
{
    private readonly MockLogger<LoggingBehavior<TestRequest, TestResponse>> _logger;
    private readonly LoggingBehavior<TestRequest, TestResponse> _behavior;

    public LoggingBehaviorTests()
    {
        _logger = new MockLogger<LoggingBehavior<TestRequest, TestResponse>>();
        _behavior = new LoggingBehavior<TestRequest, TestResponse>(_logger);
    }

    [Fact]
    public async Task Handle_SuccessRequest_ShouldLogStartAndCompletion()
    {
        // Arrange
        var request = new TestRequest();
        var response = new TestResponse();
        var nextCalled = false;
        var nextDelegate = new RequestHandlerDelegate<TestResponse>((cancellationToken) =>
        {
            nextCalled = true;
            return Task.FromResult(response);
        });

        // Act
        var result = await _behavior.Handle(request, nextDelegate, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(response, result);
        Assert.Equal(2, _logger.Logs.Count);
        
        Assert.Equal(LogLevel.Information, _logger.Logs[0].Level);
        Assert.Contains("Handling MediatR request: TestRequest", _logger.Logs[0].Message);

        Assert.Equal(LogLevel.Information, _logger.Logs[1].Level);
        Assert.Contains("Handled MediatR request successfully: TestRequest in", _logger.Logs[1].Message);
    }

    [Fact]
    public async Task Handle_FailedRequest_ShouldLogFailureAndThrow()
    {
        // Arrange
        var request = new TestRequest();
        var exception = new Exception("Test exception message");
        var nextCalled = false;
        var nextDelegate = new RequestHandlerDelegate<TestResponse>((cancellationToken) =>
        {
            nextCalled = true;
            throw exception;
        });

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<Exception>(() => 
            _behavior.Handle(request, nextDelegate, CancellationToken.None));

        Assert.True(nextCalled);
        Assert.Same(exception, thrownException);
        Assert.Equal(2, _logger.Logs.Count);

        Assert.Equal(LogLevel.Information, _logger.Logs[0].Level);
        Assert.Contains("Handling MediatR request: TestRequest", _logger.Logs[0].Message);

        Assert.Equal(LogLevel.Error, _logger.Logs[1].Level);
        Assert.Contains("Failed handling MediatR request: TestRequest after", _logger.Logs[1].Message);
        Assert.Same(exception, _logger.Logs[1].Exception);
    }

    public record TestRequest : IRequest<TestResponse>;
    public record TestResponse;
}

public class MockLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Logs { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Logs.Add((logLevel, formatter(state, exception), exception));
    }
}

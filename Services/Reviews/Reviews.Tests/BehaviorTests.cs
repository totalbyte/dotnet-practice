using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Reviews.Application.Common.Behaviors;

namespace Reviews.Tests;

public sealed class BehaviorTests
{
    private sealed record Ping(int Value) : IRequest<string>;

    private sealed class PositiveValidator : AbstractValidator<Ping>
    {
        public PositiveValidator() => RuleFor(x => x.Value).GreaterThan(0);
    }

    private sealed class EvenValidator : AbstractValidator<Ping>
    {
        public EvenValidator() => RuleFor(x => x.Value).Must(v => v % 2 == 0).WithMessage("Value must be even");
    }

    private static ValidationBehavior<Ping, string> ValidationBehavior(params IValidator<Ping>[] validators)
        => new(validators, NullLogger<ValidationBehavior<Ping, string>>.Instance);

    [Fact]
    public async Task Invalid_request_never_reaches_handler_and_all_errors_are_collected()
    {
        var handlerCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("pong");
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            ValidationBehavior(new PositiveValidator(), new EvenValidator()).Handle(new Ping(-1), next, CancellationToken.None));

        Assert.False(handlerCalled);
        Assert.Equal(2, ex.Errors.Count());
    }

    [Fact]
    public async Task Valid_request_is_passed_to_handler()
    {
        var result = await ValidationBehavior(new PositiveValidator(), new EvenValidator())
            .Handle(new Ping(2), _ => Task.FromResult("pong"), CancellationToken.None);

        Assert.Equal("pong", result);
    }

    [Fact]
    public async Task Request_without_validators_is_passed_to_handler()
    {
        var result = await ValidationBehavior().Handle(new Ping(-5), _ => Task.FromResult("pong"), CancellationToken.None);

        Assert.Equal("pong", result);
    }

    [Fact]
    public async Task Exception_handling_behavior_rethrows_original_exception()
    {
        var behavior = new ExceptionHandlingBehavior<Ping, string>(NullLogger<ExceptionHandlingBehavior<Ping, string>>.Instance);
        var failure = new InvalidOperationException("boom");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(new Ping(1), _ => Task.FromException<string>(failure), CancellationToken.None));

        Assert.Same(failure, ex);
    }

    [Fact]
    public async Task Validation_exception_contains_failures_from_validator()
    {
        var failures = new[] { new ValidationFailure("Value", "bad") };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            new LoggingBehavior<Ping, string>(NullLogger<LoggingBehavior<Ping, string>>.Instance)
                .Handle(new Ping(1), _ => throw new ValidationException(failures), CancellationToken.None));

        Assert.Single(ex.Errors);
    }
}

using FluentAssertions;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

public class ResultTests
{
    #region Result.Success Tests

    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    #endregion

    #region Result.Failure Tests

    [Fact]
    public void Failure_WithMessage_ShouldCreateFailedResult()
    {
        var result = Result.Failure("Something went wrong");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Something went wrong");
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Failure_WithMessageAndDetail_ShouldCreateFailedResult()
    {
        var result = Result.Failure("Error occurred", "Additional details");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Error occurred");
        result.ErrorDetail.Should().Be("Additional details");
    }

    [Fact]
    public void Failure_WithException_ShouldCreateFailedResult()
    {
        var exception = new InvalidOperationException("Test exception");
        var result = Result.Failure(exception);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Exception.Should().Be(exception);
        result.ErrorDetail.Should().Be("Test exception");
    }

    [Fact]
    public void Failure_WithExceptionAndCustomMessage_ShouldUseCustomMessage()
    {
        var exception = new InvalidOperationException("Inner message");
        var result = Result.Failure(exception, "Custom message");

        result.Error.Should().Be("Custom message");
        result.ErrorDetail.Should().Be("Inner message");
        result.Exception.Should().Be(exception);
    }

    #endregion

    #region OnSuccess/OnFailure Tests

    [Fact]
    public void OnSuccess_WhenSuccess_ShouldExecuteAction()
    {
        var result = Result.Success();
        var executed = false;

        result.OnSuccess(() => executed = true);

        executed.Should().BeTrue();
    }

    [Fact]
    public void OnSuccess_WhenFailure_ShouldNotExecuteAction()
    {
        var result = Result.Failure("error");
        var executed = false;

        result.OnSuccess(() => executed = true);

        executed.Should().BeFalse();
    }

    [Fact]
    public void OnFailure_WhenFailure_ShouldExecuteAction()
    {
        var result = Result.Failure("error message", "detail");
        string? capturedError = null;
        string? capturedDetail = null;

        result.OnFailure((e, d) => { capturedError = e; capturedDetail = d; });

        capturedError.Should().Be("error message");
        capturedDetail.Should().Be("detail");
    }

    [Fact]
    public void OnFailure_WhenSuccess_ShouldNotExecuteAction()
    {
        var result = Result.Success();
        var executed = false;

        result.OnFailure((_, _) => executed = true);

        executed.Should().BeFalse();
    }

    #endregion
}

public class ResultOfTTests
{
    #region Success Tests

    [Fact]
    public void Success_ShouldCreateSuccessfulResultWithValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Success_WithReferenceType_ShouldWorkCorrectly()
    {
        var result = Result<string>.Success("hello");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    #endregion

    #region Failure Tests

    [Fact]
    public void Failure_WithMessage_ShouldCreateFailedResult()
    {
        var result = Result<int>.Failure("error");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("error");
    }

    [Fact]
    public void Failure_WithMessageAndDetail_ShouldCreateFailedResult()
    {
        var result = Result<int>.Failure("error", "detail");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("error");
        result.ErrorDetail.Should().Be("detail");
    }

    [Fact]
    public void Failure_WithException_ShouldCreateFailedResult()
    {
        var exception = new ArgumentException("bad arg");
        var result = Result<int>.Failure(exception);

        result.IsFailure.Should().BeTrue();
        result.ErrorDetail.Should().Be("bad arg");
        result.Exception.Should().Be(exception);
    }

    #endregion

    #region Value Access Tests

    [Fact]
    public void GetValueOrThrow_OnSuccess_ShouldReturnValue()
    {
        var result = Result<int>.Success(100);

        result.GetValueOrThrow().Should().Be(100);
    }

    [Fact]
    public void GetValueOrThrow_OnFailure_ShouldThrowException()
    {
        var result = Result<int>.Failure("error");

        var action = () => result.GetValueOrThrow();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetValueOrDefault_OnSuccess_ShouldReturnValue()
    {
        var result = Result<int>.Success(42);

        result.GetValueOrDefault(99).Should().Be(42);
    }

    [Fact]
    public void GetValueOrDefault_OnFailure_ShouldReturnDefault()
    {
        var result = Result<int>.Failure("error");

        result.GetValueOrDefault(99).Should().Be(99);
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitValueToResult_ShouldCreateSuccess()
    {
        Result<string> result = "test value";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test value");
    }

    #endregion

    #region Map Tests

    [Fact]
    public void Map_OnSuccess_ShouldTransformValue()
    {
        var result = Result<int>.Success(10);

        var mapped = result.Map(x => x * 2);

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(20);
    }

    [Fact]
    public void Map_OnFailure_ShouldPropagateError()
    {
        var result = Result<int>.Failure("original error", "detail");

        var mapped = result.Map(x => x * 2);

        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be("original error");
        mapped.ErrorDetail.Should().Be("detail");
    }

    [Fact]
    public void Map_ToNewType_ShouldWorkCorrectly()
    {
        var result = Result<int>.Success(42);

        var mapped = result.Map(x => x.ToString());

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    #endregion

    #region OnSuccess/OnFailure Tests

    [Fact]
    public void OnSuccess_WhenSuccess_ShouldExecuteAction()
    {
        var result = Result<int>.Success(5);
        int captured = 0;

        result.OnSuccess(v => captured = v);

        captured.Should().Be(5);
    }

    [Fact]
    public void OnSuccess_WhenFailure_ShouldNotExecuteAction()
    {
        var result = Result<int>.Failure("error");
        var executed = false;

        result.OnSuccess(_ => executed = true);

        executed.Should().BeFalse();
    }

    [Fact]
    public void OnFailure_WhenFailure_ShouldExecuteAction()
    {
        var result = Result<int>.Failure("error message", "detail");
        string? capturedError = null;
        string? capturedDetail = null;

        result.OnFailure((e, d) => { capturedError = e; capturedDetail = d; });

        capturedError.Should().Be("error message");
        capturedDetail.Should().Be("detail");
    }

    [Fact]
    public void OnFailure_WhenSuccess_ShouldNotExecuteAction()
    {
        var result = Result<int>.Success(5);
        var executed = false;

        result.OnFailure((_, _) => executed = true);

        executed.Should().BeFalse();
    }

    [Fact]
    public void OnSuccessOnFailure_ShouldBeChainable()
    {
        var result = Result<int>.Success(10);
        int successValue = 0;
        string? errorValue = null;

        var returned = result
            .OnSuccess(v => successValue = v)
            .OnFailure((e, _) => errorValue = e);

        returned.Should().BeSameAs(result);
        successValue.Should().Be(10);
        errorValue.Should().BeNull();
    }

    #endregion
}

public class ResultExtensionsTests
{
    #region Try Tests

    [Fact]
    public void Try_SuccessfulFunction_ShouldReturnSuccess()
    {
        var result = ResultExtensions.Try(() => 42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Try_ThrowingFunction_ShouldReturnFailure()
    {
        var result = ResultExtensions.Try<int>(() => throw new InvalidOperationException("boom"));

        result.IsFailure.Should().BeTrue();
        result.ErrorDetail.Should().Be("boom");
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void TryAction_SuccessfulAction_ShouldReturnSuccess()
    {
        int sideEffect = 0;
        var result = ResultExtensions.Try(() => sideEffect = 42);

        result.IsSuccess.Should().BeTrue();
        sideEffect.Should().Be(42);
    }

    [Fact]
    public void TryAction_ThrowingAction_ShouldReturnFailure()
    {
        var result = ResultExtensions.Try(() => throw new ArgumentException("bad"));

        result.IsFailure.Should().BeTrue();
        result.ErrorDetail.Should().Be("bad");
    }

    #endregion

    #region TryAsync Tests

    [Fact]
    public async Task TryAsync_SuccessfulFunction_ShouldReturnSuccess()
    {
        var result = await ResultExtensions.TryAsync(async () =>
        {
            await Task.Delay(1);
            return 100;
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(100);
    }

    [Fact]
    public async Task TryAsync_ThrowingFunction_ShouldReturnFailure()
    {
        var result = await ResultExtensions.TryAsync<int>(async () =>
        {
            await Task.Delay(1);
            throw new TimeoutException("timed out");
        });

        result.IsFailure.Should().BeTrue();
        result.ErrorDetail.Should().Be("timed out");
    }

    [Fact]
    public async Task TryAsyncAction_SuccessfulAction_ShouldReturnSuccess()
    {
        int value = 0;
        var result = await ResultExtensions.TryAsync(async () =>
        {
            await Task.Delay(1);
            value = 50;
        });

        result.IsSuccess.Should().BeTrue();
        value.Should().Be(50);
    }

    [Fact]
    public async Task TryAsyncAction_ThrowingAction_ShouldReturnFailure()
    {
        var result = await ResultExtensions.TryAsync(async () =>
        {
            await Task.Delay(1);
            throw new IOException("disk error");
        });

        result.IsFailure.Should().BeTrue();
        result.ErrorDetail.Should().Be("disk error");
    }

    #endregion
}

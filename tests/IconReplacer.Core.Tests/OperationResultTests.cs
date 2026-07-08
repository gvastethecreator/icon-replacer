using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class OperationResultTests
{
    [Fact]
    public void SuccessCarriesValue()
    {
        var result = OperationResult<string>.Success("ready");

        Assert.True(result.Succeeded);
        Assert.Equal("ready", result.Value);
        Assert.Equal(ErrorCode.None, result.Error.Code);
    }

    [Fact]
    public void FailureCarriesError()
    {
        var error = new IconReplacerError(ErrorCode.InvalidIcon, "Invalid icon.");

        var result = OperationResult<string>.Failure(error);

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
        Assert.Equal(error, result.Error);
    }
}


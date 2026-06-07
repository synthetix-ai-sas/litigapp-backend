using LitigApp.Application.Common.Abstractions;

namespace LitigApp.Application.UnitTests.Common.Abstractions;

public sealed class RamaResultTests
{
    // ── Ok factory ────────────────────────────────────────────────────────────

    [Fact]
    public void Ok_Sets_IsSuccess_True_And_Value()
    {
        var result = RamaResult<string>.Ok("hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void Ok_With_Null_Value_Is_Still_Success()
    {
        var result = RamaResult<string?>.Ok(null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Null(result.Failure);
    }

    // ── Fail(RamaJudicialFailure) factory ─────────────────────────────────────

    [Fact]
    public void Fail_With_Failure_Record_Sets_IsSuccess_False_And_Failure()
    {
        var failure = new RamaJudicialFailure(FailureKind.NotFound, "Process not found");

        var result = RamaResult<string>.Fail(failure);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotNull(result.Failure);
        Assert.Same(failure, result.Failure);
    }

    [Theory]
    [InlineData(FailureKind.NotFound, "not found")]
    [InlineData(FailureKind.WafBlocked, "403 Forbidden")]
    [InlineData(FailureKind.Transient, "timeout")]
    [InlineData(FailureKind.InvalidInput, "must be 23 digits")]
    public void Fail_Preserves_FailureKind_And_Message(FailureKind kind, string message)
    {
        var result = RamaResult<int>.Fail(kind, message);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Failure);
        Assert.Equal(kind, result.Failure.Kind);
        Assert.Equal(message, result.Failure.Message);
    }

    // ── Convenience Fail(kind, message) factory ────────────────────────────────

    [Fact]
    public void Fail_Convenience_Factory_Produces_Same_Outcome_As_Direct_Fail()
    {
        const string msg = "WAF blocked";
        var direct = RamaResult<bool>.Fail(new RamaJudicialFailure(FailureKind.WafBlocked, msg));
        var convenience = RamaResult<bool>.Fail(FailureKind.WafBlocked, msg);

        Assert.Equal(direct.IsSuccess, convenience.IsSuccess);
        Assert.Equal(direct.Failure!.Kind, convenience.Failure!.Kind);
        Assert.Equal(direct.Failure.Message, convenience.Failure.Message);
    }

    // ── WafBlocked must never be confused with Transient ─────────────────────

    [Fact]
    public void WafBlocked_And_Transient_Are_Distinct_FailureKinds()
    {
        var waf = RamaResult<object>.Fail(FailureKind.WafBlocked, "403");
        var transient = RamaResult<object>.Fail(FailureKind.Transient, "503");

        Assert.NotEqual(waf.Failure!.Kind, transient.Failure!.Kind);
    }

    // ── List overload ─────────────────────────────────────────────────────────

    [Fact]
    public void Ok_With_Empty_List_Is_Success_And_Value_Is_Not_Null()
    {
        var result = RamaResult<List<string>>.Ok([]);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }
}

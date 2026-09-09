using SENG302.Api.Services;
using Microsoft.Extensions.Time.Testing;

namespace SENG302.Api.Tests.Unit.Services;

public class OneTimeCodeServiceUnitTest
{
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly IOneTimeCodeService _service;


    public OneTimeCodeServiceUnitTest()
    {
        _fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        _service = new OneTimeCodeService(_fakeTimeProvider);
    }

    [Fact]
    public void GetServerTime_ValidCall_ExpectUTCTimeSeconds()
    {
        long curTime = _service.GetEpochTime();

        Assert.True(curTime == _fakeTimeProvider.GetUtcNow().ToUnixTimeSeconds());
        Assert.True(curTime > 0);
    }

    [Fact]
    public void CompareTimes_DifferenceBelowTimeoutValue_ExpectTrue()
    {
        Assert.True(_service.CompareTimes(1774126459, 1774126758));
        Assert.True(_service.CompareTimes(1774126459, 1774126460));
    }

    [Fact]
    public void CompareTimes_DifferenceBelowTimeoutValue_ExpectFalse()
    {
        Assert.False(_service.CompareTimes(1774126459, 1774126760));
        Assert.False(_service.CompareTimes(1774126459, 1774126759));
    }


    [Fact]
    public void CompareCodes_SameCodes_ExpectTrue()
    {
        Assert.True(_service.CompareCodes("987456", "987456"));
        Assert.True(_service.CompareCodes("000111", "000111"));
    }

    [Fact]
    public void CompareCodes_DifferentCodes_ExpectFalse()
    {
        Assert.False(_service.CompareCodes("987456", "987546"));
        Assert.False(_service.CompareCodes("000111", "000123"));
    }

    [Fact]
    public void CompareCodes_InvalidLengthCodes_ExpectFalse()
    {
        Assert.False(_service.CompareCodes("9874561", "9875461"));
        Assert.False(_service.CompareCodes("00011123", "000111"));
    }

    [Fact]
    public void GenerateCodes_ValidCall_ExpectValidLength()
    {
        string code = _service.GenerateOneTimeCode();
        Assert.True(code.Length == 6);
    }


}
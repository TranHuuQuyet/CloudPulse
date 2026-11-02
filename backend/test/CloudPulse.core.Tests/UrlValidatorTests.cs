using System.Net;
using CloudPulse.Core.Validation;
using Xunit;

namespace CloudPulse.Core.Tests;

public class UrlValidatorTests
{
    private const string ExpectedPrivateIpError = "Private or loopback IP addresses are not permitted.";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_NullOrEmpty_ReturnsInvalid(string? url)
    {
        var result = await UrlValidator.ValidateAsync(url);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("not-a-valid-url")]
    [InlineData("://missing-scheme.com")]
    [InlineData("http://")]
    public async Task ValidateAsync_InvalidUrlFormat_ReturnsInvalid(string url)
    {
        var result = await UrlValidator.ValidateAsync(url);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("ftp://example.com/file.txt")]
    [InlineData("file:///etc/passwd")]
    [InlineData("gopher://example.com")]
    public async Task ValidateAsync_UnsupportedScheme_ReturnsInvalid(string url)
    {
        var result = await UrlValidator.ValidateAsync(url);

        Assert.False(result.IsValid);
        Assert.Contains("HTTP and HTTPS", result.ErrorMessage);
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:8080")]
    [InlineData("https://localhost/api/test")]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://127.0.0.1:3000")]
    [InlineData("http://127.0.0.2:8080")]
    public async Task ValidateAsync_Loopback_ReturnsInvalidWithStandardMessage(string url)
    {
        var result = await UrlValidator.ValidateAsync(url);

        Assert.False(result.IsValid);
        Assert.Equal(ExpectedPrivateIpError, result.ErrorMessage);
    }

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://169.254.169.254")]
    [InlineData("http://169.254.1.1:8080")]
    public async Task ValidateAsync_AwsMetadataAndLinkLocal_ReturnsInvalid(string url)
    {
        var result = await UrlValidator.ValidateAsync(url);

        Assert.False(result.IsValid);
        Assert.Equal(ExpectedPrivateIpError, result.ErrorMessage);
    }

    [Theory]
    [InlineData("http://10.0.0.1")]
    [InlineData("http://10.10.10.10:8080")]
    [InlineData("http://10.255.255.254")]
    [InlineData("http://172.16.0.1")]
    [InlineData("http://172.20.10.5")]
    [InlineData("http://172.31.255.254")]
    [InlineData("http://192.168.1.1")]
    [InlineData("http://192.168.0.100:5000")]
    public async Task ValidateAsync_PrivateRfc1918Ips_ReturnsInvalid(string url)
    {
        var result = await UrlValidator.ValidateAsync(url);

        Assert.False(result.IsValid);
        Assert.Equal(ExpectedPrivateIpError, result.ErrorMessage);
    }

    [Theory]
    [InlineData("http://[::1]:8080")]
    [InlineData("http://[fe80::1]")]
    public async Task ValidateAsync_IPv6LoopbackAndLinkLocal_ReturnsInvalid(string url)
    {
        var result = await UrlValidator.ValidateAsync(url);

        Assert.False(result.IsValid);
        Assert.Equal(ExpectedPrivateIpError, result.ErrorMessage);
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("127.255.255.255", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.255.255.255", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("172.32.0.1", false)] // Public
    [InlineData("192.168.0.1", true)]
    [InlineData("192.168.255.255", true)]
    [InlineData("169.254.169.254", true)]
    [InlineData("100.64.0.1", true)] // Carrier-grade NAT
    [InlineData("0.0.0.0", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("8.8.8.8", false)] // Public Google DNS
    [InlineData("1.1.1.1", false)] // Public Cloudflare DNS
    public void IsRestrictedIp_EvaluatesCorrectly(string ipString, bool expectedRestricted)
    {
        var ip = IPAddress.Parse(ipString);
        var actual = UrlValidator.IsRestrictedIp(ip);

        Assert.Equal(expectedRestricted, actual);
    }
}


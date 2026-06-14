using System.ComponentModel.DataAnnotations;
using YTSkedy.Infrastructure.YouTube;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubeBroadcastOptionsTests
{
    [Theory]
    [InlineData("private")]
    [InlineData("public")]
    [InlineData("unlisted")]
    public void Validate_AllowedPrivacyStatus_Passes(string privacyStatus)
    {
        var options = new YouTubeBroadcastOptions { PrivacyStatus = privacyStatus };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("Private")]
    [InlineData("PUBLIC")]
    [InlineData("secret")]
    [InlineData("")]
    public void Validate_DisallowedPrivacyStatus_FailsForPrivacyStatus(string privacyStatus)
    {
        var options = new YouTubeBroadcastOptions { PrivacyStatus = privacyStatus };

        var results = Validate(options);

        Assert.Contains(
            results,
            result => result.MemberNames.Contains(nameof(YouTubeBroadcastOptions.PrivacyStatus)));
    }

    private static List<ValidationResult> Validate(YouTubeBroadcastOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        return results;
    }
}

using System.Security.Cryptography;
using Axit.Core.Updates;

namespace Axit.Tests;

/// <summary>Signed releases (docs/axit/SPEC.md AX-3.4; security audit S-27): what the updater accepts.</summary>
public sealed class ReleaseSigningTests
{
    [Fact]
    public void A_signature_of_the_key_checks_and_a_changed_file_does_not()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var point = key.ExportParameters(false).Q;
        var publicKey = Convert.ToBase64String([.. point.X!, .. point.Y!]);
        var hash = SHA256.HashData("the zip"u8);
        var signature = Convert.ToBase64String(key.SignHash(hash));

        Assert.True(ReleaseSigning.Verify(hash, signature, publicKey));
        Assert.False(ReleaseSigning.Verify(SHA256.HashData("another zip"u8), signature, publicKey));
        Assert.False(ReleaseSigning.Verify(hash, signature));
        Assert.False(ReleaseSigning.Verify(hash, "not base64 at all"));
    }

    [Fact]
    public void The_built_in_public_half_is_a_point_of_p256()
    {
        var point = Convert.FromBase64String(ReleaseSigning.PublicKey);
        Assert.Equal(64, point.Length);
        using var key = ECDsa.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = new ECPoint { X = point[..32], Y = point[32..] } });
        Assert.Equal(256, key.KeySize);
    }

    [Fact]
    public void Only_this_repositorys_assets_are_taken_with_their_signature_and_checksum()
    {
        const string json = """
            { "tag_name": "v3.7.0", "assets": [
              { "name": "Axit-3.7.0-win-x64.zip", "size": 5, "digest": "sha256:ab12", "browser_download_url": "https://evil.invalid/Axit-3.7.0-win-x64.zip" },
              { "name": "Axit-3.7.0-win-x64.zip", "size": 5, "digest": "sha256:ab12", "browser_download_url": "https://github.com/KyleKeane/Axit/releases/download/v3.7.0/Axit-3.7.0-win-x64.zip" },
              { "name": "Axit-3.7.0-win-x64.zip.sig", "browser_download_url": "https://github.com/KyleKeane/Axit/releases/download/v3.7.0/Axit-3.7.0-win-x64.zip.sig" } ] }
            """;
        var release = UpdateCheck.Parse(json)!;
        Assert.Equal("https://github.com/KyleKeane/Axit/releases/download/v3.7.0/Axit-3.7.0-win-x64.zip", release.ZipUrl);
        Assert.Equal("https://github.com/KyleKeane/Axit/releases/download/v3.7.0/Axit-3.7.0-win-x64.zip.sig", release.SignatureUrl);
        Assert.Equal("sha256:ab12", release.ZipDigest);
    }

    [Fact]
    public void An_unsigned_release_has_no_signature_to_install_by()
    {
        var release = UpdateCheck.Parse("""{ "tag_name": "v3.7.0", "assets": [ { "name": "Axit-3.7.0-win-x64.zip", "browser_download_url": "https://github.com/KyleKeane/Axit/releases/download/v3.7.0/Axit-3.7.0-win-x64.zip" } ] }""")!;
        Assert.Null(release.SignatureUrl);
    }

    [Fact]
    public void The_checksum_github_recorded_must_match_when_it_is_given()
    {
        var hash = SHA256.HashData("zip"u8);
        Assert.True(Updater.MatchesDigest(hash, "sha256:" + Convert.ToHexString(hash).ToLowerInvariant()));
        Assert.False(Updater.MatchesDigest(hash, "sha256:00"));
        Assert.True(Updater.MatchesDigest(hash, null));
    }
}

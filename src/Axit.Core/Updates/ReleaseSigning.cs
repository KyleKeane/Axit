using System.Security.Cryptography;

namespace Axit.Core.Updates;

/// <summary>
/// The release signature (docs/axit/security-audit-2026-10-24.md S-27; docs/axit/SPEC.md AX-3.4, signed releases): every release's zip
/// is signed on the maintainer's machine with a key that never reaches GitHub (<c>tools/release-key.ps1</c>), and the
/// updater installs a zip only when its signature, the asset "&lt;zip&gt;.sig", checks against the public half below.
/// A stolen GitHub account or token can publish a release, but not sign one. ECDSA P-256 over the zip's SHA-256, the
/// signature as r and s in base64.
/// </summary>
public static class ReleaseSigning
{
    /// <summary>The public half of the release key, made 2026-10-24: the point's X and Y in base64.</summary>
    public const string PublicKey = "vAMJ2gn+w4fa1hWGA69TxbBQ+Ob4YYnTMKAwzQAhlXlffPSVVyb5pll/0Z93QQDGHVIq9gBOA2mLlOJ8Sx/k6w==";

    /// <summary>The signature file's suffix, after the zip's own name.</summary>
    public const string SignatureSuffix = ".sig";

    /// <summary>Whether <paramref name="signature"/> (the .sig file's text) signs the file whose SHA-256 is <paramref name="sha256"/>.</summary>
    public static bool Verify(byte[] sha256, string signature) => Verify(sha256, signature, PublicKey);

    /// <summary>The same against another public half, for the tests.</summary>
    public static bool Verify(byte[] sha256, string signature, string publicKey)
    {
        try
        {
            var point = Convert.FromBase64String(publicKey);
            var bytes = Convert.FromBase64String(signature.Trim());
            if (point.Length != 64 || bytes.Length != 64 || sha256.Length != 32)
            {
                return false;
            }

            using var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = point[..32], Y = point[32..] },
            });
            return ecdsa.VerifyHash(sha256, bytes);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return false;
        }
    }
}

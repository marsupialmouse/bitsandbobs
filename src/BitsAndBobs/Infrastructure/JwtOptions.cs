using System.ComponentModel.DataAnnotations;

namespace BitsAndBobs.Infrastructure;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets the signing key used for JWT tokens
    /// </summary>
    [Required]
    public string Key { get; set; } = "";

    /// <summary>
    /// Gets the valid JWT token issuer
    /// </summary>
    [Required]
    public string Issuer { get; set; } = "";

    /// <summary>
    /// Gets the valid JWT token audience
    /// </summary>
    [Required]
    public string Audience { get; set; } = "";
}

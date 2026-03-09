using System.Globalization;

namespace Umea.se.Toolkit.UserFromToken;

internal static class UserTokenExtensions
{
    internal static string? ToTitleCase(this string? name)
    {
        return name == null
            ? null
            : new CultureInfo("sv-SE", false).TextInfo.ToTitleCase(name.ToLower());
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Umea.se.Toolkit.CommonModels;

[Flags]
public enum SsnType
{
    None = 0,
    Person = 1,
    Coordination = 2,
    Organization = 4,
    ReserveNumber = 8,
    People = Person | Coordination,
    All = People | Organization | ReserveNumber
}

public enum Sex
{
    Female,
    Male
}

[Flags]
public enum ValidationError
{
    None = 0,
    NullOrWhitespace = 1,
    PatternMismatch = 2,
    TypeNotAllowed = 4,
    InvalidDate = 8,
    InvalidChecksum = 16,
}

public sealed partial class Ssn : IEquatable<Ssn>
{
    private readonly string _normalizedSsn;

    public string Normalized => _normalizedSsn;
    public string OriginalFormat { get; }
    public SsnType Type { get; }
    public DateTime? DateOfBirth { get; }

    public Ssn(string ssn, SsnType allowedTypes = SsnType.People)
    {
        if (!TryParse(ssn, out Ssn? parsed, out ValidationError error, allowedTypes) || parsed is null)
        {
            throw new FormatException($"Invalid SSN. Reason: {error}");
        }

        OriginalFormat = parsed.OriginalFormat;
        _normalizedSsn = parsed._normalizedSsn;
        Type = parsed.Type;
        DateOfBirth = parsed.DateOfBirth;
    }

    private Ssn(string originalFormat, string normalizedSsn, SsnType type, DateTime? dateOfBirth)
    {
        OriginalFormat = originalFormat;
        _normalizedSsn = normalizedSsn;
        Type = type;
        DateOfBirth = dateOfBirth;
    }

    public override string ToString() => Format();

    public string Format(bool withSeparator = false, bool longFormat = true, DateTime? asOf = null)
    {
        string numberPart = _normalizedSsn;
        if (!longFormat && (Type == SsnType.Person || Type == SsnType.Coordination || Type == SsnType.ReserveNumber))
        {
            numberPart = _normalizedSsn[2..];
        }

        if (!withSeparator)
        {
            return numberPart;
        }

        string separator = "-";
        if (Type == SsnType.Person && !longFormat && DateOfBirth is DateTime dob && IsHundredOrOver(dob, asOf ?? DateTime.Today))
        {
            separator = "+";
        }

        int separatorIndex = numberPart.Length - 4;
        return $"{numberPart[..separatorIndex]}{separator}{numberPart[separatorIndex..]}";
    }

    public int? GetAge(DateTime? asOf = null)
    {
        if (DateOfBirth is not DateTime dob)
        {
            return null;
        }

        DateTime referenceDate = (asOf ?? DateTime.Now).Date;
        DateTime birthDate = dob.Date;

        if (referenceDate < birthDate)
        {
            return 0;
        }

        int age = referenceDate.Year - birthDate.Year;
        if (referenceDate < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }

    public Sex? GetSex()
    {
        if ((Type & SsnType.People) == 0 || _normalizedSsn.Length < 4)
        {
            return null;
        }

        char thirdControlDigit = _normalizedSsn[^2];
        if (!IsAsciiDigit(thirdControlDigit))
        {
            return null;
        }

        return (thirdControlDigit - '0') % 2 == 0 ? Sex.Female : Sex.Male;
    }

    public static Ssn Parse(string ssn, SsnType allowedTypes = SsnType.People)
    {
        if (TryParse(ssn, out Ssn? result, out ValidationError error, allowedTypes))
        {
            return result;
        }
        throw new FormatException($"Invalid SSN. Reason: {error}");
    }

    public static bool TryParse(string ssn, out Ssn? result, SsnType allowedTypes = SsnType.People)
    {
        return TryParse(ssn, out result, out _, allowedTypes);
    }

    public static bool TryParse(string ssn, [NotNullWhen(true)] out Ssn? result, out ValidationError error, SsnType allowedTypes = SsnType.People)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(ssn))
        {
            error = ValidationError.NullOrWhitespace;
            return false;
        }

        if (!TryMatchSsnParts(ssn, out SsnParts parts, out error))
        {
            return false;
        }

        if (!TryDetermineSsnType(ref parts, out SsnType ssnType, out error))
        {
            return false;
        }

        if (!TryValidateSsn(parts, ssnType, out DateTime? dateOfBirth, out error))
        {
            return false;
        }

        if (!allowedTypes.HasFlag(ssnType))
        {
            error = ValidationError.TypeNotAllowed;
            return false;
        }

        string normalizedSsn = ssnType == SsnType.Organization
            ? $"{parts.DatePart}{parts.LastThree}{parts.ControlChar}"
            : $"{parts.Century}{parts.DatePart}{parts.LastThree}{parts.ControlChar}";

        result = new Ssn(ssn, normalizedSsn, ssnType, dateOfBirth);
        return true;
    }

    private static bool TryMatchSsnParts(string ssn, out SsnParts parts, out ValidationError error)
    {
        error = ValidationError.None;
        parts = default;

        string sanitizedSsn = ssn.Trim();
        Match match = SsnRegex().Match(sanitizedSsn);
        if (!match.Success)
        {
            error = ValidationError.PatternMismatch;
            return false;
        }

        parts = new SsnParts(
            match.Groups["century"].Value,
            match.Groups["datePart"].Value,
            match.Groups["separator"].Value,
            match.Groups["lastThree"].Value,
            char.ToUpperInvariant(match.Groups["controlChar"].Value[0])
        );

        return true;
    }

    private static bool TryDetermineSsnType(ref SsnParts parts, out SsnType ssnType, out ValidationError error)
    {
        error = ValidationError.None;
        ssnType = SsnType.None;

        bool hasLettersInLastThree = parts.LastThree.Any(char.IsLetter);
        bool isOrganizationCandidate = int.Parse(parts.DatePart.AsSpan(2, 2), CultureInfo.InvariantCulture) >= 20;

        if (isOrganizationCandidate)
        {
            if (parts.DatePart[0] == '0' || !IsAsciiDigit(parts.ControlChar) || hasLettersInLastThree || parts.Separator == "+")
            {
                error = ValidationError.PatternMismatch;
                return false;
            }
            ssnType = SsnType.Organization;
        }
        else
        {
            ssnType = int.Parse(parts.DatePart.AsSpan(4, 2), CultureInfo.InvariantCulture) > 60
                ? SsnType.Coordination
                : SsnType.Person;
        }

        if (string.IsNullOrEmpty(parts.Century))
        {
            parts.Century = CalculateCentury(parts, isOrganizationCandidate);
        }

        if (hasLettersInLastThree || char.IsLetter(parts.ControlChar))
        {
            ssnType = SsnType.ReserveNumber;
        }
        else if (ssnType != SsnType.Organization && parts.LastThree == "000" && parts.ControlChar == '0')
        {
            if (!IsValidLuhn(parts.DatePart + parts.LastThree + parts.ControlChar))
            {
                ssnType = SsnType.ReserveNumber;
            }
        }

        return true;
    }

    private static string CalculateCentury(SsnParts parts, bool isOrganizationCandidate)
    {
        if (isOrganizationCandidate)
        {
            return "16";
        }

        int year = int.Parse(parts.DatePart.AsSpan(0, 2), CultureInfo.InvariantCulture);
        int month = int.Parse(parts.DatePart.AsSpan(2, 2), CultureInfo.InvariantCulture);
        int day = int.Parse(parts.DatePart.AsSpan(4, 2), CultureInfo.InvariantCulture);

        DateTime today = DateTime.Today;
        int birthYearFull = (year > today.Year % 100 ? 1900 : 2000) + year;
        DateTime tempDob = new(birthYearFull, Math.Max(1, Math.Min(month, 12)), Math.Max(1, Math.Min(day, 28)));

        if (parts.Separator == "+" || IsHundredOrOver(tempDob, today))
        {
            return ((birthYearFull - 100) / 100).ToString("D2", CultureInfo.InvariantCulture);
        }
        else
        {
            return (birthYearFull / 100).ToString("D2", CultureInfo.InvariantCulture);
        }
    }

    private static bool TryValidateSsn(SsnParts parts, SsnType ssnType, out DateTime? dateOfBirth, out ValidationError error)
    {
        error = ValidationError.None;

        bool dateValid = TryValidateDate(parts, ssnType, out dateOfBirth, out ValidationError dateError);

        if (ssnType != SsnType.ReserveNumber)
        {
            if (!dateValid)
            {
                error = dateError;
                return false;
            }

            if (!IsValidLuhn(parts.DatePart + parts.LastThree + parts.ControlChar))
            {
                error = ValidationError.InvalidChecksum;
                return false;
            }
        }
        else
        {
            if (!dateValid)
            {
                dateOfBirth = null;
            }
        }

        return true;
    }

    private static bool TryValidateDate(SsnParts parts, SsnType ssnType, out DateTime? parsedDate, out ValidationError error)
    {
        error = ValidationError.None;
        parsedDate = null;

        if (ssnType == SsnType.Organization)
        {
            return true;
        }

        int day = int.Parse(parts.DatePart.AsSpan(4, 2), CultureInfo.InvariantCulture);

        bool isCoordinationForDate = day > 60;
        int dayForDate = isCoordinationForDate ? day - 60 : day;

        string dateString = $"{parts.Century}{parts.DatePart[..4]}{dayForDate:D2}";
        if (DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tempDate))
        {
            parsedDate = tempDate;
            return true;
        }

        error = ValidationError.InvalidDate;
        return false;
    }

    public static bool IsValid(string ssn, SsnType allowed = SsnType.People) => TryParse(ssn, out _, out _, allowed);

    public static (List<Ssn> Valid, List<(string Input, ValidationError Error)> Invalid) ParseMany(
        IEnumerable<string> ssnInputs,
        SsnType allowedTypes = SsnType.People)
    {
        List<Ssn> validSsns = [];
        List<(string, ValidationError)> invalidEntries = [];

        if (ssnInputs == null)
        {
            return (validSsns, invalidEntries);
        }

        foreach (string input in ssnInputs)
        {
            if (TryParse(input, out Ssn? ssn, out ValidationError error, allowedTypes))
            {
                validSsns.Add(ssn);
            }
            else
            {
                invalidEntries.Add((input, error));
            }
        }

        return (validSsns, invalidEntries);
    }

    private static bool IsValidLuhn(string tenDigits)
    {
        if (tenDigits.Length != 10 || tenDigits.Any(c => !IsAsciiDigit(c)))
        {
            return false;
        }

        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            int n = tenDigits[i] - '0';
            if (i % 2 == 0)
            {
                n *= 2;
                if (n > 9)
                {
                    n -= 9;
                }
            }
            sum += n;
        }
        return sum % 10 == 0;
    }

    private static bool IsHundredOrOver(DateTime birthDate, DateTime asOfDate)
    {
        return birthDate.AddYears(100) <= asOfDate;
    }

    private static bool IsAsciiDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private record struct SsnParts(
        string Century,
        string DatePart,
        string Separator,
        string LastThree,
        char ControlChar);

    #region Equality Members
    public bool Equals(Ssn? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _normalizedSsn == other._normalizedSsn;
    }

    public override bool Equals(object? obj) => Equals(obj as Ssn);
    public override int GetHashCode() => _normalizedSsn.GetHashCode();
    public static bool operator ==(Ssn left, Ssn right) => Equals(left, right);
    public static bool operator !=(Ssn left, Ssn right) => !Equals(left, right);

    [GeneratedRegex(@"^(?:(?<century>[0-9]{2})?)(?<datePart>[0-9]{6})(?<separator>[-+])?(?<lastThree>[0-9A-Za-z]{3})(?<controlChar>[0-9A-Za-z])$")]
    private static partial Regex SsnRegex();
    #endregion
}

using System.Reflection;
using Umea.se.Toolkit.CommonModels;

namespace Umea.se.Toolkit.Test;

public class SsnTests
{
    // Some test-values provided by Skatteverket:
    // Personnummer:      195001182046, 189912089803, 202301012395, 200007202385
    // Samordningsnummer: 202001612395
    // Organizationsnummer: 165560269986
    //

    #region Constructor and Core Parsing Tests

    [Fact]
    public void Constructor_WithInvalidSsn_ThrowsFormatException()
    {
        FormatException ex = Assert.Throws<FormatException>(() => new Ssn("invalid-ssn"));
        Assert.Contains("Invalid SSN. Reason: PatternMismatch", ex.Message);
    }

    [Theory]
    [InlineData("202001612395", SsnType.Person | SsnType.Organization)] // Coordination number, but only Person/Organization allowed
    [InlineData("16556026-9986", SsnType.Person)] // Organization number, but only Person allowed
    [InlineData("20040615-123A", SsnType.Person | SsnType.Coordination)] // Reserve number, but not allowed
    [InlineData("195001182046", SsnType.Coordination)] // Person number, but only Coordination allowed
    public void TryParse_Fails_WhenTypeIsNotAllowed(string ssn, SsnType allowedTypes)
    {
        bool ok = Ssn.TryParse(ssn, out _, out ValidationError err, allowedTypes);

        Assert.False(ok);
        Assert.Equal(ValidationError.TypeNotAllowed, err);
    }

    [Fact]
    public void TryParse_WithDefaultAllowedTypes_RejectsReserveNumber()
    {
        bool ok = Ssn.TryParse("20040615-123A", out _, out ValidationError err); // reserve number
        Assert.False(ok);
        Assert.Equal(ValidationError.TypeNotAllowed, err);
    }

    [Fact]
    public void TryParse_OrganizationLike_WithLetterChecksum_IsPatternMismatch()
    {
        bool ok = Ssn.TryParse("20002001-000A", out _, out ValidationError err);
        Assert.False(ok);
        Assert.Equal(ValidationError.PatternMismatch, err);
    }

    [Fact]
    public void TryParse_OrganizationNumber_WithPlusSeparator_IsPatternMismatch()
    {
        bool ok = Ssn.TryParse("556026+9986", out _, out ValidationError err, SsnType.All);
        Assert.False(ok);
        Assert.Equal(ValidationError.PatternMismatch, err);
    }

    [Fact]
    public void TryParse_OrganizationNumber_StartingWithZero_IsPatternMismatch()
    {
        // A number like 042010-1234 is not a valid format.
        bool ok = Ssn.TryParse("042010-1234", out _, out ValidationError err, SsnType.All);
        Assert.False(ok);
        Assert.Equal(ValidationError.PatternMismatch, err);
    }

    [Fact]
    public void TryParse_LowercaseControlLetter_Works()
    {
        bool ok = Ssn.TryParse("20040615-123a", out Ssn? ssn, out ValidationError err, allowedTypes: SsnType.All);
        Assert.True(ok, $"Expected success but got {err}");
        Assert.Equal(SsnType.ReserveNumber, ssn!.Type);
    }

    [Theory]
    [InlineData("2004061٥-1234")] // Arabic-Indic digit ٥ (U+06F5)
    [InlineData("20040615-123٤")] // Arabic-Indic digit ٤ (U+06F4) in control position
    [InlineData("200406१5-1234")] // Devanagari digit १ (U+0967)
    [InlineData("20040615-12३4")] // Devanagari digit ३ (U+0969) in lastThree
    [InlineData("20040615-１234")] // Fullwidth digit １ (U+FF11)
    public void TryParse_UnicodeDigits_AreRejected(string input)
    {
        bool ok = Ssn.TryParse(input, out _, out ValidationError err, allowedTypes: SsnType.All);
        Assert.False(ok);
        Assert.Equal(ValidationError.PatternMismatch, err);
    }

    [Theory]
    [InlineData("20040615-A234")]   // Letter in first control position
    [InlineData("20040615-1B34")]   // Letter in second control position  
    [InlineData("20040615-12C4")]   // Letter in third control position
    [InlineData("20040615-123D")]   // Letter in fourth control position
    [InlineData("20040615-AB34")]   // Letters in first two positions
    [InlineData("20040615-A2CD")]   // Letters in first and last two positions
    [InlineData("20040615-ABCD")]   // Letters in all control positions
    public void TryParse_ReserveNumbers_WithLettersInAnyPosition_Works(string input)
    {
        bool ok = Ssn.TryParse(input, out Ssn? ssn, out ValidationError err, allowedTypes: SsnType.All);
        Assert.True(ok, $"Expected success for {input} but got {err}");
        Assert.Equal(SsnType.ReserveNumber, ssn!.Type);
    }

    #endregion

    #region Date Validation Tests

    [Theory]
    [InlineData("20230229-1234")] // Person number, invalid date
    [InlineData("230289-1234")]   // Coordination number (29+60), invalid date
    public void TryParse_February29_OnNonLeapYear_IsInvalidDate(string input)
    {
        bool ok = Ssn.TryParse(input, out _, out ValidationError err, SsnType.All);
        Assert.False(ok);
        Assert.Equal(ValidationError.InvalidDate, err);
    }

    [Fact]
    public void TryParse_CoordinationOnLeapDay_WithLetterChecksum_Works()
    {
        const string input = "20000289-123A"; // 2000-02-29 as coordination (29 + 60 = 89)
        bool ok = Ssn.TryParse(input, out Ssn? ssn, out ValidationError err, allowedTypes: SsnType.All);

        Assert.True(ok, $"Expected success but got {err}");
        Assert.NotNull(ssn);
        Assert.Equal(SsnType.ReserveNumber, ssn!.Type);
        Assert.Equal(new DateTime(2000, 2, 29), ssn.DateOfBirth);
    }

    [Fact]
    public void TryParse_ShortFormCoordination_Works()
    {
        bool ok = Ssn.TryParse("200161-2395", out Ssn? ssn, out ValidationError err); // 2000-01-(01+60)=61
        Assert.True(ok, $"Expected success but got {err}");
        Assert.Equal(SsnType.Coordination, ssn!.Type);
    }

    #endregion

    #region Short Format Parsing Tests

    [Fact]
    public void Parse_ShortDash_UnderHundred_StaysIn1900s()
    {
        Ssn ssn = Ssn.Parse("500118-2046");
        Assert.Equal(new DateTime(1950, 1, 18), ssn.DateOfBirth);
        Assert.Equal("19500118-2046", ssn.Format(withSeparator: true, asOf: DateTime.Today));
    }

    [Fact]
    public void Parse_ShortDash_For2000sBirthYear_ResolvesToCorrectCentury()
    {
        Ssn ssn = Ssn.Parse("050101-1233");
        Assert.Equal(new DateTime(2005, 1, 1), ssn.DateOfBirth);
    }

    [Fact]
    public void Parse_ShortPlus_MeansCenturyMinus100()
    {
        bool ok = Ssn.TryParse("500118+2046", out Ssn? ssn, out ValidationError err);
        Assert.True(ok, $"Expected success but got {err}");
        Assert.Equal(new DateTime(1850, 1, 18), ssn!.DateOfBirth);
        Assert.Equal("18500118-2046", ssn.Format(withSeparator: true, asOf: DateTime.Today));
    }

    #endregion

    #region Formatting Tests

    [Fact]
    public void Format_LongAndShortForms_Work()
    {
        Ssn ssn = Ssn.Parse("195001182046");

        Assert.Equal("195001182046", ssn.Format()); // default long without separator
        Assert.Equal("195001182046", ssn.Format(withSeparator: false));
        Assert.Equal("500118-2046", ssn.Format(withSeparator: true, longFormat: false));
        Assert.Equal("5001182046", ssn.Format(longFormat: false, withSeparator: false));
    }

    [Fact]
    public void Format_SeparatorChangesBasedOnAsOfDate()
    {
        Ssn ssn = Ssn.Parse("195001182046");

        // Before 100th birthday, separator is '-'
        string formattedBefore = ssn.Format(withSeparator: true, longFormat: false, asOf: new DateTime(2050, 1, 17));
        Assert.Equal("500118-2046", formattedBefore);

        // On or after 100th birthday, separator becomes '+'
        string formattedOn = ssn.Format(withSeparator: true, longFormat: false, asOf: new DateTime(2050, 1, 18));
        Assert.Equal("500118+2046", formattedOn);
    }

    [Fact]
    public void Format_UsesPlusForHundredOrOver()
    {
        Ssn ssn = Ssn.Parse("189912089803"); // over 100 years ago as of today
        string formatted = ssn.Format(withSeparator: true, longFormat: true, asOf: DateTime.Today);

        Assert.Contains('-', formatted); // should use - separator for long format
        // Short format should render '+'
        Assert.Contains('+', ssn.Format(withSeparator: true, longFormat: false, asOf: DateTime.Today));
    }

    [Fact]
    public void Format_DashForUnderHundred()
    {
        Ssn ssn = Ssn.Parse("200007202385"); // not 100+ as of today
        string formatted = ssn.Format(withSeparator: true, asOf: new DateTime(2050, 1, 1)); // still < 100 years old then

        Assert.Contains('-', formatted);
    }

    [Fact]
    public void Format_AlwaysUsesDash_ForNonPersonTypes()
    {
        // A coordination number from 1920. As of today, this is > 100 years.
        Ssn ssn = Ssn.Parse("192001612395");

        string formatted = ssn.Format(withSeparator: true, asOf: DateTime.Today);

        Assert.Equal(SsnType.Coordination, ssn.Type);
        Assert.Contains('-', formatted);
    }

    [Theory]
    [InlineData("165560269986", "556026-9986")] // Organization
    [InlineData("20040615-123A", "040615-123A")] // Reserve
    public void Format_ShortFormat_ForOrganizationAndReserve_UsesDash(string input, string expectedFormat)
    {
        Ssn ssn = Ssn.Parse(input, SsnType.All);
        // Use a future date to ensure the 100+ year rule isn't a factor
        string formatted = ssn.Format(withSeparator: true, longFormat: false, asOf: new DateTime(2200, 1, 1));
        Assert.Equal(expectedFormat, formatted);
    }

    [Fact]
    public void Format_NonPerson_ShortForm_UsesDash()
    {
        Ssn ssn = Ssn.Parse("202001612395");
        Assert.DoesNotContain('+', ssn.Format(withSeparator: true, longFormat: false, asOf: DateTime.Today));
    }

    #endregion

    #region Age Tests

    [Fact]
    public void GetAge_ReturnsExpectedAge_OnReferenceDate()
    {
        Ssn ssn = Ssn.Parse("195001182046");

        Assert.Equal(98, ssn.GetAge(new DateTime(2049, 1, 17)));
        Assert.Equal(99, ssn.GetAge(new DateTime(2049, 1, 18)));
        Assert.Equal(100, ssn.GetAge(new DateTime(2050, 1, 18)));
    }

    [Fact]
    public void GetAge_ReturnsNull_WhenDateOfBirthIsMissing()
    {
        Ssn ssn = Ssn.Parse("165560269986", SsnType.All);

        Assert.Null(ssn.GetAge(new DateTime(2024, 1, 1)));
        Assert.Null(ssn.GetAge());
    }

    [Fact]
    public void GetAge_AsOfBeforeBirthDate_ReturnsZero()
    {
        Ssn ssn = Ssn.Parse("195001182046");

        Assert.Equal(0, ssn.GetAge(new DateTime(1949, 1, 17)));
    }

    #endregion

    #region Sex Tests

    [Fact]
    public void GetSex_ReturnsFemale_WhenThirdControlDigitIsEven()
    {
        Ssn ssn = Ssn.Parse("195001182046");

        Assert.Equal(Sex.Female, ssn.GetSex());
    }

    [Fact]
    public void GetSex_ReturnsMale_WhenThirdControlDigitIsOdd()
    {
        Ssn ssn = Ssn.Parse("202301012395");

        Assert.Equal(Sex.Male, ssn.GetSex());
    }

    [Fact]
    public void GetSex_ReturnsMale_ForCoordinationNumber()
    {
        Ssn ssn = Ssn.Parse("202001612395");

        Assert.Equal(Sex.Male, ssn.GetSex());
    }

    [Fact]
    public void GetSex_ReturnsNull_ForNonPeopleTypes()
    {
        Ssn organization = Ssn.Parse("165560269986", SsnType.All);
        Ssn reserve = Ssn.Parse("20040615-123A", SsnType.All);

        Assert.Null(organization.GetSex());
        Assert.Null(reserve.GetSex());
    }

    #endregion

    #region Equality and ToString Tests

    [Fact]
    public void Equality_WorksAcrossFormats()
    {
        Ssn longForm = Ssn.Parse("195001182046");
        Ssn shortForm = Ssn.Parse("500118-2046");

        Assert.True(longForm == shortForm);
        Assert.True(longForm.Equals(shortForm));
        Assert.Equal(longForm.GetHashCode(), shortForm.GetHashCode());
    }

    [Fact]
    public void Inequality_WorksAsExpected()
    {
        Ssn ssn1 = Ssn.Parse("195001182046");
        Ssn ssn2 = Ssn.Parse("200007202385");

        Assert.True(ssn1 != ssn2);
        Assert.False(ssn1 == ssn2);
    }

    [Fact]
    public void ToString_UsesDefaultFormat()
    {
        Ssn ssn = Ssn.Parse("195001182046");
        DateTime date = new DateTime(2024, 1, 1);

        Assert.Equal("195001182046", ssn.Format(asOf: date).ToString());
    }

    #endregion

    #region ParseMany Tests

    [Fact]
    public void ParseMany_ReturnsValidAndInvalid()
    {
        string[] inputs = new[]
        {
        // Valid cases
        "195001182046",  // valid personnummer
        "202001612395",  // valid samordningsnummer
        "165560269986",  // valid organization

        // Invalid cases
        "20230230-1234", // invalid date
        "abc",           // bad format
        "   ",           // whitespace
        ""               // empty
    };

        (List<Ssn>? valid, List<(string Input, ValidationError Error)>? invalid) = Ssn.ParseMany(inputs, SsnType.All);

        // Assert valid count
        Assert.Equal(3, valid.Count);
        Assert.Contains(valid, s => s.Type == SsnType.Person);
        Assert.Contains(valid, s => s.Type == SsnType.Coordination);
        Assert.Contains(valid, s => s.Type == SsnType.Organization);

        // Assert invalid count and specific errors
        Assert.Equal(4, invalid.Count);
        Assert.Contains(invalid, e => e.Input == "20230230-1234" && e.Error == ValidationError.InvalidDate);
        Assert.Contains(invalid, e => e.Input == "abc" && e.Error == ValidationError.PatternMismatch);
        Assert.Contains(invalid, e => e.Input == "   " && e.Error == ValidationError.NullOrWhitespace);
        Assert.Contains(invalid, e => e.Input == "" && e.Error == ValidationError.NullOrWhitespace);
    }

    [Fact]
    public void ParseMany_WithTypeRestriction_FiltersOutTypes()
    {
        string[] inputs = new[]
        {
            "195001182046", // Person
            "202001612395", // Coordination
            "165560269986"  // Organization
        };

        (List<Ssn>? valid, List<(string Input, ValidationError Error)>? invalid) = Ssn.ParseMany(inputs, allowedTypes: SsnType.Person);

        Assert.Single(valid);
        Assert.Equal(SsnType.Person, valid[0].Type);

        Assert.Equal(2, invalid.Count);
        Assert.All(invalid, e => Assert.Equal(ValidationError.TypeNotAllowed, e.Error));
    }

    [Fact]
    public void ParseMany_NullInput_ReturnsEmpty()
    {
        (List<Ssn>? valid, List<(string Input, ValidationError Error)>? invalid) = Ssn.ParseMany(null!);
        Assert.Empty(valid);
        Assert.Empty(invalid);
    }

    [Fact]
    public void ParseMany_EmptyInput_ReturnsEmpty()
    {
        (List<Ssn>? valid, List<(string Input, ValidationError Error)>? invalid) = Ssn.ParseMany([]);
        Assert.Empty(valid);
        Assert.Empty(invalid);
    }

    #endregion

    #region Data-Driven Tests

    /// <summary>
    /// Data provider that reads test cases from data/ssn_test_cases.csv.
    /// </summary>
    public static TheoryData<string, bool, ValidationError, string> SsnTestDataFromCsv()
    {
        TheoryData<string, bool, ValidationError, string> theoryData = new TheoryData<string, bool, ValidationError, string>();

        string? assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string csvPath = Path.Combine(assemblyPath!, "data", "ssn_test_cases.csv");

        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException("The CSV test case file was not found.", csvPath);
        }

        IEnumerable<string> lines = File.ReadAllLines(csvPath).Skip(1);

        foreach (string? line in lines)
        {
            string[] parts = line.Split(',');
            if (parts.Length != 4)
            {
                continue;
            }

            string ssn = parts[0].Trim('"');
            bool expectedIsValid = bool.Parse(parts[1]);
            ValidationError expectedError = Enum.Parse<ValidationError>(parts[2]);
            string description = parts[3];

            theoryData.Add(ssn, expectedIsValid, expectedError, description);
        }

        return theoryData;
    }

    /// <summary>
    /// This theory runs one test for each row in the data/ssn_test_cases.csv file.
    /// </summary>
    [Theory]
    [MemberData(nameof(SsnTestDataFromCsv))]
    public void Ssn_Validation_FromCsv(string ssn, bool expectedIsValid, ValidationError expectedError, string description)
    {
        bool actualResult = Ssn.TryParse(ssn, out _, out ValidationError actualError, SsnType.All);

        Assert.True(actualResult == expectedIsValid, $"Input: '{ssn}' | Expected validation result: {expectedIsValid}, but got: {actualResult}. | Reason: {description}");

        if (!expectedIsValid)
        {
            Assert.True(actualError == expectedError, $"Input: '{ssn}' | Expected error: {expectedError}, but got: {actualError}. | Reason: {description}");
        }
    }

    #endregion
}

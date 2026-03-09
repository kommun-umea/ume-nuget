#pragma warning disable CS0612 // Type or member is obsolete
using Shouldly;
using Umea.se.Toolkit.CommonModels;
using Umea.se.Toolkit.CommonModels.Exceptions;

namespace Umea.se.Toolkit.Test;

public class PersonnummerTest // covering largely InheritableString
{
    private const string C = "1234567890TT";

    [Fact]
    public void New_Assign()
    {

        Personnummer pn = new(C);

        string s = pn;
        s.ShouldBe(C);
    }

    [Fact]
    public void New_ToString()
    {
        Personnummer pn = new(C);

        pn.ToString().ShouldBe(C);
    }

    [Fact]
    public void New_Equality1()
    {
        Personnummer pn = new(C);

        (pn == C).ShouldBeTrue();
    }

    [Fact]
    public void New_Equality2()
    {
        Personnummer pn = new(C);

        (C == pn).ShouldBeTrue();
    }

    [Fact]
    public void Assign_EqualsString()
    {
        Personnummer pn = C;
        // ReSharper disable once SuspiciousTypeConversion.Global
        // This is a "suspicious cast". That is why we are testing it here.
        pn.Equals(C).ShouldBeTrue();
    }

    [Fact]
    public void Assign_Assign()
    {
        Personnummer pn = C;

        string s = pn;
        s.ShouldBe(C);
    }

    [Fact]
    public void Assign_Equality()
    {
        Personnummer pn1 = C;
        Personnummer pn2 = C;
        (pn1 == pn2).ShouldBeTrue();
    }

    [Fact]
    public void Assign_EqualsPn()
    {
        Personnummer pn1 = C;
        Personnummer pn2 = C;
        (pn1.Equals(pn2)).ShouldBeTrue();
    }

    [Fact]
    public void Validation()
    {
        Action badAssignmentCall = delegate { Personnummer _ = new("123"); };
        badAssignmentCall.ShouldThrow<ValidationException>();
    }

    [Fact]
    public void ToDateOfBirth()
    {
        Personnummer pn = new(C);
        (pn.ToDateOfBirth().Equals("1234-56-78")).ShouldBeTrue();
    }
}
#pragma warning restore CS0612 // Type or member is obsolete

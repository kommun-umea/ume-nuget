namespace Umea.se.Toolkit.CommonModels;

[Obsolete]
public class Personnummer : InheritableString
{
    public Personnummer(string value) : base(value)
    { }

    protected override bool IsValid(string value)
    {
        return value.Length is 12;
    }

    public string ToDateOfBirth()
    {
        string value = ToString();
        return $"{value[..4]}-{value[4..6]}-{value[6..8]}";
    }

    public static implicit operator Personnummer(string d)
    {
        return new Personnummer(d);
    }
}

using Umea.se.Toolkit.CommonModels.Exceptions;

namespace Umea.se.Toolkit.CommonModels;

public abstract class InheritableString
{
    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    private readonly string _value;

    protected InheritableString(string value)
    {
        // ReSharper disable once VirtualMemberCallInConstructor - it is tested and works.
        if (!IsValid(value))
        {
            throw new ValidationException($"Invalid creation of an {nameof(InheritableString)}.");
        }

        _value = value;
    }

    protected virtual bool IsValid(string value) => true;

    public override string ToString()
    {
        return _value;
    }

    public static implicit operator string(InheritableString d)
    {
        return d._value;
    }

    protected bool Equals(InheritableString other)
    {
        return _value == other._value;
    }

    protected bool Equals(string s)
    {
        return _value == s;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() == GetType())
        {
            return Equals((InheritableString)obj);
        }

        if (obj.GetType() == "".GetType())
        {
            return Equals((string)obj);
        }

        return false;
    }

    public static bool operator ==(InheritableString a, InheritableString b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(InheritableString a, InheritableString b)
    {
        return !a.Equals(b);
    }

    public static bool operator ==(InheritableString a, string b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(InheritableString a, string b)
    {
        return !a.Equals(b);
    }
}

/// <summary>
/// Exception to throw when the password formatting is invalid (doesn't meet requirements)
/// </summary>
public class InvalidPasswordException : Exception
{
    public InvalidPasswordException() { }

    public InvalidPasswordException(string message) : base(message) { }

    public InvalidPasswordException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception to throw when passwords are mismatched
/// </summary>
public class MismatchedPasswordException : Exception
{
    public MismatchedPasswordException() { }

    public MismatchedPasswordException(string message) : base(message) { }

    public MismatchedPasswordException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception to throw when the country code given from the front-end is invalid
/// </summary>
public class InvalidCountryException : Exception
{
    public InvalidCountryException() { }

    public InvalidCountryException(string message) : base(message) { }

    public InvalidCountryException(string message, Exception inner) : base(message, inner) { }
}


/// <summary>
/// Exception to throw when a user attempts to enable AI tagging without having accepted the AI privacy policy
/// </summary>
public class PrivacyPolicyNotAcceptedException : Exception
{
    public PrivacyPolicyNotAcceptedException() { }

    public PrivacyPolicyNotAcceptedException(string message) : base(message) { }

    public PrivacyPolicyNotAcceptedException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception to throw when general, and multiple validation error(s) occur.
/// </summary>
public class MultipleValidationException : Exception
{
    public Dictionary<string, string> Errors { get; }

    public MultipleValidationException(Dictionary<string, string> errors) : base("validation errors occurred")
    {
        Errors = errors;
    }
    
}

/// <summary>
/// Exception to throw when general, and multiple validation error(s) occur.
/// Can handle multiple exceptions in the same field
/// </summary>
public class MultipleValidationListException : Exception
{
    public Dictionary<string, List<string>> Errors { get; }

    public MultipleValidationListException(Dictionary<string, List<string>> errors) : base("validation errors occurred")
    {
        Errors = errors;
    }
}

/// <summary>
/// Exception to throw when a single field can have multiple validation errors
/// </summary>
public class ValidationListException : Exception
{
    public List<string> Errors { get; }
    public ValidationListException(List<string> errors) : base("validation errors occurred")
    {
        Errors = errors;
    }
}
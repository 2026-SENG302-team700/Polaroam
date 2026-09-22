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
/// Exception to throw when an image tag cannot be found
/// </summary>
public class TagNotFoundException : Exception
{
    public TagNotFoundException(string? error) : base(error ?? "Tag not found") {}
}

/// <summary>
/// Exception to throw when file size is exceeded.
/// </summary>
public class FileSizeExceededException : Exception
{
    public FileSizeExceededException(string? error) : base(error ?? "File size exceeded") {}
}

/// <summary>
/// Exception to throw when file mime is invalid
/// </summary>
public class FileMimeTypeInvalidException : Exception
{
    public FileMimeTypeInvalidException(string? error) : base(error ?? "File size exceeded") {}
}

/// <summary>
/// Exception to throw e-mail validation fails
/// </summary>
public class EmailInvalidationException : Exception
{
    public EmailInvalidationException(string? error):  base(error ?? "Email is invalid found") {}
}

/// <summary>
/// Exception to throw when a user is already part of a trip.
/// </summary>
public class UserInTripException : Exception
{
    public UserInTripException(string? error) : base(error ?? "User already exists in trip") {}
}

/// <summary>
/// Exception to throw when the user already has a pending invite
/// </summary>
public class PendingInviteException : Exception
{
    public PendingInviteException(string? error) : base(error ?? "This user already has a pending invite") {}
}
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Domain.Errors;

public static class UserErrors
{
    public static readonly Error NotFound = new("User.NotFound", "User was not found.");
    public static readonly Error EmailTaken = new("User.EmailTaken", "Email address is already in use.");
}

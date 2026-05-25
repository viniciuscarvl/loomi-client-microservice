using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Domain.Errors;

public static class ClientErrors
{
    public static readonly Error NotFound = new("Client.NotFound", "Client was not found.");
    public static readonly Error EmailTaken = new("Client.EmailTaken", "Email address is already in use.");
}

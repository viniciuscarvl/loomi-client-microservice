namespace ClientMicroservice.Application.Common;

public sealed record FileData(Stream Content, string FileName, string ContentType);

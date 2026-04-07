namespace Muzu.Api.Core.Rules;

public sealed class DirectivaAccessBlockedException : Exception
{
    public DirectivaAccessBlockedException(string message) : base(message)
    {
    }
}

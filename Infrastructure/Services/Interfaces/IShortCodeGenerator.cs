namespace Infrastructure.Services.Interfaces
{
    public interface IShortCodeGenerator
    {
        /// <summary>Generates one candidate 8-character code. Caller is responsible for uniqueness.</summary>
        string Generate();
    }
}

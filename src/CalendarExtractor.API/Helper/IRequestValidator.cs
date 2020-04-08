namespace CalendarExtractor.API.Helper
{
    public interface IRequestValidator
    {
        bool Validate(AzureRequest request);
    }
}
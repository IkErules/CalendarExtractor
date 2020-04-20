namespace CalendarExtractor.API.Helper
{
    public interface IRequestValidator
    {
        bool Validate(calendar_information_request request);
    }
}
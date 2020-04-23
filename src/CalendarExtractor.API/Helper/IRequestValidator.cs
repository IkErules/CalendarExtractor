namespace CalendarExtractor.API.Helper
{
    public interface IRequestValidator
    {
        bool Validate(CalendarInformationRequest request);
    }
}
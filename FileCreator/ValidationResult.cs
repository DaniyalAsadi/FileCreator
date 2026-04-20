namespace FileCreator;

// کلاسی برای نگهداری نتایج اعتبارسنجی
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
    public string ErrorMessage { get; set; }
    public object Header { get; set; } // JwtHeader
    public Dictionary<string, string> Payload { get; set; }
    public string FormattedPayload { get; set; } // Payload به صورت JSON فرمت شده
}

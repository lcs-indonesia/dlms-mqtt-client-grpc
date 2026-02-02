namespace DlmsMqttExecutor.Application.Exceptions;

public class StatusCodeException : Exception
{
    public int StatusCode { get; set; }
    public StatusCodeException(int statusCode, string message, Exception? ex = null) : base(message, ex)
    {
        StatusCode = statusCode;
    }
}

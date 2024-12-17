namespace smtp.producer.Models;

public record SendReponse(string requestId, string message, int code, string timestamp);
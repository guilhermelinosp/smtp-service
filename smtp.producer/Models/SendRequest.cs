using Newtonsoft.Json;

namespace smtp.producer.Models;

public record SendRequest
{
	[JsonProperty("to")] 
	public string? To { get; set; }
	
	[JsonProperty("subject")]
	public string? Subject { get; set; }
	
	[JsonProperty("body")]
	public string? Body { get; set; }
	
	[JsonProperty("attachments")] 
	public List<string>? Attachments { get; set; }
	
	[JsonProperty("priority")] 
	public string? Priority { get; set; }
}
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using smtp.producer.Models;
using smtp.producer.Producers;


var builder = WebApplication.CreateSlimBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;
var host = builder.Host;
var web = builder.WebHost;

web.ConfigureKestrel((context, options) =>
	options.Configure(context.Configuration));

services.AddControllers()
	.AddNewtonsoftJson(options =>
	{
		options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
	});

services.AddSerilog();
services.AddSingleton(new Producer(configuration).InitializeAsync(CancellationToken.None));
services.AddHealthChecks();

host.UseSerilog((context, options) =>
	options.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

app.UseRouting();
app.UseHealthChecks("/");
app.UseSerilogRequestLogging();
app.MapControllers();
app.MapPost("/smtp", async context =>
{
	var id = Guid.NewGuid().ToString();
	var time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
	context.Response.ContentType = "application/json";

	SendRequest? request;

	try
	{
		request = JsonConvert.DeserializeObject<SendRequest>(
			await new StreamReader(context.Request.Body).ReadToEndAsync());
	}
	catch (JsonException)
	{
		context.Response.StatusCode = 400;
		await context.Response.WriteAsync(
			JsonConvert.SerializeObject(new SendReponse(id, "Invalid request format.", 400, time)));
		return;
	}

	if (string.IsNullOrWhiteSpace(request!.To) ||
	    string.IsNullOrWhiteSpace(request.Subject) ||
	    string.IsNullOrWhiteSpace(request.Body))
	{
		context.Response.StatusCode = 400;
		await context.Response.WriteAsync(
			JsonConvert.SerializeObject(new SendReponse(id, "Missing required fields.", 400, time)));
		return;
	}

	var cancellationToken = context.RequestAborted;
	var serializedMessage = JsonConvert.SerializeObject(new { id, time, request });

	await Task.Run(async () =>
	{
		try
		{
			await Producer.SendAsync(serializedMessage, cancellationToken);
			Log.Information("Message sent successfully: {@RequestId}", id);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to send message: {@RequestId}", id);
		}
	}, cancellationToken);

	context.Response.StatusCode = 202;
	await context.Response.WriteAsync(
		JsonConvert.SerializeObject(new SendReponse(id, "Message is being processed.", 202, time)),
		cancellationToken: cancellationToken);
});

await app.RunAsync();
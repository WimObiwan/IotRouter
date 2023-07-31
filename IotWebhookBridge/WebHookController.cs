using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IotWebhookBridge;

[Route("webhook")]
public class WebhookController : Controller
{
    private readonly IMqttService _mqttService;
    private readonly WebhookOptions _options;

    public WebhookController(IMqttService mqttService, IOptions<WebhookOptions> options)
    {
        _mqttService = mqttService;
        _options = options.Value;
    }
    
    [HttpPost("{*suffix}")]
    public async Task<IActionResult> Post(string suffix)
    {
        if (!string.IsNullOrEmpty(_options.SecretHeader))
        {
            var value = Request.Headers[_options.SecretHeader].FirstOrDefault();
            if (value == null)
                return BadRequest("Header not found");
            if (!string.IsNullOrEmpty(_options.SecretValue)
                && value != _options.SecretValue)
                return BadRequest("Header value incorrect");
        }
        
        using StreamReader reader = new(Request.Body);
        string text = await reader.ReadToEndAsync();
        await _mqttService.SendAsync(text, suffix);
        
        return Ok();
    }
}
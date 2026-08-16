using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartShip.Shipment.Application.Services;

public static class ConsumerHelper
{
    public static async Task<bool> ValidateCustomerExistsAsync(
    IHttpClientFactory factory,
    ILogger logger,
    int customerId,
    IConfiguration config)
    {
        try
        {
            var client = factory.CreateClient("IdentityService");

            var apiKey = config["InternalApi:ApiKey"];

            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Remove("X-Internal-Key");
                client.DefaultRequestHeaders.Add("X-Internal-Key", apiKey);
            }

            var url = $"api/auth/internal/users/{customerId}/exists";

            logger.LogInformation(
                "Calling IdentityService: {BaseAddress}{Url}",
                client.BaseAddress,
                url);

            var response = await client.GetAsync(url);

            var responseBody = await response.Content.ReadAsStringAsync();

            logger.LogInformation(
                "IdentityService response: Status={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                responseBody);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning(
                    "Customer {CustomerId} does not exist or is inactive.",
                    customerId);

                return false;
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to validate customer {CustomerId}",
                customerId);

            return false;
        }
    }
}
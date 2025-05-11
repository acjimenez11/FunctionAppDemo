using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MiFunctionApp
{
    public class SaludoFunction
    {
        private readonly ILogger _logger;

        public SaludoFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SaludoFunction>();
        }

        // Cambiar el nombre de la función para evitar conflictos
        [Function("SaludoUnico")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function procesada.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("¡Hola! Bienvenido a tu Azure Function App con .NET 8 en macOS ARM64");

            return response;
        }
    }
}
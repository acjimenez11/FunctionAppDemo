// FunctionAppDemo/SaludoFunction.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration; // Para IConfiguration
using Microsoft.Extensions.Logging;
using System; // Para UriBuilder
using System.Net;
using System.Net.Http; // Para HttpClient, HttpRequestMessage, HttpResponseMessage
using System.Threading.Tasks; // Para Task
using System.Web; // Para HttpUtility (usado para construir query strings)

namespace FunctionAppDemo
{
    public class SaludoFunction
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Inyecta IHttpClientFactory y IConfiguration
        public SaludoFunction(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<SaludoFunction>();
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [Function("SaludoFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("SaludoFunction: Procesando una solicitud HTTP trigger.");

            // Lee la configuración de API Management desde las variables de entorno/local.settings.json
            string apimEndpointUrl = _configuration["APIM_ENDPOINT_URL"];
            string apimSubscriptionKey = _configuration["APIM_SUBSCRIPTION_KEY"];

            if (string.IsNullOrEmpty(apimEndpointUrl) || string.IsNullOrEmpty(apimSubscriptionKey))
            {
                _logger.LogError("Error: APIM_ENDPOINT_URL o APIM_SUBSCRIPTION_KEY no están configuradas.");
                var configErrorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                configErrorResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await configErrorResponse.WriteStringAsync("Error de configuración interna. Por favor, contacte al administrador.");
                return configErrorResponse;
            }

            // Opcional: Tomar un parámetro 'nombre' de la query string de la solicitud a SaludoFunction
            // y pasarlo al servicio backend.
            string nombreParam = req.Query.Get("nombre");
            string urlParaLlamar = apimEndpointUrl;

            if (!string.IsNullOrEmpty(nombreParam))
            {
                try
                {
                    var uriBuilder = new UriBuilder(apimEndpointUrl);
                    var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                    query["nombre"] = nombreParam; // Añade o actualiza el parámetro 'nombre'
                    uriBuilder.Query = query.ToString();
                    urlParaLlamar = uriBuilder.ToString();
                    _logger.LogInformation($"Se pasará el parámetro 'nombre={nombreParam}' al servicio APIM.");
                }
                catch (UriFormatException ex)
                {
                     _logger.LogError(ex, $"URL base de APIM ('{apimEndpointUrl}') no tiene un formato válido.");
                     var uriErrorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                     uriErrorResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                     await uriErrorResponse.WriteStringAsync("Error de configuración interna (URL de APIM).");
                     return uriErrorResponse;
                }
            }
            
            _logger.LogInformation($"Llamando al servicio APIM en la URL: {urlParaLlamar}");

            // Crea un HttpClient usando la factory
            var client = _httpClientFactory.CreateClient();

            // Prepara la solicitud al servicio APIM
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, urlParaLlamar);
            requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", apimSubscriptionKey); // Clave de suscripción de APIM

            HttpResponseMessage serviceResponse;
            string responseContent = "";

            try
            {
                // Envía la solicitud
                serviceResponse = await client.SendAsync(requestMessage);
                responseContent = await serviceResponse.Content.ReadAsStringAsync();

                if (serviceResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Respuesta exitosa del servicio APIM. Status: {serviceResponse.StatusCode}.");
                    _logger.LogDebug($"Contenido de la respuesta: {responseContent}"); // LogDebug para no llenar logs con datos grandes

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    // Intenta preservar el Content-Type de la respuesta del servicio APIM
                    if (serviceResponse.Content.Headers.ContentType != null)
                    {
                        response.Headers.Add("Content-Type", serviceResponse.Content.Headers.ContentType.ToString());
                    }
                    else
                    {
                        response.Headers.Add("Content-Type", "application/json; charset=utf-8"); // Default si no se especifica
                    }
                    await response.WriteStringAsync(responseContent);
                    return response;
                }
                else
                {
                    _logger.LogError($"Error al llamar al servicio APIM. Status: {serviceResponse.StatusCode}. Respuesta: {responseContent}");
                    
                    var errorResponse = req.CreateResponse(serviceResponse.StatusCode); // Propaga el código de estado
                    errorResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                    await errorResponse.WriteStringAsync($"Error al consumir el servicio APIM: {serviceResponse.ReasonPhrase}. Detalles: {responseContent}");
                    return errorResponse;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Excepción HttpRequestException al llamar al servicio APIM.");
                var exceptionResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                exceptionResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await exceptionResponse.WriteStringAsync($"Excepción al conectar con el servicio APIM: {ex.Message}");
                return exceptionResponse;
            }
            catch (Exception ex) // Captura general para otros errores inesperados
            {
                _logger.LogError(ex, "Error inesperado en SaludoFunction durante la llamada a APIM.");
                var generalExceptionResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                generalExceptionResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await generalExceptionResponse.WriteStringAsync($"Error inesperado en el servidor: {ex.Message}");
                return generalExceptionResponse;
            }
        }
    }
}
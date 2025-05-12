using Microsoft.Extensions.DependencyInjection; // Necesario para AddHttpClient
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services => {
        // Registra IHttpClientFactory y los servicios relacionados de HttpClient
        services.AddHttpClient(); 
    })
    .Build();

host.Run();
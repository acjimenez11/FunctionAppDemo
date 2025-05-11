using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services => {
        // Aquí puedes registrar servicios adicionales si los necesitas
    })
    .Build();

host.Run();
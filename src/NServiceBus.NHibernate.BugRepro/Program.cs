// Reproduction of https://github.com/Particular/NServiceBus.NHibernate/issues/959
// Based on gist: https://gist.github.com/DavidBoike/dc4a5afc739b522cb0fb9134343095f6
//
// Two namespaces have Order/OrderLine entities mapped to the same database tables.
// The saga mapper incorrectly creates indexes for these non-saga collections,
// causing: "NHibernate.MappingException: Index IDX_9669ABAB already exists!"

using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Transport.SqlServer;

var connectionString = Environment.GetEnvironmentVariable("SQLServerConnectionString")
    ?? "Server=localhost;Database=nservicebus;User Id=sa;Password=NServiceBus!1;TrustServerCertificate=True;Encrypt=False";

Console.WriteLine("=== NServiceBus.NHibernate Issue #959 Reproduction ===");
Console.WriteLine($"Connection: {connectionString}");
Console.WriteLine();

var endpointConfiguration = new EndpointConfiguration("Issue959-Repro");

endpointConfiguration.EnableInstallers();
endpointConfiguration.SendFailedMessagesTo("error");

// --- Transport: SQL Server ---
var transport = new SqlServerTransport(connectionString);
transport.SchemaAndCatalog.UseSchemaForQueue("error", "dbo");
endpointConfiguration.UseTransport(transport);

// --- NHibernate mappings: two namespaces with identical table targets ---
var mapper = new ModelMapper();
mapper.AddMapping(typeof(SomeNamespace.OrderMap));
mapper.AddMapping(typeof(SomeNamespace.OrderLineMap));
mapper.AddMapping(typeof(SomeOtherNamespace.OrderMap));
mapper.AddMapping(typeof(SomeOtherNamespace.OrderLineMap));

var compiled = mapper.CompileMappingForAllExplicitlyAddedEntities();
compiled.autoimport = false;

var nhConfig = new Configuration();
nhConfig.SetProperty(Environment.Dialect, typeof(MsSql2012Dialect).FullName);
nhConfig.SetProperty(Environment.ConnectionDriver, typeof(MicrosoftDataSqlClientDriver).FullName);
nhConfig.SetProperty(Environment.ConnectionString, connectionString);
nhConfig.AddMapping(compiled);

// --- Persistence: NHibernate with the pre-loaded non-saga mappings ---
endpointConfiguration.UsePersistence<NHibernatePersistence>()
    .UseConfiguration(nhConfig);

try
{
    Console.WriteLine("Starting endpoint...");
    var endpoint = await Endpoint.Start(endpointConfiguration);
    Console.WriteLine();
    Console.WriteLine("SUCCESS: Endpoint started without errors.");
    Console.WriteLine("The fix for issue #959 is working correctly.");
    Console.WriteLine();
    Console.WriteLine("Press Ctrl+C to stop...");

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    try { await Task.Delay(Timeout.Infinite, cts.Token); }
    catch (OperationCanceledException) { }

    await endpoint.Stop();
    Console.WriteLine("Endpoint stopped.");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("FAILURE: Endpoint failed to start!");
    Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner:     {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    }
    Console.WriteLine();
    Console.WriteLine("This is the bug described in issue #959.");
    Environment.ExitCode = 1;
}

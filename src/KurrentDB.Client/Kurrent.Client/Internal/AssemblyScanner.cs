using System.Reflection;
using KurrentDB.Client;

namespace Kurrent.Client;

/// <summary>
/// Scans assemblies and provides a mechanism to load and retrieve types from the assemblies.
/// The scanner supports parallel queries on the loaded types and allows filtering
/// using custom matching criteria.
/// </summary>
[PublicAPI]
class AssemblyScanner {
	/// <summary>
	/// Provides access to a singleton instance of the <see cref="AssemblyScanner"/> class.
	/// This instance is lazily initialized and thread-safe. It enables scanning and retrieving types
	/// from loaded assemblies based on specified criteria.
	/// </summary>
	public static AssemblyScanner System => LazySystemInstance.Value;

	static readonly Lazy<AssemblyScanner> LazySystemInstance = new(() => new(), LazyThreadSafetyMode.ExecutionAndPublication);

    static readonly string[] DefaultExcludePatterns = [
	    // Microsoft and .NET Framework libraries
	    "system.",              // Core .NET Framework libraries and namespaces
        "microsoft.",           // Microsoft libraries and frameworks
        "windows.",             // Windows-specific functionality
        "netstandard.",         // .NET Standard libraries
        "mscorlib.",            // Core .NET runtime library
        "dotnet.",              // .NET Core specific libraries
        "runtime.",             // Runtime-related libraries
        "blazor.",              // Microsoft Blazor framework

        // Others
        "serilog.",             // Structured logging library
        "nlog.",                // Flexible logging platform
        "log4net.",             // Logging framework for .NET
        "newtonsoft.",          // JSON.NET serialization library
        "njsonschema.",         // JSON Schema validation
        "protobuf.",            // Protocol Buffers serialization
	    "grpc.core.",           // gRPC communication framework
	    "grpc.net.",            // gRPC communication framework
	    "rabbitmq.",            // RabbitMQ client
	    "kafka.",               // Kafka client libraries
	    "confluent.",           // Confluent Kafka libraries
	    "stackexchange.",       // StackExchange.Redis and other SE libraries
	    "humanizer.",           // String manipulation and humanization
	    "oneof.",               // Discriminated union implementation
	    "polly.",               // Resilience and transient fault handling
	    "dotnext.",             // .NET extensions and utilities
	    "lrucache.",            // LRU Cache implementation
	    "swashbuckle.",         // Swagger/OpenAPI documentation
	    "elastic.",             // Elasticsearch client
	    "eventuous.",           // Event sourcing and CQRS libraries
	    "jetbrains.",           // JetBrains libraries
	    "namotion",             // Namotion libraries
	    "faster",               // Faster
	    "fluentstorage.",       // FluentStorage libraries
	    "jsonpath.",            // JSONPath libraries
	    "jint.",                // Jint libraries
	    "jsoncons.",            // JsonCons libraries
	    "nodatime.",            // NodaTime libraries
	    "scrutor.",             // Scrutor libraries
	    "autofac.",             // Autofac IoC container
	    "ninject.",             // Ninject IoC container
	    "simpleinjector.",      // Simple Injector IoC container
	    "prometheus.",          // Prometheus monitoring
	    "applicationinsights.", // Azure Application Insights
	    "opentelemetry.",       // OpenTelemetry observability framework
	    "azure.",               // Microsoft Azure SDK
	    "awssdk.",              // Amazon Web Services SDK
	    "entityframework.",     // Entity Framework ORM
	    "dapper.",              // Lightweight ORM
	    "npgsql.",              // PostgreSQL data provider
	    "sqlite.",              // SQLite data provider
	    "pomelo.",              // MySQL provider
	    "mongodb.",             // MongoDB client
	    "bouncycastle.",        // Cryptography library
	    "castle.",              // Castle Project libraries including DynamicProxy
    ];

    public AssemblyScanner(params Assembly?[] assemblies) {
        if (assemblies.Length == 0)
            assemblies = LoadAssemblies(AppDomain.CurrentDomain.BaseDirectory, IsRelevantAssembly);

        Assemblies = assemblies.Where(x => x is not null).Cast<Assembly>().ToArray();
        LazyTypes  = new(() => LoadAllTypes(Assemblies));
    }

    Assembly[]       Assemblies { get; }
    Lazy<List<Type>> LazyTypes  { get; }

    /// <summary>
    /// Retrieves a parallel query of all types loaded from the assemblies scanned by the <see cref="AssemblyScanner"/>.
    /// </summary>
    /// <returns>A <see cref="ParallelQuery{Type}"/> containing all types found in the scanned assemblies.</returns>
    public ParallelQuery<Type> Scan() => LazyTypes.Value.AsParallel();

    /// <summary>
    /// Determines whether the specified assembly filename is relevant by checking against predefined exclusion patterns.
    /// </summary>
    /// <param name="filename">The name of the assembly file to evaluate for relevance.</param>
    /// <returns><c>true</c> if the assembly is considered relevant; otherwise, <c>false</c>.</returns>
    static bool IsRelevantAssembly(string filename) =>
	    !DefaultExcludePatterns.Any(pattern => filename.StartsWith(pattern, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Loads all types from the specified assemblies, optionally including internal types.
    /// </summary>
    /// <param name="assemblies">An array of <see cref="Assembly"/> objects to load types from.</param>
    /// <param name="includeInternalTypes">A boolean indicating whether to include internal types or only exported types. Default is <c>true</c>.</param>
    /// <returns>A <see cref="List{Type}"/> containing all distinct loaded types.</returns>
    static List<Type> LoadAllTypes(Assembly[] assemblies, bool includeInternalTypes = true) {
	    return assemblies
		    .AsParallel()
		    .SelectMany(ass => GetAllTypes(ass, includeInternalTypes))
            .Distinct()
            .ToList();

        static IEnumerable<Type> GetAllTypes(Assembly assembly, bool includeInternalTypes) {
            try {
                return includeInternalTypes ? assembly.GetTypes() : assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException ex) when (ex.Types is not null) {
                return ex.Types.Where(type => type is not null).Cast<Type>();
            }
        }
    }

    /// <summary>
    /// Loads assemblies from the specified directory based on the given criteria and returns them as an array of <see cref="Assembly"/>.
    /// </summary>
    /// <param name="directoryPath">The path of the directory from which to load assemblies.</param>
    /// <param name="assemblyFileNameFilter">
    /// A predicate used to filter assembly file names. If null, all assemblies in the directory are included.
    /// </param>
    /// <param name="onError">
    /// An <see cref="Action{T1, T2}"/> to handle errors that occur while loading assemblies. The first parameter is the file path of the assembly, and the second parameter is the exception that occurred.
    /// </param>
    /// <returns>An array of <see cref="Assembly"/> objects loaded from the specified directory.</returns>
    static Assembly[] LoadAssemblies(string directoryPath, Predicate<string>? assemblyFileNameFilter = null, Action<string, Exception>? onError = null) {
	    var assemblies = new List<Assembly>();

	    foreach (var assemblyFile in Directory.EnumerateFiles(directoryPath, "*.dll", SearchOption.TopDirectoryOnly)) {
            if (!(assemblyFileNameFilter?.Invoke(Path.GetFileName(assemblyFile)) ?? true))
                continue;

            try {
                // Load the assembly from the specified path
                var assembly = Assembly.LoadFrom(assemblyFile);
                assemblies.Add(assembly);
            }
            catch (Exception ex) {
                onError?.Invoke(assemblyFile, ex);
            }
        }

        return assemblies.ToArray();
    }
}

/// <summary>
/// Provides extension methods for filtering and querying types in an <see cref="AssemblyScanner"/> instance.
/// The methods allow narrowing down results by type, generic type, or specific attributes such as full name
/// and instantiability of classes.
/// </summary>
static class AssemblyScannerExtensions {
	/// <summary>
	/// Filters the query to include only types that are assignable to the specified type and are instantiable classes.
	/// </summary>
	/// <param name="scan">The parallel query of types to filter.</param>
	/// <param name="type">The type that the returned types must be assignable to.</param>
	/// <returns>A <see cref="ParallelQuery{Type}"/> containing types that are assignable to the specified type and are instantiable classes.</returns>
	internal static ParallelQuery<Type> InstancesOf(this ParallelQuery<Type> scan, Type type) =>
		scan.Where(t => type.IsAssignableFrom(t) && t.IsInstantiableClass());

	/// <summary>
	/// Filters the query to include only types that are assignable to the specified generic type and are instantiable classes.
	/// </summary>
	/// <param name="scan">The parallel query of types to filter.</param>
	/// <typeparam name="T">The type that the returned types must be assignable to.</typeparam>
	/// <returns>A <see cref="ParallelQuery{Type}"/> containing types that are assignable to the specified generic type and are instantiable classes.</returns>
	internal static ParallelQuery<Type> InstancesOf<T>(this ParallelQuery<Type> scan) =>
		InstancesOf(scan, typeof(T));

	/// <summary>
	/// Filters the query to include only types that match the specified full name and are instantiable classes.
	/// </summary>
	/// <param name="scan">The parallel query of types to filter.</param>
	/// <param name="name">The full name of the type to match.</param>
	/// <returns>A <see cref="ParallelQuery{Type}"/> containing types that match the specified full name and are instantiable classes.</returns>
	internal static ParallelQuery<Type> InstancesWithFullName(this ParallelQuery<Type> scan, string name) =>
		scan.Where(t => t.MatchesFullName(name) && t.IsInstantiableClass());

	/// <summary>
	/// Retrieves the first type from a parallel query of types or returns a placeholder type if the query is empty.
	/// </summary>
	/// <param name="scan">The parallel query of <see cref="Type"/> objects to evaluate.</param>
	/// <returns>The first <see cref="Type"/> in the query or a placeholder type if no types are found.</returns>
	internal static Type FirstOrMissing(this ParallelQuery<Type> scan) =>
		scan.FirstOrDefault() ?? SystemTypes.MissingType;

    /// <summary>
    /// Filters the scanned types to include only those within the specified namespace prefix.
    /// </summary>
    /// <param name="scan">A <see cref="ParallelQuery{Type}"/> containing types to be filtered.</param>
    /// <param name="namespacePrefix">The namespace prefix to be matched against the namespaces of the types.</param>
    /// <returns>A <see cref="ParallelQuery{Type}"/> containing types that match the given namespace prefix.</returns>
    internal static ParallelQuery<Type> InstancesInNamespace(this ParallelQuery<Type> scan, string namespacePrefix) =>
        scan.Where(t => t.MatchesNamespace(namespacePrefix));
}

# ComponentDiscovery

...describes the problem domain which relates to the **use case of loading plugins & extensions** into a ['Composite Application'](https://en.wikipedia.org/wiki/Composite_application) (like a 'Shell') **which are provided decentralized**. One common way is the registration of several sources/locations into a centralized list, may be within a file or the windows registry. Another way is to crawl some certain file system directories in order to find libraries to load.

This framework will do the last approach in a high efficient way an provides easy-to-use artifacts for each level of consume. And it was tested / is already used in some enterprise-environment!

* You can just use it to extend the .NET out-of-the-box type resolving (alias ['Fusion'](https://docs.microsoft.com/de-de/dotnet/framework/unmanaged-api/fusion/)) to do **lookups on additional directories**. 
* You can use our '**AssemblyIndexer**' to subscribe a callback for every new loaded assembly.
* You can use our '**ClassificationBasedAssemblyIndxer**' which works like a [Mandatory access control](https://en.wikipedia.org/wiki/Mandatory_access_control) using '**Clearances**' to control, which plug ins should be loaded, based on certain assembly meta-data or namespace-patterns. (may be to **load different Product-Portfolios** for specific Licenses or different customizings for specific tenants)
* You can use our '**ExtensionMethodIndexer**' to enumerate '**extension methods**' for a type **via reflection** (as easy as real implemented methods of that type). This allows you to build framework technology, which also supports 'extension methods' with a semantics of a 'decentralized member' (like 'Calculated Fields' on a Entity) in order not to stand in the way of the settlement of this in its correct problem domain.

* You can use our '**TypeIndexer**' to subscribe a callback for every type which **has a given attribute**, **inherits from a given base** or **implements a given interface**.
* You can use our generic '**ProviderRepository**' which will **automatically initialize and dispose** instances of providers (of a given type) for you.
* You can use our '**InstanceDiscovery**'-Framework which comes as a additional, high level layer above the 'ComponentDiscovery' and represents a **lightweight and smart DI framework** - also with the focus on decentralized supply of service-instances (Singletons) and/or service-factories. See the '**InstanceDiscoveryContext**'. 

You can install this Lib via [NuGet](https://www.nuget.org/packages/ComponentDiscovery/)
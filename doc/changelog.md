# Change log
This files contains a version history including all changes relevant for semantic Versioning...

*(it is automatically maintained using the ['KornSW-VersioningUtil'](https://github.com/KornSW/VersioningUtil))*




## Upcoming Changes

* set preferAssemblyLoadingViaFusion to true for default Indexer in InstanceDiscovery



## v 4.11.0
released **2025-12-10**, including:
 - **new Feature**: added '*InstanceDiscoveryContext.UseExternalManagedTypeIndexer*' and '*InstanceDiscoveryContext.ActivateScoping*' to allow less invasive customizing of runtime scopes
 - Fix: not more NullReferenceException when InstanceDiscovery reading provided instances from Private Getter-Methods



## v 4.10.3
released **2025-06-05**, including:
 - Fix: added Recursion-detection for Appdomain-LoadAssembly event when doing Fallback-load via fileName (after Fusion-Load has thrown an Exception).



## v 4.10.2
released **2025-05-30**, including:
 - Removed .NET 4.6-Targets and enabled .NET 8.0-Targets (while switching build-runner from Win-2019 to WIN-2022)



## v 4.10.1
released **2025-02-11**, including:
 - new revision without significant changes



## v 4.10.0
released **2025-02-11**, including:
 - **new Feature**: added 'DnLib' based Assembly-Classification-detection-Strategy
 - **new Feature**: added Target for .NET4.8
 - **new Feature**: added Target for .NET8
 - removed Target for .NET5
 - updated Newtonsoft to 13.0.3

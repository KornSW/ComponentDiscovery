Imports System

Public Interface IInstanceProvidingAttribute

  ReadOnly Property InjectionDemand As InjectionDemand
  ReadOnly Property DiscoverableAsType As Type
  ReadOnly Property ProvidesNullDiscoverable As Boolean

End Interface

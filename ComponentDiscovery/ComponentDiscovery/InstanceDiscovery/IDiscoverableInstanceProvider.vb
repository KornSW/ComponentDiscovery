Imports System

Public Interface IDiscoverableInstanceProvider

  ''' <summary>
  ''' This used to address an instance-source when relating multiple sources in order to specify
  ''' overriding rules like 'prefer MyCustomizedServiceProvider instead of MyCommonServiceProvider'.
  ''' In most cases this is equal to the concrete implmentation of the 'IDiscoverableInstanceProvider'
  ''' interface except in constellations, where providers are just wrapping another source -> then its
  ''' possible to disclose the 'real' source here (to be addressed by overriding rules)
  ''' </summary>
  ReadOnly Property RepresentingOriginType As Type

  ''' <summary>
  ''' Each provider which is added to the InstanceAccessContext be be asked to 
  ''' declare priorization rules (in addition to the possibility to declare priorization
  ''' rules in a centralized way on directly at the InstanceAccessContext). If this
  ''' method is invoked, then the provider can use the callback to create one or more
  ''' rules by passign a foreign/related origin type and a value of 'true' 
  ''' when the current provider shloud be preferred against this type or 'false' when the foreign
  ''' one should be preferred.
  ''' </summary>
  Sub DeclarePriorizationRules(callback As Action(Of Type, Boolean))

  ''' <summary>
  ''' If the provider offers a fixed amount of known types, then it can declare them over this
  ''' property to avoid being asked by the framework for instances of any other type.
  ''' An empty array as return value has the semantic, that no instances of any type is provided.
  ''' An return value of null has the semantic, that the provider has a dynamic amount of known types,
  ''' which forces the framework to ask it each time. 
  ''' </summary>
  ReadOnly Property DedicatedDiscoverableTypes As Type()

  ''' <summary>
  ''' 
  ''' </summary>
  ''' <param name="requestingContext"></param>
  ''' <param name="requestedType"></param>
  ''' <param name="instance"></param>
  ''' <param name="lifetimeResponsibility">
  ''' informs, which party (the provider or the consumer) has the responsibility
  ''' to manage the lifecycle of the returned instances including their disposal.
  ''' </param>
  ''' <returns></returns>
  Function TryGetInstance(requestingContext As InstanceDiscoveryContext, requestedType As Type, ByRef instance As Object, ByRef lifetimeResponsibility As LifetimeResponsibility) As Boolean

End Interface

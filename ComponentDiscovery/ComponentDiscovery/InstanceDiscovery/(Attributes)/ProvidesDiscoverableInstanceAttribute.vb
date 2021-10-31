Imports System

''' <summary>
''' Declares, that this STATIC PARAMETERLESS METHOD or STATIC PARAMETERLESS PROPERTY provides a
''' managed instance (usually a SINGLETON) to be discovered.
''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
''' </summary>
<AttributeUsage(AttributeTargets.Method Or AttributeTargets.Property, AllowMultiple:=True)>
Public Class ProvidesDiscoverableInstanceAttribute
  Inherits Attribute
  Implements IInstanceProvidingAttribute

  ''' <summary>
  ''' Declares, that this STATIC PARAMETERLESS METHOD or STATIC PARAMETERLESS PROPERTY provides a
  ''' managed instance (usually a SINGLETON) to be discovered.
  ''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
  ''' </summary>
  ''' <param name="discoverableAsType">
  ''' needs to be specified only if the type under which this instance should be
  ''' discoverable is not equal to to the return type (may be a base-class or interface).
  ''' </param>
  ''' <param name="providesNullDiscoverable">
  ''' if true, then the provider will use returned null values as valid discovery result and pass it to the consumers 
  ''' (this option is useful, when the instance is optional within the system).
  ''' Otherwise the provider will just respond that it cannot provide instances of this type, so that the framework 
  ''' will skip the provider and continue asking other providers (this option is useful, when the instance is just not jet available
  ''' or there are multiple providers for one type).
  ''' </param>
  Public Sub New(Optional discoverableAsType As Type = Nothing, Optional providesNullDiscoverable As Boolean = False)
    Me.DiscoverableAsType = discoverableAsType
    Me.ProvidesNullDiscoverable = providesNullDiscoverable
  End Sub

  Private ReadOnly Property InjectionDemand As InjectionDemand Implements IInstanceProvidingAttribute.InjectionDemand
    Get
      Return InjectionDemand.IfAvailable
    End Get
  End Property

  Public ReadOnly Property DiscoverableAsType As Type Implements IInstanceProvidingAttribute.DiscoverableAsType

  Public ReadOnly Property ProvidesNullDiscoverable As Boolean Implements IInstanceProvidingAttribute.ProvidesNullDiscoverable

End Class

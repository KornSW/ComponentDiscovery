'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System

Namespace Composition.InstanceDiscovery

  ''' <summary>
  ''' Declares, that this STATIC PARAMETERLESS METHOD or STATIC PARAMETERLESS PROPERTY provides a
  ''' managed instance (usually a SINGLETON) to be discovered.
  ''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
  ''' NOTE: if this attribute is used on targets with non-optional parameters, you'll need to declare
  ''' an 'InjectAttribute' or 'TryInjectAttribute' on each of these parameters to enable DI!
  ''' </summary>
  <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Property, AllowMultiple:=True)>
  Public Class ProvidesDiscoverableInstanceAttribute
    Inherits Attribute

    ''' <summary>
    ''' Declares, that this STATIC PARAMETERLESS METHOD or STATIC PARAMETERLESS PROPERTY provides a
    ''' managed instance (usually a SINGLETON) to be discovered.
    ''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
    ''' NOTE: if this attribute is used on targets with non-optional parameters, you'll need to declare
    ''' an 'InjectAttribute' or 'TryInjectAttribute' on each of these parameters to enable DI!
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

    Public ReadOnly Property DiscoverableAsType As Type

    Public ReadOnly Property ProvidesNullDiscoverable As Boolean

  End Class

End Namespace

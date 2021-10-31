Imports System

''' <summary>
''' Declares, that this CONSTRUCTOR or STATIC METHOD shall be used as factory to
''' create discoverable (short living) instances 'on-demand' when they are requested.
''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
''' </summary>
<AttributeUsage(AttributeTargets.Constructor Or AttributeTargets.Method, AllowMultiple:=True)>
Public Class CreatesDiscoverableInstanceAttribute
  Inherits Attribute
  Implements IInstanceProvidingAttribute

  ''' <summary>
  ''' Declares, that this CONSTRUCTOR or STATIC METHOD shall be used as factory to
  ''' create discoverable (short living) instances 'on-demand' when they are requested.
  ''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
  ''' </summary>
  ''' <param name="discoverableAsType">
  ''' needs to be specified only if the type under which this instance should be
  ''' discoverable is not equal to to the return type (may be a base-class or interface).
  ''' </param>
  Public Sub New(Optional discoverableAsType As Type = Nothing)
    Me.InjectionDemand = InjectionDemand.Disabled
    Me.DiscoverableAsType = discoverableAsType
  End Sub

  ''' <summary>
  ''' 
  ''' </summary>
  ''' <param name="injectionDemand"></param>
  ''' <param name="discoverableAsType"></param>
  Public Sub New(injectionDemand As InjectionDemand, Optional discoverableAsType As Type = Nothing)
    Me.InjectionDemand = injectionDemand
    Me.DiscoverableAsType = discoverableAsType
  End Sub

  Public ReadOnly Property InjectionDemand As InjectionDemand Implements IInstanceProvidingAttribute.InjectionDemand

  Public ReadOnly Property DiscoverableAsType As Type Implements IInstanceProvidingAttribute.DiscoverableAsType

  Private ReadOnly Property ProvidesNullDiscoverable As Boolean Implements IInstanceProvidingAttribute.ProvidesNullDiscoverable
    Get
      Return False
    End Get
  End Property

End Class

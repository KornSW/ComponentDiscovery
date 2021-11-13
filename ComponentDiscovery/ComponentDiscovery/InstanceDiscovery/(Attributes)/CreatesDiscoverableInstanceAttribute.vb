'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System

Namespace Composition.InstanceDiscovery

  ''' <summary>
  ''' Declares, that this CONSTRUCTOR or STATIC METHOD shall be used as factory to
  ''' create discoverable (short living) instances 'on-demand' when they are requested.
  ''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
  ''' NOTE: if this attribute is used on targets with non-optional parameters, you'll need to declare
  ''' an 'InjectAttribute' or 'TryInjectAttribute' on each of these parameters to enable DI!
  ''' </summary>
  <AttributeUsage(AttributeTargets.Constructor Or AttributeTargets.Method, AllowMultiple:=True)>
  Public Class CreatesDiscoverableInstanceAttribute
    Inherits Attribute

    ''' <summary>
    ''' Declares, that this CONSTRUCTOR or STATIC METHOD shall be used as factory to
    ''' create discoverable (short living) instances 'on-demand' when they are requested.
    ''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
    ''' NOTE: if this attribute is used on targets with non-optional parameters, you'll need to declare
    ''' an 'InjectAttribute' or 'TryInjectAttribute' on each of these parameters to enable DI!
    ''' </summary>
    ''' <param name="discoverableAsType">
    ''' needs to be specified only if the type under which this instance should be
    ''' discoverable is not equal to to the return type (may be a base-class or interface).
    ''' </param>
    Public Sub New(Optional discoverableAsType As Type = Nothing)
      Me.DiscoverableAsType = discoverableAsType
    End Sub

    Public ReadOnly Property DiscoverableAsType As Type

  End Class

End Namespace

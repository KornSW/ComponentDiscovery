'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System

Namespace Composition.InstanceDiscovery

  ''' <summary>
  ''' Declares, that the current class consumes discoverable instances, provides discoverable instances or can be used to create some.
  ''' This attribute just flags the current class to be inspected when the instance discovery framework
  ''' is searching for Methods, Properties or Constructors with a 'ProvidesDiscoverableInstanceAttribute', 
  ''' 'CreatesDiscoverableInstanceAttribute' or 'InjectAttribute'.
  ''' These must be used together with this attribute on the class.
  ''' </summary>
  <AttributeUsage(AttributeTargets.Class, AllowMultiple:=False)>
  Public Class SupportsInstanceDiscoveryAttribute
    Inherits Attribute

    ''' <summary>
    ''' Declares, that the current class consumes discoverable instances, provides discoverable instances or can be used to create some.
    ''' This attribute just flags the current class to be inspected when the instance discovery framework
    ''' is searching for Methods, Properties or Constructors with a 'ProvidesDiscoverableInstanceAttribute', 
    ''' 'CreatesDiscoverableInstanceAttribute' or 'InjectAttribute'.
    ''' These must be used together with this attribute on the class.
    ''' </summary>
    Public Sub New()
    End Sub

  End Class

End Namespace

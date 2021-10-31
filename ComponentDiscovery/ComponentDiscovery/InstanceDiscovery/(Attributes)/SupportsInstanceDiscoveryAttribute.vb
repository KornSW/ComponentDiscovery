Imports System

''' <summary>
''' Declares, that the current class consumes discoverable instances, provides discoverable instances or can be used to create some.
''' This attribute just flags the current class to be inspected when the instance discovery framework
''' is searching for Methods, Properties or Constructors with a 'ConsumesDiscoverableInstanceAttribute',
''' 'ProvidesDiscoverableInstanceAttribute' or 'CreatesDiscoverableInstanceAttribute'.
''' These must be used together with this attribute on the class.
''' </summary>
<AttributeUsage(AttributeTargets.Class, AllowMultiple:=False)>
Public Class SupportsInstanceDiscoveryAttribute
  Inherits Attribute

  ''' <summary>
  ''' Declares, that the current class provides discoverable instances or can be used to create some.
  ''' This attributes just flags the current class to be inspected when the instance discovery framework
  ''' is searching for Methods, Properties or Constructors with a 'ProvidesDiscoverableInstanceAttribute' or
  ''' 'CreatesDiscoverableInstanceAttribute'. These must be used together with this attribute on the class.
  ''' </summary>
  Public Sub New()
  End Sub

End Class

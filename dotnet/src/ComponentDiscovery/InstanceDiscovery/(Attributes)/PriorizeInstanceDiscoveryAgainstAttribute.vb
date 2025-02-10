'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System

Namespace Composition.InstanceDiscovery

  ''' <summary>
  ''' Declares a rule which affects the priorization of all discoverable instances, which are registered using a
  ''' 'CreatesDiscoverableInstanceAttribute' or 'ProvidesDiscoverableInstanceAttribute' on this class,
  ''' against discoverable instances which are provided from the specified foreign provider/origin type.
  ''' </summary>
  <AttributeUsage(AttributeTargets.Class, AllowMultiple:=True)>
  Public Class PriorizeInstanceDiscoveryAgainstAttribute
    Inherits Attribute

    ''' <summary>
    ''' Declares a rule which affects the priorization of all discoverable instances, which are registered using a
    ''' 'CreatesDiscoverableInstanceAttribute' or 'ProvidesDiscoverableInstanceAttribute' on this class,
    ''' against discoverable instances which are provided from the specified foreign provider/origin type.
    ''' </summary>
    ''' <param name="foreignOriginType">
    ''' Provider/origin type against which the current class shoud be priorized
    ''' </param>
    ''' <param name="currentIsPreferred">
    ''' Specifies, the current class is the preferred one ('false' will prefer the given 'foreignOriginType')
    ''' </param>
    Public Sub New(foreignOriginType As Type, currentIsPreferred As Boolean)
      Me.ForeignOriginType = foreignOriginType
      Me.CurrentIsPreferred = currentIsPreferred
    End Sub

    ''' <summary>
    ''' Provider/origin type against which the current class shoud be priorized
    ''' </summary>
    Public ReadOnly Property ForeignOriginType As Type

    ''' <summary>
    ''' Specifies, the current class is the preferred one ('false' will prefer the given 'foreignOriginType')
    ''' </summary>
    Public ReadOnly Property CurrentIsPreferred As Boolean

  End Class

End Namespace

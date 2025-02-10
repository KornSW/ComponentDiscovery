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
  ''' against discoverable instances which are provided by any foreign provider/origin type.
  ''' </summary>
  <AttributeUsage(AttributeTargets.Class, AllowMultiple:=True)>
  Public Class PriorizeInstanceDiscoveryAttribute
    Inherits Attribute

    ''' <summary>
    ''' Declares a rule which affects the priorization of all discoverable instances, which are registered using a
    ''' 'CreatesDiscoverableInstanceAttribute' or 'ProvidesDiscoverableInstanceAttribute' on this class,
    ''' against discoverable instances which are provided by any foreign provider/origin type.
    ''' </summary>
    ''' <param name="currentIsPreferred">
    ''' Specifies, the current class is the preferred one ('false' will prefer any other foreign provider/origin)
    ''' </param>
    Public Sub New(currentIsPreferred As Boolean)
      Me.CurrentIsPreferred = currentIsPreferred
    End Sub

    ''' <summary>
    ''' Specifies, the current class is the preferred one ('false' will prefer any other foreign provider/origin)
    ''' </summary>
    Public ReadOnly Property CurrentIsPreferred As Boolean

  End Class

End Namespace

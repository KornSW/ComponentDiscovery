'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Threading

Namespace Composition.InstanceDiscovery

  Partial Class InstanceDiscoveryContext
    Implements IDisposable

    Private _LifetimeTracker As New CallTreeTracker(
      AddressOf Me.DisposeSelfManagedInstances
    )

    ''' <summary>
    ''' disposes all 'self-managed' instances for which the lifetime-handling has been delegated to this context
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
      _ContextDisposalHandler.Invoke(Me)

      _LifetimeTracker.EnterTerminationPhase()

      'Me.DisposeSelfManagedInstances() ... should no be called via _LifetimeTracker
    End Sub

    Private Sub DisposeSelfManagedInstances()
      SyncLock _SelfManagedInstances
        For Each selfManagedInstance In _SelfManagedInstances.Values
          If (selfManagedInstance IsNot Nothing AndAlso TypeOf (selfManagedInstance) Is IDisposable) Then
            DirectCast(selfManagedInstance, IDisposable).Dispose()
          End If
        Next
        _SelfManagedInstances.Clear()
      End SyncLock
    End Sub

  End Class

End Namespace

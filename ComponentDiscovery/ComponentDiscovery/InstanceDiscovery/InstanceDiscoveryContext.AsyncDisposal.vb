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

    Private _LifetimeTracker As CallTreeTracker = Nothing

    ''' <summary>
    ''' starts tracking of sub-threads in order to wait for them
    ''' to finish during the disposal of this instance.
    ''' Calling the 'Dispose' method will NOT wait and immediately end,
    ''' but the disposal itself will be delayed in background 
    ''' (so this works fine with a 'using'-block)!  
    ''' </summary>
    Public Sub EnableDelayedDisposal()
      _LifetimeTracker = New CallTreeTracker(
        AddressOf Me.DisposeSelfManagedInstances
      )
    End Sub

    ''' <summary>
    ''' disposes all 'self-managed' instances for which the lifetime-handling has been delegated to this context
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
      _ContextDisposalHandler.Invoke(Me)

      If (_LifetimeTracker IsNot Nothing) Then
        _LifetimeTracker.EnterTerminationPhase()
        'Me.DisposeSelfManagedInstances() ... should now be called via _LifetimeTracker
      Else
        Me.DisposeSelfManagedInstances()
      End If

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

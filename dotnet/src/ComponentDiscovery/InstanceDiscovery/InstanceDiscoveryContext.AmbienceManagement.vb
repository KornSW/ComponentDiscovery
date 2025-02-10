'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Ambience
Imports System.Diagnostics

Namespace Composition.InstanceDiscovery

  Partial Class InstanceDiscoveryContext
    Implements IDisposable

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _ContextAmbienceManager As ContextAmbienceManager(Of InstanceDiscoveryContext) = Nothing

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _CurrentContextGetter As CurrentContextGetter(Of InstanceDiscoveryContext) = (
      Function()
        If (_ContextAmbienceManager Is Nothing) Then
          _ContextAmbienceManager = New ContextAmbienceManager(Of InstanceDiscoveryContext)()
        End If
        Return _ContextAmbienceManager.GetCurrent(False)
      End Function
    )

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _ContextCreationHandler As ContextCreationHandler(Of InstanceDiscoveryContext) = (
      Sub(context As InstanceDiscoveryContext)
        If (_ContextAmbienceManager Is Nothing) Then
          _ContextAmbienceManager = New ContextAmbienceManager(Of InstanceDiscoveryContext)()
        End If
        _ContextAmbienceManager.ThreadEntering(context)
      End Sub
    )

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _ContextDisposalHandler As ContextDisposalHandler(Of InstanceDiscoveryContext) = (
      Sub(context As InstanceDiscoveryContext)
        If (_ContextAmbienceManager Is Nothing) Then
          _ContextAmbienceManager = New ContextAmbienceManager(Of InstanceDiscoveryContext)()
        End If
        _ContextAmbienceManager.ThreadLeaving(context)
      End Sub
    )

    Public Shared ReadOnly Property Current As InstanceDiscoveryContext
      Get
        Return _CurrentContextGetter.Invoke()
      End Get
    End Property

    Public Shared Sub HookAmbienceManagment(
      currentContextGetter As CurrentContextGetter(Of InstanceDiscoveryContext),
      contextCreationHandler As ContextCreationHandler(Of InstanceDiscoveryContext),
      contextDisposalHandler As ContextDisposalHandler(Of InstanceDiscoveryContext)
    )
      _CurrentContextGetter = currentContextGetter
      _ContextCreationHandler = contextCreationHandler
      _ContextDisposalHandler = contextDisposalHandler
    End Sub

  End Class

End Namespace

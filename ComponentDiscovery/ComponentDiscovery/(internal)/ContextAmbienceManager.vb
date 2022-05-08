Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading

Namespace System.Ambience

#Region " Sample "

  '  Public Class MyContext
  '    Implements IDisposable

  '    Public Sub New()
  '      _ContextCreationHandler.Invoke(Me)
  '    End Sub

  '    Public Sub Dispose() Implements IDisposable.Dispose
  '      _ContextDisposalHandler.Invoke(Me)
  '    End Sub

  '#Region " Ambience Management "

  '    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  '    Private Shared _ContextAmbienceManager As ContextAmbienceManager(Of MyContext) = Nothing

  '    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  '    Private Shared _CurrentContextGetter As CurrentContextGetter(Of MyContext) = (
  '        Function()
  '          If (_ContextAmbienceManager Is Nothing) Then
  '            _ContextAmbienceManager = New ContextAmbienceManager(Of MyContext)()
  '          End If
  '          Return _ContextAmbienceManager.GetCurrent(False)
  '        End Function
  '      )

  '    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  '    Private Shared _ContextCreationHandler As ContextCreationHandler(Of MyContext) = (
  '        Sub(context As MyContext)
  '          If (_ContextAmbienceManager Is Nothing) Then
  '            _ContextAmbienceManager = New ContextAmbienceManager(Of MyContext)()
  '          End If
  '          _ContextAmbienceManager.ThreadEntering(context)
  '        End Sub
  '      )

  '    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  '    Private Shared _ContextDisposalHandler As ContextDisposalHandler(Of MyContext) = (
  '        Sub(context As MyContext)
  '          If (_ContextAmbienceManager Is Nothing) Then
  '            _ContextAmbienceManager = New ContextAmbienceManager(Of MyContext)()
  '          End If
  '          _ContextAmbienceManager.ThreadLeaving(context)
  '        End Sub
  '      )

  '    Public Shared ReadOnly Property Current As MyContext
  '      Get
  '        Return _CurrentContextGetter.Invoke()
  '      End Get
  '    End Property

  '    Public Shared Sub HookAmbienceManagment(
  '        currentContextGetter As CurrentContextGetter(Of MyContext),
  '        contextCreationHandler As ContextCreationHandler(Of MyContext),
  '        contextDisposalHandler As ContextDisposalHandler(Of MyContext)
  '      )
  '      _CurrentContextGetter = currentContextGetter
  '      _ContextCreationHandler = contextCreationHandler
  '      _ContextDisposalHandler = contextDisposalHandler
  '    End Sub

  '#End Region

  '  End Class

#End Region

  Public Delegate Function CurrentContextGetter(Of TContext)() As TContext
  Public Delegate Sub ContextCreationHandler(Of TContext)(context As TContext)
  Public Delegate Sub ContextDisposalHandler(Of TContext)(context As TContext)

  Friend Class ContextAmbienceManager(Of TContext)

    Private _KeysByContext As New Dictionary(Of TContext, String)
    Private _CurrentPath As New AsyncLocal(Of String)

    Private Function GetKeyForContext(contextClass As TContext) As String
      SyncLock _KeysByContext
        Dim key As String = Nothing
        If (Not _KeysByContext.TryGetValue(contextClass, key)) Then
          key = Guid.NewGuid().ToString()
          _KeysByContext.Add(contextClass, key)
        End If
        Return key
      End SyncLock
    End Function

    Private Function GetContextByKey(key As String) As TContext
      SyncLock _KeysByContext
        Return _KeysByContext.Where(Function(kvp) kvp.Value = key).Select(Function(kvp) kvp.Key).FirstOrDefault()
      End SyncLock
    End Function

    Public Sub ThreadEntering(contextClass As TContext)
      Dim key = Me.GetKeyForContext(contextClass)
      If (_CurrentPath.Value Is Nothing) Then
        _CurrentPath.Value = key
      Else
        _CurrentPath.Value = _CurrentPath.Value + "/" + key
      End If
    End Sub

    Public Sub ThreadLeaving(context As TContext)
      Dim curr = _CurrentPath.Value

      If (curr Is Nothing) Then
        Exit Sub
      End If
      Dim key = Me.GetKeyForContext(context)

      If (curr = key) Then
        _CurrentPath.Value = Nothing
        Exit Sub
      End If

      If (Not curr.EndsWith("/" + key)) Then
        Throw New Exception($"Cannot pop context '{key}' since this is not the current!")
      End If
      _CurrentPath.Value = curr.Substring(0, curr.LastIndexOf("/"c))

    End Sub

    Public Function GetCurrent(thowIfNull As Boolean) As TContext
      Dim curr = _CurrentPath.Value

      If (curr Is Nothing) Then
        If (thowIfNull) Then
          Throw New Exception($"No context ({GetType(TContext).Name}) was set")
        Else
          Return Nothing
        End If
      End If

      Dim key As String
      Dim idx = curr.LastIndexOf("/"c)
      If (idx = -1) Then
        key = curr
      Else
        Dim pos = idx + 1
        key = curr.Substring(pos, curr.Length - pos)
      End If

      Return Me.GetContextByKey(key)
    End Function

  End Class

End Namespace

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Threading.Tasks

Namespace System.Threading

  Friend Class CallTreeTracker
    Implements IDisposable

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _AsyncLocal As AsyncLocal(Of Guid)

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _RootNode As ThreadNode = Nothing

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _OnLifetimeEnded As Action = Nothing

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _TerminationPhase As Boolean = False

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _TrackingFilter As Func(Of Boolean, Boolean)

    ''' <summary>
    ''' </summary>
    ''' <param name="onLifetimeEnded"></param>
    ''' <param name="trackingFilter">
    '''   is called at the start of a new child-thread and gives the chance to decide 
    '''   if the new thread sould be tracked or not
    '''   (the boolean param informs about the tracking of the parent thread)
    ''' </param>
    Public Sub New(onLifetimeEnded As Action, Optional trackingFilter As Func(Of Boolean, Boolean) = Nothing)

      Task.Run(
        Sub()
          Thread.CurrentThread.Priority = ThreadPriority.Lowest
          Do Until _TerminationPhase
            Thread.Yield()
          Loop
          Do While Me.HasActiveChildThreads
            Thread.Sleep(50)
          Loop
          _OnLifetimeEnded.Invoke()
        End Sub
      )

      _OnLifetimeEnded = onLifetimeEnded
      _TrackingFilter = trackingFilter
      _AsyncLocal = New AsyncLocal(Of Guid)(AddressOf Me.OnAsyncLocalChanged)
      _RootNode = New ThreadNode()
      _AsyncLocal.Value = _RootNode.NodeUid

    End Sub

    Private Sub OnAsyncLocalChanged(args As AsyncLocalValueChangedArgs(Of Guid))

      If (Not args.ThreadContextChanged) Then
        Exit Sub
      End If

      If (args.PreviousValue = Guid.Empty) Then

        If (_TerminationPhase AndAlso args.CurrentValue = _RootNode.NodeUid) Then
          'dont track new child-threads from the root, after the root closure has been left
          '(_AwaitingEndOfLifeTime will indicate this)
          Exit Sub
        End If

        Dim parentNode = _RootNode.FindNodeById(args.CurrentValue)

        Dim tracked = True
        If (_TrackingFilter IsNot Nothing) Then

          If (parentNode IsNot Nothing) Then
            tracked = _TrackingFilter.Invoke(parentNode.Tracked)
          Else
            tracked = _TrackingFilter.Invoke(False)
          End If
        End If

        Dim newNodeForCurrentSubthread As New ThreadNode(parentNode, tracked)
        If (parentNode IsNot Nothing) Then
          SyncLock parentNode.Childs
            parentNode.Childs.Add(newNodeForCurrentSubthread)
          End SyncLock
        End If

        _AsyncLocal.Value = newNodeForCurrentSubthread.NodeUid

      End If

      'LEAVING THREAD
      If (args.CurrentValue = Guid.Empty) Then

        Dim nodeForCurrentSubthread = _RootNode.FindNodeById(args.PreviousValue)
        If (nodeForCurrentSubthread IsNot Nothing) Then
          nodeForCurrentSubthread.HasTerminated = True
        End If

      End If

    End Sub

    Public ReadOnly Property HasActiveChildThreads As Boolean
      Get
        Dim allTerminated = _RootNode.CrawlChildsAndEnsurePredicate(
          Function(node) node.Tracked = False OrElse node.HasTerminated, True
        )
        Return (Not allTerminated)
      End Get
    End Property

    Public ReadOnly Property RootNode As ThreadNode
      Get
        Return _RootNode
      End Get
    End Property

    Public ReadOnly Property TerminationPhase As Boolean
      Get
        Return _TerminationPhase
      End Get
    End Property

    Public Function FindNodeForCurrentThread() As ThreadNode
      Return _RootNode.FindNodeByManagedThreadId(Thread.CurrentThread.ManagedThreadId)
    End Function

    Public Sub EnterTerminationPhase() Implements IDisposable.Dispose

      'a bit of time for the first child-thread to start running -> this
      'prevents from problems when disposing the manager immediately after starting
      Thread.Sleep(50)

      _TerminationPhase = True

    End Sub

    <DebuggerDisplay("Thread {ManagedThreadId} ({NodeUid})")>
    Public Class ThreadNode

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Public ReadOnly Property NodeUid As Guid = Guid.NewGuid()

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Public ReadOnly Property ManagedThreadId As Integer = Thread.CurrentThread.ManagedThreadId

      <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
      Public ReadOnly Property Childs As New List(Of ThreadNode)

      Public Property HasTerminated As Boolean = False

      ''' <summary>
      ''' if false, the node is disregarded when waiting for running threads
      ''' </summary>
      ''' <returns></returns>
      Public ReadOnly Property Tracked As Boolean

      Public Function CrawlChildsAndEnsurePredicate(callback As Func(Of ThreadNode, Boolean), recurse As Boolean) As Boolean

        Dim childs As ThreadNode()
        SyncLock Me.Childs
          childs = Me.Childs.ToArray()
        End SyncLock

        For Each child In childs
          If (Not callback.Invoke(child)) Then
            Return False
          End If
          If (recurse) Then
            If (Not child.CrawlChildsAndEnsurePredicate(callback, recurse)) Then
              Return False
            End If
          End If
        Next

        Return True
      End Function

      Public Function FindNodeByManagedThreadId(managedThreadId As Integer) As ThreadNode
        If (Me.ManagedThreadId = managedThreadId) Then
          Return Me
        End If
        SyncLock Me.Childs
          For Each child In Me.Childs
            Dim foundChild As ThreadNode = child.FindNodeByManagedThreadId(managedThreadId)
            If (foundChild IsNot Nothing) Then
              Return foundChild
            End If
          Next
        End SyncLock
        Return Nothing
      End Function

      Public Function FindNodeById(tragetId As Guid) As ThreadNode
        If (Me.NodeUid = tragetId) Then
          Return Me
        End If
        SyncLock Me.Childs
          For Each child In Me.Childs
            Dim foundChild As ThreadNode = child.FindNodeById(tragetId)
            If (foundChild IsNot Nothing) Then
              Return foundChild
            End If
          Next
        End SyncLock
        Return Nothing
      End Function

      Public Sub New(Optional parentNode As ThreadNode = Nothing, Optional tracked As Boolean = True)
        Me.Tracked = tracked
        Me.ParentNode = parentNode
      End Sub

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Public Property ParentNode As ThreadNode

    End Class

  End Class

End Namespace

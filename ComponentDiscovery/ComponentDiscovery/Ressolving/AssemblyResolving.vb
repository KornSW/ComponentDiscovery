'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Reflection

Public Class AssemblyResolving

  Private Sub New()
  End Sub

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private Shared _IsInitialized As Boolean = False

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private Shared _ResolvePaths As New List(Of String)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private Shared _ResolvePathSubscribers As New List(Of Action(Of String))

  Public Shared Sub Initialize()

    If (_IsInitialized) Then
      Exit Sub
    Else
      _IsInitialized = True
    End If

    AssemblyResolving.EnableAppDomainHandler()

    AssemblyResolving.AddResolvePath(BaseDirectory)

    For Each entry In ConfiguredResolvePaths
      AssemblyResolving.AddResolvePath(entry)
    Next

  End Sub

  Public Shared ReadOnly Property BaseDirectory As String
    Get
      Return AppDomain.CurrentDomain.BaseDirectory
    End Get
  End Property

  ''' <summary>YOU CAN
  ''' specify a rooted path OR
  ''' use '.\AnyFolder' for a path which is relative to the BaseDirectory of the AppDomain OR
  ''' use '#\AnyFolder' for a path which is relative to the location of the 'EntryAssembly' OR
  ''' use '!\AnyFolder' for a path which is relative to the location of the ComponentDiscovery.dll
  ''' </summary>
  ''' <param name="folder"></param>
  Public Shared Sub AddResolvePath(folder As String)
    AssemblyResolving.AddResolvePath(folder, String.Empty)
  End Sub

  ''' <summary>YOU CAN
  ''' specify a rooted path OR
  ''' use '.\AnyFolder' for a path which is relative to the BaseDirectory of the AppDomain OR
  ''' use '#\AnyFolder' for a path which is relative to the location of the 'EntryAssembly' OR
  ''' use '!\AnyFolder' for a path which is relative to the location of the ComponentDiscovery.dll
  ''' </summary>
  ''' <param name="folder"></param>
  Public Shared Sub AddResolvePath(folder As String, relativeTo As Assembly)
    AssemblyResolving.AddResolvePath(folder, Path.GetDirectoryName(relativeTo.Location))
  End Sub

  ''' <summary>YOU CAN
  ''' specify a rooted path OR
  ''' use '.\AnyFolder' for a path which is relative to the BaseDirectory of the AppDomain OR
  ''' use '#\AnyFolder' for a path which is relative to the location of the 'EntryAssembly' OR
  ''' use '!\AnyFolder' for a path which is relative to the location of the ComponentDiscovery.dll
  ''' </summary>
  ''' <param name="folder"></param>
  Public Shared Sub AddResolvePath(folder As String, relativeTo As String)
    AssemblyResolving.Initialize()

    If (String.IsNullOrWhiteSpace(folder)) Then
      Exit Sub
    End If

    folder = AssemblyResolving.NormalizePath(folder, relativeTo)

    If (Not _ResolvePaths.Contains(folder)) Then
      SyncLock _ResolvePaths
        _ResolvePaths.Add(folder)
      End SyncLock
      AssemblyResolving.NotifySubscribers(folder)
    End If

  End Sub

  Public Shared ReadOnly Property ResolvePaths As String()
    Get
      SyncLock _ResolvePaths
        Return _ResolvePaths.ToArray()
      End SyncLock
    End Get
  End Property

  Public Shared ReadOnly Property ConfiguredResolvePaths As String()
    Get
      Dim paths As New List(Of String)
      For Each folder In My.Settings.AssemblyResolvePaths.Split(";"c)
        If (Not String.IsNullOrWhiteSpace(folder)) Then
          paths.Add(AssemblyResolving.NormalizePath(folder, Nothing))
        End If
      Next
      Return paths.ToArray()
    End Get
  End Property

#Region " Relative Paths and Placeholders "

  Private Shared Function NormalizePath(folder As String, relativeTo As String) As String

    folder = AssemblyResolving.ResolveRootPlaceholder(folder)

    If (Path.IsPathRooted(folder)) Then
      Return Path.GetFullPath(folder).ToLower()

    ElseIf (String.IsNullOrWhiteSpace(relativeTo)) Then
      Return Path.GetFullPath(Path.Combine(AssemblyResolving.BaseDirectory, folder)).ToLower()

    Else
      relativeTo = AssemblyResolving.ResolveRootPlaceholder(relativeTo)
      Return Path.GetFullPath(Path.Combine(relativeTo, folder)).ToLower()

    End If

  End Function

  Private Shared Function ResolveRootPlaceholder(folder As String) As String

    If (folder.StartsWith("#")) Then
      Dim ea = Assembly.GetEntryAssembly()
      If (ea IsNot Nothing) Then
        Return Path.GetFullPath(Path.Combine(ea.Location, "." + Path.DirectorySeparatorChar + folder.Substring(1)))
      End If
      Return Path.GetFullPath("." + Path.DirectorySeparatorChar + folder.Substring(1))

    ElseIf (folder.StartsWith("!")) Then
      Dim ea = Assembly.GetExecutingAssembly()
      Return Path.GetFullPath(Path.Combine(ea.Location, "." + Path.DirectorySeparatorChar + folder.Substring(1)))

    End If

    Return folder
  End Function

#End Region

#Region " Subscription "

  Private Shared Sub NotifySubscribers(newAddedFolder As String)
    AssemblyResolving.Initialize()

    SyncLock _ResolvePathSubscribers
      For Each subscriber In _ResolvePathSubscribers
        subscriber.Invoke(newAddedFolder)
      Next
    End SyncLock
  End Sub

  Public Shared Sub SubscribeForNewResolvePathAdded(subscriber As Action(Of String))
    AssemblyResolving.Initialize()

    SyncLock _ResolvePathSubscribers
      If (_ResolvePathSubscribers.Contains(subscriber)) Then
        Exit Sub
      End If
      _ResolvePathSubscribers.Add(subscriber)
    End SyncLock

    SyncLock _ResolvePaths
      For Each alreadyKnownPath In _ResolvePaths
        subscriber.Invoke(alreadyKnownPath)
      Next
    End SyncLock

  End Sub

  Public Shared Sub UnsubscribeFromNewResolvePathAdded(subscriber As Action(Of String))

    SyncLock _ResolvePathSubscribers
      If (_ResolvePathSubscribers.Contains(subscriber)) Then
        _ResolvePathSubscribers.Remove(subscriber)
      End If
    End SyncLock

  End Sub

#End Region

#Region " AppDomain Handler (Fusion) "

  Private Shared Sub EnableAppDomainHandler()
    AddHandler AppDomain.CurrentDomain.AssemblyResolve, AddressOf AssemblyResolving.AppDomain_AssemblyResolve
  End Sub

  Private Shared Function AppDomain_AssemblyResolve(sender As Object, e As ResolveEventArgs) As Assembly

    Dim assemblyName = New AssemblyName(e.Name)
    Dim assemblyFileName = assemblyName.Name + ".dll"
    Dim assemblyFilePath As String
    Dim assemblyFileInfo As FileInfo

    For Each resolvePath In AssemblyResolving.ResolvePaths
      assemblyFilePath = Path.Combine(resolvePath, assemblyFileName)
      assemblyFileInfo = New FileInfo(assemblyFilePath)
      If ((assemblyFileInfo.Exists) AndAlso (assemblyFileInfo.Length > 0)) Then
        Try
          Return Assembly.LoadFrom(assemblyFilePath)
        Catch
        End Try
      End If
    Next

    Return Nothing
  End Function

#End Region

End Class

'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Reflection

Public Class AssemblyIndexer
  Implements IAssemblyIndexer

#Region " Fields & Constructor "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _PreferAssemblyLoadingViaFusion As Boolean

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AppDomainBindingIsEnabled As Boolean = False

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AutoImportFromResolvePathsEnabled As Boolean = False

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _RecursiveReferencesIncluded As Boolean = False

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _DismissedAssemblies As New List(Of String)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _ApprovedAssemblies As New List(Of Assembly)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _CurrentlyApprovingAssemblies As New List(Of String)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _OnAssemblyApprovedMethods As New List(Of Action(Of Assembly))

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _FullyImportedDirectories As New List(Of String)

  Public Sub New()
    MyClass.New(True, True, True)
  End Sub

  Public Sub New(enableResolvePathsBinding As Boolean, enableAppDomainBinding As Boolean, Optional preferAssemblyLoadingViaFusion As Boolean = True)

    _PreferAssemblyLoadingViaFusion = preferAssemblyLoadingViaFusion

    'this triggers the registration of the appdomain resolve handlers!!!
    AssemblyResolving.Initialize()

    'this triggers assembly-add
    Me.AppDomainBindingEnabled = enableAppDomainBinding

    'this triggers assembly-add
    Me.ResolvePathsBindingEnabled = enableResolvePathsBinding

  End Sub

#End Region

#Region " Adding of Assemblies (manual)  "

  Protected Overridable Function GetPrimaryApplicationAssemblyDirectory() As DirectoryInfo
    Return New DirectoryInfo(AssemblyResolving.PrimaryApplicationAssemblyDirectory)
  End Function

  Public Sub TryApproveAssemblyFilesFromApplicationAssemblyDirectory(
    recursive As Boolean,
    Optional pattern As String = "*.dll|*.exe",
    Optional forceReapprove As Boolean = False
  )

    Me.TryApproveAssemblyFilesFrom(Me.GetPrimaryApplicationAssemblyDirectory, recursive, pattern, forceReapprove)
  End Sub

  Public Sub TryApproveAssemblyFilesFrom(
    directoryInfo As DirectoryInfo,
    recursive As Boolean,
    Optional searchPattern As String = "*.dll|*.exe",
    Optional forceReapprove As Boolean = False
  )

    searchPattern = searchPattern.ToLower()

    If (directoryInfo.Exists) Then
      SyncLock _FullyImportedDirectories
        _FullyImportedDirectories.Add(directoryInfo.FullName)
      End SyncLock
    Else
      Exit Sub
    End If

    Diag.Verbose(Function() String.Format("AssemblyIndexer: adding assembly files '{0}\{1}' ...", directoryInfo.FullName.ToLower(), searchPattern))

    If (String.IsNullOrWhiteSpace(searchPattern)) Then
      searchPattern = "*.dll|*.exe"
    End If

    For Each token In searchPattern.Split("|"c)
      For Each fileInfo In directoryInfo.GetFiles(token)
        Me.TryApproveAssemblyFile(fileInfo, forceReapprove)
      Next
    Next

    If (recursive) Then
      For Each subDirectoryInfo In directoryInfo.GetDirectories()
        Try
          Me.TryApproveAssemblyFilesFrom(subDirectoryInfo, recursive, searchPattern)
        Catch ' Occurs when filesystem permissions are missing
        End Try
      Next
    End If

  End Sub

  Protected ReadOnly Property FullyImportedDirectories As String()
    Get
      SyncLock _FullyImportedDirectories
        Return _FullyImportedDirectories.ToArray()
      End SyncLock
    End Get
  End Property

  Public Function TryApproveAssemblyFile(fileInfo As FileInfo, Optional forceReapprove As Boolean = False) As Boolean Implements IAssemblyIndexer.TryApproveAssemblyFile
    Try
      Return Me.TryApproveAssembly(fileInfo.FullName, forceReapprove)
    Catch
    End Try
    Return False
  End Function

  Public Function TryApproveAssemblyFile(assemblyFullFilename As String) As Boolean Implements IAssemblyIndexer.TryApproveAssemblyFile
    Return Me.TryApproveAssembly(assemblyFullFilename, False)
  End Function

  Public Function TryApproveCurrentAssembly() As Boolean Implements IAssemblyIndexer.TryApproveCurrentAssembly
    Return Me.TryApproveAssembly(Assembly.GetCallingAssembly())
  End Function

  Public Overridable Function TryApproveAssembly(assembly As Assembly) As Boolean Implements IAssemblyIndexer.TryApproveAssembly
    Return Me.TryApproveAssembly(assembly.Location, False)
  End Function

  Protected Function TryApproveAssembly(assemblyFileFullName As String, forceReapprove As Boolean) As Boolean
    assemblyFileFullName = assemblyFileFullName.ToLower()
    Dim fileName As String = Path.GetFileNameWithoutExtension(assemblyFileFullName)

    If (Me.IsAssemblyAlreadyIndexed(assemblyFileFullName)) Then
      Return True
    End If

    If (_CurrentlyApprovingAssemblies.Contains(assemblyFileFullName)) Then
      Return False
    End If

    If (_DismissedAssemblies.Contains(assemblyFileFullName)) Then
      If (forceReapprove) Then
        _DismissedAssemblies.Remove(assemblyFileFullName)
      Else
        Return False
      End If
    End If

    ' We need to do this because this method needs to be absolutely thread-safe
    _CurrentlyApprovingAssemblies.Add(assemblyFileFullName)
    Try

      Diag.Verbose(Function() $"AssemblyIndexer: approving incomming assembly '{fileName}'...")

      If (Me.VerifyAssembly(assemblyFileFullName, False)) Then
        Diag.Verbose(Function() $"AssemblyIndexer: incomming assembly '{fileName}' was APPROVED and will be added to index!")
        Return Me.LoadAndAdd(assemblyFileFullName)

      Else
        Diag.Verbose(Function() $"AssemblyIndexer: incomming assembly '{fileName}' was REPRESSED and will not be added to index!")
        _DismissedAssemblies.Add(assemblyFileFullName)

      End If

    Finally
      _CurrentlyApprovingAssemblies.Remove(assemblyFileFullName)
    End Try

    Return False
  End Function

  Protected Function IsAssemblyAlreadyProcessed(assemblyFullFilename As String) As Boolean
    If (_DismissedAssemblies.Contains(assemblyFullFilename)) Then
      Return True
    End If
    Return Me.IsAssemblyAlreadyIndexed(assemblyFullFilename)
  End Function

  Protected Function IsAssemblyAlreadyIndexed(assemblyFullFilename As String) As Boolean
    assemblyFullFilename = assemblyFullFilename.ToLower()
    For Each ia In _ApprovedAssemblies
      If (ia.Location.ToLower() = assemblyFullFilename) Then
        Return True
      End If
    Next
    Return False
  End Function

  Private Function LoadAndAdd(assemblyFullFilename As String) As Boolean
    Dim ass As Assembly = Nothing

    Try

      If (_PreferAssemblyLoadingViaFusion) Then
        Try
          'load via Fusion!!!
          ass = Assembly.Load(Path.GetFileNameWithoutExtension(assemblyFullFilename))
        Catch ex As FileNotFoundException
          ass = Nothing
        End Try
      End If

      If (ass Is Nothing) Then
        ass = Assembly.LoadFile(assemblyFullFilename)
      End If

    Catch ex As Exception
      Dim fileName As String = Path.GetFileNameWithoutExtension(assemblyFullFilename)

      Diag.Warning(
        String.Format(
          "AssemblyIndexer: assembly '{0}' could not be added to index because because 'Assembly.LoadFile(""{1}"")' caused the following exception: {2}",
          fileName, assemblyFullFilename, ex.Message
        )
      )

      Return False
    End Try

    _ApprovedAssemblies.Add(ass)

    Me.OnAssemblyApproved(ass)

    Return True
  End Function

#End Region

#Region " Adding of Assemblies (automatic via appdomain event) "

  Public Property AppDomainBindingEnabled() As Boolean
    Get
      Return _AppDomainBindingIsEnabled
    End Get
    Set(value As Boolean)
      If (Not _AppDomainBindingIsEnabled = value) Then
        _AppDomainBindingIsEnabled = value
        If (_AppDomainBindingIsEnabled) Then

          AddHandler AppDomain.CurrentDomain.AssemblyLoad, AddressOf Me.AppDomain_AssemblyLoad
          Diag.Info("AssemblyIndexer: Appdomain subscription ENABLED")

          For Each assembly In AppDomain.CurrentDomain.GetAssemblies()
            If (Not assembly.IsDynamic) Then

              Diag.Verbose(Function() $"AssemblyIndexer: assembly '{assembly.GetName().Name}' received over appdomain subscription")
              Me.TryApproveAssembly(assembly)

            End If
          Next

        Else
          RemoveHandler AppDomain.CurrentDomain.AssemblyLoad, AddressOf Me.AppDomain_AssemblyLoad
          Diag.Info("AssemblyIndexer: appdomain subscription DISABLED")

        End If
      End If
    End Set
  End Property

  Private Sub AppDomain_AssemblyLoad(sender As Object, args As AssemblyLoadEventArgs)
    If (Me.AppDomainBindingEnabled) Then
      If (Not args.LoadedAssembly.IsDynamic) Then

        Diag.Verbose(Function() $"AssemblyIndexer: assembly '{args.LoadedAssembly.GetName().Name}' received over appdomain subscription")
        Me.TryApproveAssembly(args.LoadedAssembly)

      End If
    End If
  End Sub

#End Region

#Region " Adding of Assemblies (automatic on new resolve path) "

  Public Property ResolvePathsBindingEnabled() As Boolean
    Get
      Return _AutoImportFromResolvePathsEnabled
    End Get
    Set(value As Boolean)

      If (_AutoImportFromResolvePathsEnabled = True AndAlso value = False) Then
        AssemblyResolving.UnsubscribeFromNewResolvePathAdded(AddressOf OnNewResolvePathAdded)

      ElseIf (_AutoImportFromResolvePathsEnabled = False AndAlso value = True) Then
        AssemblyResolving.SubscribeForNewResolvePathAdded(AddressOf OnNewResolvePathAdded)

      End If

      _AutoImportFromResolvePathsEnabled = value

    End Set
  End Property

  Private Sub OnNewResolvePathAdded(newResolvePath As String)
    Me.TryApproveAssemblyFilesFrom(New DirectoryInfo(newResolvePath), False, "*.dll")
  End Sub

#End Region

#Region " Reference based recursion "

  Public Sub IncludeRecursiveReferences()
    If (Not _RecursiveReferencesIncluded) Then
      _RecursiveReferencesIncluded = True
      For Each alreadyApprovedAssembly In Me.ApprovedAssemblies
        Diag.Verbose(Function() $"AssemblyIndexer: adding references of '{ alreadyApprovedAssembly.GetName().Name}'... (recursion is enabled)")
        Me.AddAvaliableAssembliesReferencedBy(alreadyApprovedAssembly)
      Next
    End If
  End Sub

  Public ReadOnly Property RecursiveReferencesIncluded As Boolean
    Get
      Return _RecursiveReferencesIncluded
    End Get
  End Property

  Protected Overridable Sub OnAssemblyApproved(assembly As Assembly)
    Me.NotifySubscribers(assembly)
    If (_RecursiveReferencesIncluded) Then
      Diag.Verbose(Function() $"AssemblyIndexer: adding references of '{assembly.GetName().Name}'... (recursion is enabled)")
      Me.AddAvaliableAssembliesReferencedBy(assembly)
    End If
  End Sub

  Protected Sub AddAvaliableAssembliesReferencedBy(assembly As Assembly)
    Try
      Dim referencedAssemblyNames = assembly.GetReferencedAssemblies()
      For Each referencedAssemblyName In referencedAssemblyNames
        Dim referencedAssembly As Assembly = Nothing
        Try
          referencedAssembly = Assembly.Load(referencedAssemblyName)
        Catch
        End Try
        If (referencedAssembly IsNot Nothing) Then
          Me.TryApproveAssembly(referencedAssembly)
        End If
      Next
    Catch
    End Try
  End Sub

#End Region

#Region " Approve Assemblies "

  Protected Overridable Function VerifyAssembly(assemblyFullFilename As String, isReapprove As Boolean) As Boolean
    Return True
  End Function

  Public Overridable Sub ReapproveDismissedAssemblies()

    Diag.Info($"'{NameOf(ReapproveDismissedAssemblies)}' was triggered...")

    For Each suppressedAssembly In _DismissedAssemblies.ToArray()
      Dim assFileName As String = Path.GetFileNameWithoutExtension(suppressedAssembly)

      If (Not _CurrentlyApprovingAssemblies.Contains(suppressedAssembly)) Then
        'we need to do this because this method needs to be absolute thread-safe
        _CurrentlyApprovingAssemblies.Add(suppressedAssembly)
        Try
          Diag.Verbose(Function() $"AssemblyIndexer: re-approving repressed assembly '{assFileName}'...")

          If (Me.VerifyAssembly(suppressedAssembly, True)) Then
            Diag.Verbose(Function() $"AssemblyIndexer: repressed assembly '{assFileName}' was retroactively APPROVED and will be added to index!")

            If (Me.LoadAndAdd(suppressedAssembly)) Then

              _DismissedAssemblies.Remove(suppressedAssembly)

            End If

          End If

        Finally
          _CurrentlyApprovingAssemblies.Remove(suppressedAssembly)
        End Try
      End If

    Next
  End Sub

#End Region

#Region " Publication "

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public ReadOnly Property PreferAssemblyLoadingViaFusion As Boolean
    Get
      Return _PreferAssemblyLoadingViaFusion
    End Get
  End Property

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public ReadOnly Property DismissedAssemblies As String() Implements IAssemblyIndexer.DismissedAssemblies
    Get
      Return _DismissedAssemblies.ToArray()
    End Get
  End Property

  Public ReadOnly Property ApprovedAssemblies As Assembly() Implements IAssemblyIndexer.ApprovedAssemblies
    Get
      Return _ApprovedAssemblies.ToArray()
    End Get
  End Property

  Public Sub SubscribeForAssemblyApproved(onAssemblyApprovedMethod As Action(Of Assembly)) Implements IAssemblyIndexer.SubscribeForAssemblyApproved

    If (_OnAssemblyApprovedMethods.Contains(onAssemblyApprovedMethod)) Then
      Exit Sub
    End If

    _OnAssemblyApprovedMethods.Add(onAssemblyApprovedMethod)

    For Each approvedAssembly In _ApprovedAssemblies.ToArray()
      onAssemblyApprovedMethod.Invoke(approvedAssembly)
    Next

  End Sub

  Protected Sub NotifySubscribers(approvedAssembly As Assembly)
    For Each onAssemblyApprovedMethod In _OnAssemblyApprovedMethods
      onAssemblyApprovedMethod.Invoke(approvedAssembly)
    Next
  End Sub

  Public Sub UnsubscribeFromAssemblyApproved(onAssemblyApprovedMethod As Action(Of Assembly)) Implements IAssemblyIndexer.UnsubscribeFromAssemblyApproved

    If (_OnAssemblyApprovedMethods.Contains(onAssemblyApprovedMethod)) Then
      _OnAssemblyApprovedMethods.Remove(onAssemblyApprovedMethod)
    End If

  End Sub

#End Region

#Region " IDisposable "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AlreadyDisposed As Boolean = False

  ''' <summary>
  '''   Dispose the current object instance
  ''' </summary>
  Protected Overridable Sub Dispose(disposing As Boolean)
    If (Not _AlreadyDisposed) Then
      If (disposing) Then
        Me.AppDomainBindingEnabled = False
        Me.ResolvePathsBindingEnabled = False
      End If
      _AlreadyDisposed = True
    End If
  End Sub

  ''' <summary>
  '''   Dispose the current object instance and suppress the finalizer
  ''' </summary>
  Public Sub Dispose() Implements IDisposable.Dispose
    Me.Dispose(True)
    GC.SuppressFinalize(Me)
  End Sub

#End Region

End Class

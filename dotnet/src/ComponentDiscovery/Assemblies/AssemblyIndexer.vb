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
Imports System.Text

Namespace ComponentDiscovery

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
          If (Not _FullyImportedDirectories.Contains(directoryInfo.FullName)) Then
            _FullyImportedDirectories.Add(directoryInfo.FullName)
          End If
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
      Catch ex As Exception
        Diag.Error(ex)
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

    ''' <summary>
    ''' In addition to protection against concurrency issues, we also need explicit protection against recursion.
    ''' In the special case that AppDomainBinding is enabled and TryApproveAssembly is called from the AppDomain_AssemblyLoad event,
    ''' it can happen that the assembly currently being loaded cannot be activated via Fusion
    ''' (e.g., due to an exception when loading a related satellite assembly), which then causes LoadAndAdd to attempt to load
    ''' the assembly via FileName! In this case, however, the framework does not recognize that it is already loading this assembly
    ''' itself and triggers the assemblyload event again. Fortunately, this latter event occurs synchronously, so we can detect it
    ''' and simply have to "endure" this inner event! BUT waiting (which is important for a multithreading scenario) would freeze
    ''' the thread permanently in this case...
    ''' </summary>
    <ThreadStatic>
    Private Shared _CurrentlyLoadingAssemblyFileFullName As String = String.Empty

    Protected Function TryApproveAssembly(assemblyFileFullName As String, forceReapprove As Boolean) As Boolean
      assemblyFileFullName = assemblyFileFullName.ToLower()
      Dim fileName As String = Path.GetFileNameWithoutExtension(assemblyFileFullName)

      If (Me.IsAssemblyAlreadyIndexed(assemblyFileFullName)) Then
        Return True
      End If

      Dim needToWaitForResult As Boolean = False
      Dim waitIterations As Integer = 0
      Do
        SyncLock (_CurrentlyApprovingAssemblies)
          needToWaitForResult = _CurrentlyApprovingAssemblies.Contains(assemblyFileFullName)
        End SyncLock

        If (needToWaitForResult) Then

          If (_CurrentlyLoadingAssemblyFileFullName = assemblyFileFullName) Then
            'in the recursion scenario described above, the return value is irrelevant, as it is not evaluated...
            'but we must not wait here under any circumstances!
            Return False
          End If

          waitIterations = waitIterations + 1
          Threading.Thread.Sleep(100)
          If (waitIterations > 200) Then
            Return False 'lock detected... (after 20s)
          End If

        End If
      Loop While (needToWaitForResult)

      SyncLock _DismissedAssemblies
        If (_DismissedAssemblies.Contains(assemblyFileFullName)) Then
          If (forceReapprove) Then
            _DismissedAssemblies.Remove(assemblyFileFullName)
          Else
            Return False
          End If
        End If
      End SyncLock

      SyncLock (_CurrentlyApprovingAssemblies)
        ' We need to do this because this method needs to be absolutely thread-safe
        _CurrentlyApprovingAssemblies.Add(assemblyFileFullName)
      End SyncLock

      Try

        'set recursion-protection
        _CurrentlyLoadingAssemblyFileFullName = assemblyFileFullName

        Diag.Verbose(Function() $"AssemblyIndexer: approving incomming assembly '{fileName}'...")

        If (Me.VerifyAssembly(assemblyFileFullName, False)) Then
          Diag.Verbose(Function() $"AssemblyIndexer: incomming assembly '{fileName}' was APPROVED and will be added to index!")
          Return Me.LoadAndAdd(assemblyFileFullName)

        Else
          Diag.Verbose(Function() $"AssemblyIndexer: incomming assembly '{fileName}' was REPRESSED and will not be added to index!")
          SyncLock _DismissedAssemblies
            If (Not _DismissedAssemblies.Contains(assemblyFileFullName)) Then
              _DismissedAssemblies.Add(assemblyFileFullName)
            End If
          End SyncLock
        End If

      Finally

        SyncLock (_CurrentlyApprovingAssemblies)
          _CurrentlyApprovingAssemblies.Remove(assemblyFileFullName)
        End SyncLock

        'release recursion-protection
        _CurrentlyLoadingAssemblyFileFullName = String.Empty

      End Try

      Return False
    End Function

    Protected Function IsAssemblyAlreadyProcessed(assemblyFullFilename As String) As Boolean

      SyncLock _DismissedAssemblies
        If (_DismissedAssemblies.Contains(assemblyFullFilename)) Then
          Return True
        End If
      End SyncLock

      Return Me.IsAssemblyAlreadyIndexed(assemblyFullFilename)
    End Function

    Protected Function IsAssemblyAlreadyIndexed(assemblyFullFilename As String) As Boolean

      assemblyFullFilename = assemblyFullFilename.ToLower()

      SyncLock _ApprovedAssemblies
        For Each ia In _ApprovedAssemblies
          If (ia.Location.Equals(assemblyFullFilename, StringComparison.InvariantCultureIgnoreCase)) Then
            Return True
          End If
        Next
      End SyncLock

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

      If (ass Is Nothing) Then
        Dim fileName As String = Path.GetFileNameWithoutExtension(assemblyFullFilename)

        Diag.Warning(
          String.Format(
            "AssemblyIndexer: assembly '{0}' could not be added to index because because 'Assembly.LoadFile(""{1}"")' returned null.",
            fileName, assemblyFullFilename
          )
        )

        Return False
      End If

      SyncLock _ApprovedAssemblies
        If (Not _ApprovedAssemblies.Contains(ass)) Then
          _ApprovedAssemblies.Add(ass)
        End If
      End SyncLock

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

          'if we are not active yet, postpone the wireups until were getting active
          If (_IsLazyInitialized) Then

            If (_AppDomainBindingIsEnabled) Then
              Me.SubscribeAssembliesFromAppdomain()
            Else
              RemoveHandler AppDomain.CurrentDomain.AssemblyLoad, AddressOf Me.AppDomain_AssemblyLoad
              Diag.Info("AssemblyIndexer: appdomain subscription DISABLED")
            End If

          End If
        End If
      End Set
    End Property

    Private Sub SubscribeAssembliesFromAppdomain()

      AddHandler AppDomain.CurrentDomain.AssemblyLoad, AddressOf Me.AppDomain_AssemblyLoad
      Diag.Info("AssemblyIndexer: Appdomain subscription ENABLED")

      'import the assemblies for which were missed the AssemblyLoad-Events
      For Each assembly In AppDomain.CurrentDomain.GetAssemblies()
        If (Not assembly.IsDynamic) Then
          Diag.Verbose(Function() $"AssemblyIndexer: assembly '{assembly.GetName().Name}' received over appdomain subscription")
          Me.TryApproveAssembly(assembly)
        End If
      Next

    End Sub

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

        'if we are not active yet, postpone the wireups until were getting active
        If (_IsLazyInitialized) Then
          If (_AutoImportFromResolvePathsEnabled = True AndAlso value = False) Then
            AssemblyResolving.UnsubscribeFromNewResolvePathAdded(AddressOf OnNewResolvePathAdded)

          ElseIf (_AutoImportFromResolvePathsEnabled = False AndAlso value = True) Then
            Me.SubscribeAssembliesResolvePaths()

          End If
        End If
        _AutoImportFromResolvePathsEnabled = value
      End Set
    End Property

    Private Sub SubscribeAssembliesResolvePaths()
      AssemblyResolving.SubscribeForNewResolvePathAdded(AddressOf OnNewResolvePathAdded)
    End Sub

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

      For Each dismissedAssembly In Me.DismissedAssemblies 'liefert array snapshot
        Dim assFileName As String = Path.GetFileNameWithoutExtension(dismissedAssembly)

        Dim cascadeDetected As Boolean = False
        SyncLock _CurrentlyApprovingAssemblies
          cascadeDetected = _CurrentlyApprovingAssemblies.Contains(dismissedAssembly)
        End SyncLock

        If (Not cascadeDetected) Then

          SyncLock _CurrentlyApprovingAssemblies
            'we need to do this because this method needs to be absolutely thread-safe
            _CurrentlyApprovingAssemblies.Add(dismissedAssembly)
          End SyncLock

          Try
            Diag.Verbose(Function() $"AssemblyIndexer: re-approving repressed assembly '{assFileName}'...")

            If (Me.VerifyAssembly(dismissedAssembly, True)) Then
              Diag.Verbose(Function() $"AssemblyIndexer: repressed assembly '{assFileName}' was retroactively APPROVED and will be added to index!")

              If (Me.LoadAndAdd(dismissedAssembly)) Then

                SyncLock _DismissedAssemblies
                  _DismissedAssemblies.Remove(dismissedAssembly)
                End SyncLock

              End If

            End If

          Finally
            SyncLock _CurrentlyApprovingAssemblies
              _CurrentlyApprovingAssemblies.Remove(dismissedAssembly)
            End SyncLock
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
        SyncLock _DismissedAssemblies
          Return _DismissedAssemblies.ToArray()
        End SyncLock
      End Get
    End Property

    Public ReadOnly Property ApprovedAssemblies As Assembly() Implements IAssemblyIndexer.ApprovedAssemblies
      Get
        SyncLock _ApprovedAssemblies
          Return _ApprovedAssemblies.ToArray()
        End SyncLock
      End Get
    End Property


    Private _IsLazyInitialized As Boolean = False

    ''' <summary>
    ''' a Hook to do some self-initialzation logic (for example pulling some default clearances)
    ''' exactly at the moment when the current instance gets its first subscriber
    ''' </summary>
    Protected Overridable Sub OnLazyInitializing()
    End Sub

    Private Sub EnsureIsLazyInitialized()
      If (_IsLazyInitialized) Then
        Exit Sub
      End If
      _IsLazyInitialized = True

      'this triggers the registration of the appdomain resolve handlers!!!
      AssemblyResolving.Initialize()

      Me.OnLazyInitializing()

      If (_AutoImportFromResolvePathsEnabled) Then
        Me.SubscribeAssembliesResolvePaths()
      End If

      If (_AppDomainBindingIsEnabled) Then
        Me.SubscribeAssembliesFromAppdomain()
      End If

    End Sub

    Public Sub SubscribeForAssemblyApproved(onAssemblyApprovedMethod As Action(Of Assembly)) Implements IAssemblyIndexer.SubscribeForAssemblyApproved

      If (_OnAssemblyApprovedMethods.Contains(onAssemblyApprovedMethod)) Then
        Exit Sub
      End If

      _OnAssemblyApprovedMethods.Add(onAssemblyApprovedMethod)
      Me.EnsureIsLazyInitialized()

      For Each approvedAssembly In Me.ApprovedAssemblies
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

#Region " Diagnostics "

    ''' <summary>
    ''' Generates a Report for Diagnostics and Troubleshooting
    ''' </summary>
    Public Function DumpFullState() As String
      Dim result As New StringBuilder

      result.AppendLine($"{Me.GetType().Name}:")
      result.AppendLine()

      Me.DumpFullStateTo(result)
      Return result.ToString()
    End Function

    Protected Overridable Sub DumpFullStateTo(result As StringBuilder)

      If (_IsLazyInitialized) Then
        result.AppendLine("Initially activated: YES")
      Else
        result.AppendLine("Initially activated: NO")
      End If
      result.AppendLine()

      Dim indent As Integer = 20
      For Each a In Me.DismissedAssemblies
        If (a.Length > indent) Then
          indent = a.Length
        End If
      Next
      For Each a In Me.ApprovedAssemblies
        If (a.Location.Length > indent) Then
          indent = a.Location.Length
        End If
      Next
      indent += 4

      result.AppendLine("#### FULLY IMPORTED DIRECTORIES ###")
      For Each p In Me.FullyImportedDirectories
        result.AppendLine(p)
      Next
      result.AppendLine()

      result.AppendLine("#### APPROVED ASSEMBLIES ###")
      For Each a In Me.ApprovedAssemblies
        result.Append(a.Location)
        result.Append(New String(" "c, indent - a.Location.Length))
      Next
      result.AppendLine()

      result.AppendLine("#### DISMISSED ASSEMBLIES ###")
      For Each a In Me.DismissedAssemblies
        result.Append(a)
        result.Append(New String(" "c, indent - a.Length))
      Next
      result.AppendLine()

      Me.DumpBaseConfigTo(result)
    End Sub

    Protected Sub DumpBaseConfigTo(result As StringBuilder)
      result.AppendLine("#### CONFIGURATION ###")
      If (_PreferAssemblyLoadingViaFusion) Then
        result.AppendLine("PreferAssemblyLoadingViaFusion: ENABLED")
      Else
        result.AppendLine("PreferAssemblyLoadingViaFusion: disabled")
      End If
      If (_AppDomainBindingIsEnabled) Then
        result.AppendLine("AppDomainBinding: ENABLED")
      Else
        result.AppendLine("AppDomainBinding: disabled")
      End If
      If (_AutoImportFromResolvePathsEnabled) Then
        result.AppendLine("AutoImportFromResolvePaths: ENABLED")
      Else
        result.AppendLine("AutoImportFromResolvePaths: disabled")
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

End Namespace

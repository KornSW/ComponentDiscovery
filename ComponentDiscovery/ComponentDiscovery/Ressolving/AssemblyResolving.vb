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

    'DEFAULTS:

    If (Assembly.GetEntryAssembly() IsNot Nothing) Then
      AssemblyResolving.AddResolvePath(Path.GetDirectoryName(Assembly.GetEntryAssembly.Location))
    End If

    If (System.Web.HttpRuntime.AppDomainId IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(System.Web.HttpRuntime.BinDirectory)) Then
      AssemblyResolving.AddResolvePath(System.Web.HttpRuntime.BinDirectory) '(not 'HttpRuntime.AppDomainAppPath'!!!)
    End If

    AssemblyResolving.AddResolvePath(AppDomain.CurrentDomain.BaseDirectory)

    If (AppDomain.CurrentDomain.RelativeSearchPath IsNot Nothing) Then
      AssemblyResolving.AddResolvePath(AppDomain.CurrentDomain.RelativeSearchPath)
    End If

    For Each entry In ConfiguredResolvePaths
      AssemblyResolving.AddResolvePath(entry, AssemblyResolvingFixpoint.LocationOfAppdomainConfigFile)
    Next

  End Sub

  ''' <summary>
  ''' The primary fixpoint when resolving relative paths.
  ''' Note: when running windows applications the 'PrimaryApplicationAssemblyDirectory' and the 'PrimaryApplicationRootDirectory' are equal - 
  ''' BUT NOT UNDER ASP.NET!!!
  ''' </summary>
  Public Shared ReadOnly Property PrimaryApplicationRootDirectory As String
    Get
      If (Assembly.GetEntryAssembly() IsNot Nothing) Then
        Return Path.GetDirectoryName(Assembly.GetEntryAssembly.Location)

      ElseIf (System.Web.HttpRuntime.AppDomainId IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(System.Web.HttpRuntime.AppDomainAppPath)) Then
        Return System.Web.HttpRuntime.AppDomainAppPath '(not 'HttpRuntime.BinDirectory'!!!)

      Else
        Return AppDomain.CurrentDomain.BaseDirectory

      End If
    End Get
  End Property

  ''' <summary>
  ''' The primary location of application binaries.
  ''' Note: when running windows applications the 'PrimaryApplicationAssemblyDirectory' and the 'PrimaryApplicationRootDirectory' are equal - 
  ''' BUT NOT UNDER ASP.NET!!!
  ''' </summary>
  Public Shared ReadOnly Property PrimaryApplicationAssemblyDirectory As String
    Get
      If (Assembly.GetEntryAssembly() IsNot Nothing) Then
        Return Path.GetDirectoryName(Assembly.GetEntryAssembly.Location)

      ElseIf (System.Web.HttpRuntime.AppDomainId IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(System.Web.HttpRuntime.AppDomainAppPath)) Then
        Return System.Web.HttpRuntime.BinDirectory '(not 'HttpRuntime.AppDomainAppPath'!!!)

      Else
        Return AppDomain.CurrentDomain.BaseDirectory

      End If
    End Get
  End Property

  Public Shared Sub AddResolvePath(fullOrRelativePath As String, Optional relativeTo As AssemblyResolvingFixpoint = AssemblyResolvingFixpoint.PrimaryApplicationDirectory)
    Select Case relativeTo

      Case AssemblyResolvingFixpoint.LocationOfCallingAssembly
        AssemblyResolving.AddResolvePath(fullOrRelativePath, Assembly.GetCallingAssembly())

      Case AssemblyResolvingFixpoint.LocationOfAssemblyResolving
        AssemblyResolving.AddResolvePath(fullOrRelativePath, Assembly.GetExecutingAssembly())

      Case AssemblyResolvingFixpoint.LocationOfAppdomainConfigFile
        Dim configFilePath As String = Nothing
        If (AppDomain.CurrentDomain.SetupInformation?.ConfigurationFile IsNot Nothing) Then
          configFilePath = Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile)
        End If
        AssemblyResolving.AddResolvePath(fullOrRelativePath, configFilePath)

      Case Else 'AssemblyResolvingFixpoint.PrimaryApplicationDirectory
        AssemblyResolving.AddResolvePath(fullOrRelativePath, PrimaryApplicationRootDirectory)

    End Select
  End Sub

  Public Shared Sub AddResolvePath(fullOrRelativePath As String, relativeTo As Assembly)
    AssemblyResolving.AddResolvePath(fullOrRelativePath, Path.GetDirectoryName(relativeTo.Location))
  End Sub

  Private Shared Sub AddResolvePath(fullOrRelativePath As String, relativeTo As String)
    AssemblyResolving.Initialize()

    If (String.IsNullOrWhiteSpace(fullOrRelativePath)) Then
      Exit Sub
    End If

    If (String.IsNullOrWhiteSpace(relativeTo)) Then
      relativeTo = PrimaryApplicationRootDirectory
    End If

    Dim normalizedFullPath = AssemblyResolving.NormalizePath(fullOrRelativePath, relativeTo)

    Dim infoString As String
    If (Path.IsPathRooted(fullOrRelativePath)) Then
      If (fullOrRelativePath.ToLower() = normalizedFullPath) Then
        infoString = $"ResolvePath '{normalizedFullPath}'"
      Else
        infoString = $"ResolvePath '{normalizedFullPath}' ('{fullOrRelativePath}')"
      End If
    Else
      infoString = $"ResolvePath '{normalizedFullPath}' ('{fullOrRelativePath}' on fixpoint '{relativeTo}')"
    End If

    If (Not _ResolvePaths.Contains(normalizedFullPath)) Then
      SyncLock _ResolvePaths
        Diag.Info($"AssemblyResolving: adding {infoString}...")

        If (Directory.Exists(normalizedFullPath)) Then
          _ResolvePaths.Add(normalizedFullPath)
          AssemblyResolving.NotifySubscribers(normalizedFullPath)

        Else
          Diag.Error($"AssemblyResolving: {infoString} does not exist!")
        End If

      End SyncLock
    End If

  End Sub

  ''' <summary>
  ''' returns a rooted full path from the given 'fullOrRelativePath' which will be evaluated using the 'PrimaryApplicationDirectory' as fixpoint
  ''' </summary>
  ''' <param name="fullOrRelativePath"></param>
  ''' <returns></returns>
  Public Shared Function ResolveRelativePath(fullOrRelativePath As String) As String
    AssemblyResolving.Initialize()
    Return AssemblyResolving.NormalizePath(fullOrRelativePath, PrimaryApplicationRootDirectory)
  End Function

  ''' <summary>
  ''' Please dont use this property to build your own resolver! If the AssemblyResolving is initialized,
  ''' you can easy use .NET (Fusion) to load your assembly without knowing its location: Assembly.Load("YourAssemblyName") 'without file extension!
  ''' The only task is, to make sure that 'AssemblyResolving.Initialize()' has been called at least once!
  ''' </summary>
  Public Shared ReadOnly Property ResolvePaths As String()
    Get
      SyncLock _ResolvePaths
        Return _ResolvePaths.ToArray()
      End SyncLock
    End Get
  End Property

  ''' <summary>
  ''' Please dont use this property to build your own resolver! If the AssemblyResolving is initialized,
  ''' you can easy use .NET (Fusion) to load your assembly without knowing its location: Assembly.Load("YourAssemblyName") 'without file extension!
  ''' The only task is, to make sure that 'AssemblyResolving.Initialize()' has been called at least once!
  ''' </summary>
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
      folder = Path.GetFullPath(folder)
    ElseIf (String.IsNullOrWhiteSpace(relativeTo)) Then
      folder = Path.GetFullPath(Path.Combine(AssemblyResolving.PrimaryApplicationRootDirectory, folder))
    Else
      relativeTo = AssemblyResolving.ResolveRootPlaceholder(relativeTo)
      folder = Path.GetFullPath(Path.Combine(relativeTo, folder))
    End If

    If (Not folder.EndsWith(Path.DirectorySeparatorChar)) Then
      'das erledigt 'Path.GetFullPath' komischer weise nicht....
      folder = folder + Path.DirectorySeparatorChar
    End If

    Return folder.ToLower()
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

#Region " Resolving Logic "

  Public Shared Function TryResolveFullAssemblyPath(assemblyName As String, useFusionLoad As Boolean, ByRef assemblyFullFileName As String) As Boolean
    If (useFusionLoad) Then
      Dim ass = Assembly.Load(assemblyName)
      If (ass Is Nothing) Then
        Return False
      Else
        assemblyFullFileName = ass.Location
        Return True
      End If
    Else
      Return TryFindAssemblyFileByName(assemblyName, assemblyFullFileName)
    End If
  End Function

  Public Shared Function TryFindAssemblyFileByName(assemblyName As String, ByRef assemblyFullFileName As String) As Boolean

    Dim assemblyFilePath As String
    Dim assemblyFileInfo As FileInfo

    For Each resolvePath In AssemblyResolving.ResolvePaths
      assemblyFilePath = Path.Combine(resolvePath, assemblyName)
      assemblyFileInfo = New FileInfo(assemblyFilePath)
      If ((assemblyFileInfo.Exists) AndAlso (assemblyFileInfo.Length > 0)) Then
        assemblyFullFileName = assemblyFilePath
        Return True
      End If
    Next

    Dim sc = StringComparison.CurrentCultureIgnoreCase
    If (Not assemblyName.EndsWith(".dll", sc) AndAlso Not assemblyName.EndsWith(".exe", sc)) Then
      If (TryFindAssemblyFileByName(assemblyName, assemblyFullFileName + ".dll")) Then
        Return True
      End If
      If (TryFindAssemblyFileByName(assemblyName, assemblyFullFileName + ".exe")) Then
        Return True
      End If
    End If

    Return False
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

    Dim assN = New AssemblyName(e.Name)
    Dim assemblyFullFileName As String = Nothing

    If (TryFindAssemblyFileByName(assN.Name + ".dll", assemblyFullFileName)) Then
      Try
        Return Assembly.LoadFrom(assemblyFullFileName)
      Catch
      End Try
    End If

    If (TryFindAssemblyFileByName(assN.Name + ".exe", assemblyFullFileName)) Then
      Try
        Return Assembly.LoadFrom(assemblyFullFileName)
      Catch
      End Try
    End If

    Return Nothing
  End Function

#End Region

  Public Enum AssemblyResolvingFixpoint As Integer
    PrimaryApplicationDirectory = 0
    LocationOfCallingAssembly = 1
    LocationOfAssemblyResolving = 2
    LocationOfAppdomainConfigFile = 3
  End Enum

End Class

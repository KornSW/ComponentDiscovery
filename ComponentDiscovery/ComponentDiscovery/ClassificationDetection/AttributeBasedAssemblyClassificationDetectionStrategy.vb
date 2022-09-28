'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection
Imports System.Runtime
Imports System.Threading
Imports System.Threading.Tasks

Namespace ComponentDiscovery.ClassificationDetection

  Public Class AttributeBasedAssemblyClassificationDetectionStrategy
    Implements IAssemblyClassificationDetectionStrategy

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _EnableAnalysisSandbox As Boolean

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _EnablePersistentCache As Boolean

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _DefaultsIfNoAttributeFound As String()

    Public Sub New(ParamArray defaultsIfNoAttributeFound As String())
      MyClass.New(False, defaultsIfNoAttributeFound)
    End Sub

    Public Sub New(enablePersistentCache As Boolean, ParamArray defaultsIfNoAttributeFound As String())
#If NET461 Then
      MyClass.New(True, False, defaultsIfNoAttributeFound)
#Else
#Disable Warning BC40000 'this call is safe!
      MyClass.New(False, enablePersistentCache, defaultsIfNoAttributeFound)
#Enable Warning BC40000
#End If
    End Sub

#If NET461 Then
     Public Sub New(enableAnalysisSandbox As Boolean, enablePersistentCache As Boolean, ParamArray defaultsIfNoAttributeFound As String())
#Else
    <Obsolete("WARNING: In .NET CORE there are no AppDomains anymore - so we cannot use them to run a AnalysisSandbox! were working to find another solution to provide this feature again!")>
    Public Sub New(enableAnalysisSandbox As Boolean, enablePersistentCache As Boolean, ParamArray defaultsIfNoAttributeFound As String())
      If (enableAnalysisSandbox) Then
        Throw New NotSupportedException("In .NET CORE there are no AppDomains anymore - so we cannot use them to run a AnalysisSandbox! were working to find another solution to provide this feature again!")
      End If
#End If
      _EnableAnalysisSandbox = enableAnalysisSandbox
      _EnablePersistentCache = enablePersistentCache
      _DefaultsIfNoAttributeFound = defaultsIfNoAttributeFound

      If (_DefaultsIfNoAttributeFound Is Nothing) Then
        _DefaultsIfNoAttributeFound = {}
      End If

    End Sub

    Public ReadOnly Property DefaultsIfNoAttributeFound As String()
      Get
        Return _DefaultsIfNoAttributeFound
      End Get
    End Property

    Public Function TryDetectClassificationsForAssembly(
      assemblyFullFilename As String,
      taxonomicDimensionName As String,
      ByRef classifications As String()
    ) As Boolean Implements IAssemblyClassificationDetectionStrategy.TryDetectClassificationsForAssembly

      Return Me.TryDetectClassificationsForAssemblyCore(
        assemblyFullFilename,
        taxonomicDimensionName,
        classifications
      )

    End Function

    Protected Overridable ReadOnly Property FetchMethod As Func(Of String, String, String())
      Get
        Return AddressOf FetchClassificationExpressionsFromAssembly
      End Get
    End Property

    Private Function TryDetectClassificationsForAssemblyCore(
      assemblyFullFilename As String,
      taxonomicDimensionName As String,
      ByRef classifications As String()
    ) As Boolean

      Dim result As String() = Nothing

      If (
        _EnablePersistentCache AndAlso
        PersistentIndexCache.GetInstance().TryGetClassificationExpressionsFromCache(
          assemblyFullFilename, taxonomicDimensionName, result
        )
      ) Then

        classifications = result
        If (classifications.Length = 0) Then
          classifications = _DefaultsIfNoAttributeFound
        End If

        Return True
      End If

      Try
        If (_EnableAnalysisSandbox) Then
#If NET461 Then
          EnsureSandboxDomainIsInitialized()
          _NumberOfAssembliesUsedInSandbox += 1
          SyncLock _AppdomainAccessSemaphore
            result = _SandboxDomain.Invoke(Of String, String, String())(Me.FetchMethod, assemblyFullFilename, taxonomicDimensionName)
          End SyncLock
#Else

          Throw New NotSupportedException()
          'https://www.michael-whelan.net/replacing-appdomain-in-dotnet-core/

#End If
        Else
          'UNSAFE!!!
          result = Me.FetchMethod.Invoke(assemblyFullFilename, taxonomicDimensionName)
        End If

      Catch ex As Exception
        result = Nothing
      End Try

      If (result Is Nothing) Then
        'Nothing = ERROR
        Return False
      End If

      If (_EnablePersistentCache) Then
        PersistentIndexCache.GetInstance().AppendClassificationExpressionToCache(assemblyFullFilename, taxonomicDimensionName, result)
      End If

      classifications = result
      If (classifications.Length = 0) Then
        classifications = _DefaultsIfNoAttributeFound
      End If

      Return True
    End Function

    Private Shared Function FetchClassificationExpressionsFromAssembly(assemblyFullFilename As String, dimensionName As String) As String()
      Try
        Dim assemblyToAnalyze = Assembly.LoadFile(assemblyFullFilename)
        dimensionName = dimensionName.ToLower()
        If (assemblyToAnalyze IsNot Nothing) Then

          Dim attribs = assemblyToAnalyze.GetCustomAttributes.Where(Function(a) a.GetType().Name = NameOf(AssemblyMetadataAttribute) OrElse a.GetType().Name = NameOf(AssemblyClassificationAttribute)).ToArray()

          Dim expressions = (
            attribs.OfType(Of AssemblyMetadataAttribute).
            Where(Function(a) a.Key.ToLower() = dimensionName).
            Select(Function(a) a.Value)
          )

          expressions = expressions.Union(
            attribs.OfType(Of AssemblyClassificationAttribute).
            Where(Function(a) a.TaxonomicDimensionName.ToLower() = dimensionName).
            Select(Function(a) a.ClassificationExpression)
          )

          Return expressions.Distinct().ToArray()

        End If
      Catch ex As BadImageFormatException 'non-.NET-dll
        'EXPECTED: happens on non-.NET-dll
      Catch ex As Exception
        Diag.Error(ex)
      End Try

      Return Nothing '=ERROR
    End Function

#Region " Sandboxing "

    Private Shared _SandboxDomain As AppDomain = Nothing

    Private Shared _KeepSandboxDomainRunningUntil As DateTime

    Private Shared _AppdomainAccessSemaphore As New Object

    Private Shared _SheduledAppdomainShutdownTask As Task = Nothing

    Private Shared _OuterAppDomainShutDown As Boolean = False

    Private Shared _NumberOfAssembliesUsedInSandbox As Integer = 0

    Shared Sub New()
      AddHandler AppDomain.CurrentDomain.DomainUnload,
        Sub(s, e)
          _OuterAppDomainShutDown = True
          If (_SandboxDomain IsNot Nothing) Then
            AppDomain.Unload(_SandboxDomain)
            _SandboxDomain = Nothing
          End If
        End Sub
    End Sub

    Private Shared Sub EnsureSandboxDomainIsInitialized()
      SyncLock _AppdomainAccessSemaphore

        If (_NumberOfAssembliesUsedInSandbox >= 40) Then
          ShutdownSandboxAppdomain()
        End If

        _KeepSandboxDomainRunningUntil = DateTime.Now.AddSeconds(10)

        If (_SandboxDomain Is Nothing) Then

#If NET461 Then

          Dim parentSetup = AppDomain.CurrentDomain.SetupInformation
          Dim setup As New AppDomainSetup()

          setup.ApplicationName = "Sandbox for Assembly Indexing"
          setup.ApplicationBase = parentSetup.ApplicationBase
          setup.PrivateBinPath = parentSetup.PrivateBinPath
          setup.TargetFrameworkName = parentSetup.TargetFrameworkName
          setup.LoaderOptimization = LoaderOptimization.SingleDomain

          _SandboxDomain = AppDomain.CreateDomain("AssemblyEvaluationSandbox", Nothing, setup)
#Else
          Throw New NotSupportedException()
#End If

          _NumberOfAssembliesUsedInSandbox = 0

        End If
      End SyncLock
      If (_SheduledAppdomainShutdownTask Is Nothing OrElse _SheduledAppdomainShutdownTask.IsCompleted) Then
        _SheduledAppdomainShutdownTask = Task.Run(AddressOf SheduledAppdomainShutdown)
      End If
    End Sub

    Private Shared Sub SheduledAppdomainShutdown()
      Do
        Try
          Thread.Sleep(500)
        Catch
        End Try
        SyncLock _AppdomainAccessSemaphore
          If (_KeepSandboxDomainRunningUntil < DateTime.Now AndAlso _SandboxDomain IsNot Nothing) Then
            ShutdownSandboxAppdomain()
            Exit Do
          End If
        End SyncLock
      Loop Until _OuterAppDomainShutDown
    End Sub

    Private Shared Sub ShutdownSandboxAppdomain()
      Dim domainToUnload = _SandboxDomain
      _SandboxDomain = Nothing
      Try
        AppDomain.Unload(domainToUnload)

        GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce
        GC.Collect(GC.MaxGeneration)
      Catch
      End Try
    End Sub

#End Region

  End Class

End Namespace

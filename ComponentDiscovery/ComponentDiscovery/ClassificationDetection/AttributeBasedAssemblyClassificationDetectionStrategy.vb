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
Imports System.Threading
Imports System.Threading.Tasks

Namespace ClassificationDetection

  Public Class AttributeBasedAssemblyClassificationDetectionStrategy
    Implements IAssemblyClassificationDetectionStrategy

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _EnableAnalysisSandbox As Boolean

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _EnablePersistentCache As Boolean

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _DefaultsIfNoAttributeFound As String()

    Public Sub New(ParamArray defaultsIfNoAttributeFound As String())
      MyClass.New(True, False, defaultsIfNoAttributeFound)
    End Sub

    Public Sub New(enableAnalysisSandbox As Boolean, enablePersistentCache As Boolean, ParamArray defaultsIfNoAttributeFound As String())

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

    Public Function TryDetectClassificationsForAssembly(assemblyFullFilename As String, taxonomicDimensionName As String, ByRef classifications As String()) As Boolean Implements IAssemblyClassificationDetectionStrategy.TryDetectClassificationsForAssembly

      Dim result As String() = Nothing

      If (
        _EnablePersistentCache AndAlso
        PersistentIndexCache.GetInstance().TryGetClassificationExpressionsFromCache(assemblyFullFilename, taxonomicDimensionName, result)
      ) Then

        classifications = result
        Return True
      End If

      Try
        If (_EnableAnalysisSandbox) Then
          EnsureSandboxDomainIsInitialized()
          SyncLock _AppdomainAccessSemaphore
            result = _SandboxDomain.Invoke(Of String, String, String())(AddressOf FetchClassificationExpressionsFromAssembly, assemblyFullFilename, taxonomicDimensionName)
          End SyncLock
        Else
          'UNSAFE!!!
          result = FetchClassificationExpressionsFromAssembly(assemblyFullFilename, taxonomicDimensionName)
        End If

      Catch ex As Exception
        result = Nothing
      End Try

      If (result Is Nothing) Then
        'Nothing = ERROR
        Return False
      ElseIf (result.Length = 0) Then
        'no Attributes found -> take the default
        result = _DefaultsIfNoAttributeFound
      End If

      If (_EnablePersistentCache) Then
        PersistentIndexCache.GetInstance().WriteClassificationExpressionToCache(assemblyFullFilename, taxonomicDimensionName, result)
      End If

      classifications = result
      Return True
    End Function

    Private Shared Function FetchClassificationExpressionsFromAssembly(assemblyFullFilename As String, dimensionName As String) As String()
      Try
        Dim assemblyToAnalyze = Assembly.LoadFile(assemblyFullFilename)
        dimensionName = dimensionName.ToLower()
        If (assemblyToAnalyze IsNot Nothing) Then

          Dim attribs = assemblyToAnalyze.GetCustomAttributes.Where(Function(a) a.GetType.Name = NameOf(AssemblyClassificationAttribute)).ToArray()

          Dim expressions = (
          attribs.OfType(Of AssemblyClassificationAttribute).
          Where(Function(a) a.TaxonomicDimensionName.ToLower() = dimensionName).
          Select(Function(a) a.ClassificationExpression).
          ToArray()
        )
          Return expressions

        End If
      Catch ex As BadImageFormatException 'non-.NET-dll
        'EXPECTED: happens on non-.NET-dll
      Catch ex As Exception
        System.Diagnostics.Trace.WriteLine(ex.Message)
      End Try

      Return Nothing '=ERROR
    End Function

#Region " Sandboxing "

    Private Shared _SandboxDomain As AppDomain = Nothing

    Private Shared _KeepSandboxDomainRunningUntil As DateTime

    Private Shared _AppdomainAccessSemaphore As New Object

    Private Shared _SheduledAppdomainShutdownTask As Task = Nothing

    Private Shared _OuterAppDomainShutDown As Boolean = False

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
        _KeepSandboxDomainRunningUntil = DateTime.Now.AddSeconds(10)
        If (_SandboxDomain Is Nothing) Then

          Dim parentSetup = AppDomain.CurrentDomain.SetupInformation
          Dim setup As New AppDomainSetup()
          setup.ApplicationName = "Sandbox for Assembly Indexing"
          setup.ApplicationBase = parentSetup.ApplicationBase
          setup.PrivateBinPath = parentSetup.PrivateBinPath
          setup.TargetFrameworkName = parentSetup.TargetFrameworkName
          setup.LoaderOptimization = LoaderOptimization.SingleDomain

          _SandboxDomain = AppDomain.CreateDomain("AssemblyEvaluationSandbox", Nothing, setup)
        End If
      End SyncLock
      If (_SheduledAppdomainShutdownTask Is Nothing OrElse _SheduledAppdomainShutdownTask.IsCompleted) Then
        _SheduledAppdomainShutdownTask = Task.Run(AddressOf SheduledAppdomainShutdown)
      End If
    End Sub

    Private Shared Sub SheduledAppdomainShutdown()

      Do

        Try
          Thread.Sleep(300)
        Catch
        End Try

        SyncLock _AppdomainAccessSemaphore

          If (_KeepSandboxDomainRunningUntil < DateTime.Now AndAlso _SandboxDomain IsNot Nothing) Then
            Dim domainToUnload = _SandboxDomain
            _SandboxDomain = Nothing
            Try
              AppDomain.Unload(domainToUnload)
            Catch
            End Try
            Exit Do
          End If

        End SyncLock

      Loop Until _OuterAppDomainShutDown

    End Sub

#End Region

  End Class

End Namespace

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection
Imports System.Threading
Imports System.Threading.Tasks

' HACK: Umbenennen: AttributeBasedClassificationStrategy
'HACK: trennen in IAssemblyClassificationEvaluationStrategy und IAssemblyApprovalStrategy
Public Class AttributeBasedApprovalStrategy
  Implements IAssemblyClassificationStrategy

  Private _ClassificationExpressionsPerAssembly As New Dictionary(Of String, String())

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _ClearanceExpressions As New List(Of String)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _DimensionName As String

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _EnableAnalysisSandbox As Boolean

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _EnablePersistentCache As Boolean

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _DefaultsIfNoAttributeFound As String()

  Public Event ClearancesAdded(addedClearanceExpressions() As String) Implements IAssemblyClassificationStrategy.ClearancesAdded

  Public Sub New(
    dimensionName As String,
    Optional enableAnalysisSandbox As Boolean = False,
    Optional enablePersistentCache As Boolean = False,
    Optional defaultsIfNoAttributeFound As String() = Nothing
  )
    _DimensionName = dimensionName
    _EnableAnalysisSandbox = enableAnalysisSandbox
    _EnablePersistentCache = enablePersistentCache
    _DefaultsIfNoAttributeFound = defaultsIfNoAttributeFound

    If (_DefaultsIfNoAttributeFound Is Nothing) Then
      _DefaultsIfNoAttributeFound = {}
    End If

  End Sub

  Public ReadOnly Property DimensionName As String
    Get
      Return _DimensionName
    End Get
  End Property

  Public ReadOnly Property DefaultsIfNoAttributeFound As String()
    Get
      Return _DefaultsIfNoAttributeFound
    End Get
  End Property

  Public ReadOnly Property Clearances As String() Implements IAssemblyClassificationStrategy.Clearances
    Get
      Return _ClearanceExpressions.ToArray()
    End Get
  End Property

#Region " Sandboxing "

  Private Shared _SandboxDomain As AppDomain = Nothing

  Private Shared _KeepSandboxDomainRunningUntil As DateTime

  Private Shared _AppdomainAccessSemaphore As New Object

  Private Shared _SheduledAppdomainShutdownTask As Task = Nothing

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
      Thread.Sleep(1000)
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
    Loop
  End Sub

#End Region

  Protected Function GetClassifications(assemblyFullFilename As String) As String() Implements IAssemblyClassificationStrategy.GetClassifications

    SyncLock _ClassificationExpressionsPerAssembly

      If (_ClassificationExpressionsPerAssembly.ContainsKey(assemblyFullFilename)) Then
        Return _ClassificationExpressionsPerAssembly(assemblyFullFilename)
      End If

      Dim classificationExpressions As String() = Nothing

      If (
        _EnablePersistentCache AndAlso
        AssemblyClassificationCache.GetInstance().TryGetClassificationExpressionsFromCache(assemblyFullFilename, _DimensionName, classificationExpressions)
      ) Then
        Return classificationExpressions
      End If

      Try
        If (_EnableAnalysisSandbox) Then
          EnsureSandboxDomainIsInitialized()
          SyncLock _AppdomainAccessSemaphore
            classificationExpressions = _SandboxDomain.Invoke(Of String, String, String())(AddressOf FetchClassificationExpressionsFromAssembly, assemblyFullFilename, _DimensionName)
          End SyncLock
        Else
          'UNSAFE!!!
          classificationExpressions = FetchClassificationExpressionsFromAssembly(assemblyFullFilename, _DimensionName)
        End If

      Catch ex As Exception
        classificationExpressions = Nothing
      End Try

      If (classificationExpressions Is Nothing) Then
        'Nothing = ERROR
        Return New String() {}
      ElseIf (classificationExpressions.Length = 0) Then
        'no Attributes found -> take the default
        classificationExpressions = _DefaultsIfNoAttributeFound
      End If

      _ClassificationExpressionsPerAssembly.Add(assemblyFullFilename, classificationExpressions)

      If (_EnablePersistentCache) Then
        AssemblyClassificationCache.GetInstance().WriteScopeValuesToCache(assemblyFullFilename, _DimensionName, classificationExpressions)
      End If

      Return classificationExpressions
    End SyncLock
  End Function

  Private Shared Function FetchClassificationExpressionsFromAssembly(assemblyFullFilename As String, dimensionName As String) As String()
    Try
      Dim assemblyToAnalyze = Assembly.LoadFile(assemblyFullFilename)
      dimensionName = dimensionName.ToLower()
      If (assemblyToAnalyze IsNot Nothing) Then

        Dim attribs = assemblyToAnalyze.GetCustomAttributes.Where(Function(a) a.GetType.Name = NameOf(AssemblyClassificationAttribute)).ToArray()

        Dim expressions = (
          attribs.OfType(Of AssemblyClassificationAttribute).
          Where(Function(a) a.SemanticDimensionName.ToLower() = dimensionName).
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

  Public Function AddClearancesFromAssembly(assemblyFullFilename As String) As Boolean Implements IAssemblyClassificationStrategy.AddClearancesFromAssembly
    Dim classificationExpressions = Me.GetClassifications(assemblyFullFilename)
    Return Me.AddClearances(classificationExpressions)
  End Function

  Public Function AddClearances(ParamArray addingExpressions() As String) As Boolean Implements IAssemblyClassificationStrategy.AddClearances
    Dim newExpressionsDetected As Boolean = False
    Dim addedExpressions As New List(Of String)

    For Each addingExpression In addingExpressions
      addingExpression = addingExpression.ToLower()

      If (Not _ClearanceExpressions.Contains(addingExpression)) Then
        _ClearanceExpressions.Add(addingExpression)
        addedExpressions.Add(addingExpression)
        newExpressionsDetected = True
      End If

    Next

    Me.OnClearancesAdded(addedExpressions.ToArray())
    Return newExpressionsDetected
  End Function

  Public Function VerifyAssembly(assemblyFullFilename As String) As Boolean Implements IAssemblyClassificationStrategy.VerifyAssembly
    Dim classificationExpressions = Me.GetClassifications(assemblyFullFilename)

    If (classificationExpressions.Any()) Then

      For Each classificationExpression In classificationExpressions
        If (Not _ClearanceExpressions.Contains(classificationExpression.ToLower())) Then
          Return False
        End If
      Next

      Return True
    End If

    Return False
  End Function

  Protected Sub OnClearancesAdded(addedExpressions() As String)
    If (ClearancesAddedEvent IsNot Nothing) Then
      RaiseEvent ClearancesAdded(addedExpressions)
    End If
  End Sub

End Class

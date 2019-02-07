Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection

Public Class ClassificationBasedAssemblyIndexer
  Inherits AssemblyIndexer

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _ClassificationStrategiesBySemanticDimension As New Dictionary(Of String, IAssemblyClassificationStrategy)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _OverwrittenApplicationWorkDir As DirectoryInfo = Nothing

  Public Sub New()
    MyBase.New()
  End Sub

  Public Sub New(applicationWorkDir As String)
    MyBase.New()
    _OverwrittenApplicationWorkDir = New DirectoryInfo(applicationWorkDir)
  End Sub

  Protected Overrides Function GetApplicationWorkDir() As DirectoryInfo
    If (_OverwrittenApplicationWorkDir Is Nothing) Then
      Return MyBase.GetApplicationWorkDir()
    Else
      Return _OverwrittenApplicationWorkDir
    End If
  End Function

  Public Function GetClearances(semanticDimensionName As String) As String()
    Return _ClassificationStrategiesBySemanticDimension(semanticDimensionName).Clearances
  End Function

  Protected Friend ReadOnly Property SemanticDimensionNames As String()
    Get
      Return _ClassificationStrategiesBySemanticDimension.Keys.ToArray()
    End Get
  End Property

  Protected Friend ReadOnly Property ClassificationStrategies As IAssemblyClassificationStrategy()
    Get
      Return _ClassificationStrategiesBySemanticDimension.Values.ToArray()
    End Get
  End Property

  Protected Friend ReadOnly Property ClassificationStrategy(semanticDimensionName As String) As IAssemblyClassificationStrategy
    Get
      Dim lowerDimensionName = semanticDimensionName.ToLower()
      For Each semanticDimensionName In _ClassificationStrategiesBySemanticDimension.Keys
        If (semanticDimensionName.ToLower() = lowerDimensionName) Then
          Return _ClassificationStrategiesBySemanticDimension(semanticDimensionName)
        End If
      Next
      Throw New KeyNotFoundException(String.Format("There is no ClassificationStrategy registered under dimension '{0}'!", semanticDimensionName))
    End Get
  End Property

  ''' <summary>
  '''   Adds clearances to the internal clearance collection which will broaden the set of approvable assemblies.
  ''' </summary>
  ''' <remarks> Adding clearances could implicitely approve additional assemblies. </remarks>
  Public Sub AddClearances(semanticDimensionName As String, ParamArray clearanceExpressions() As String)
    Me.ClassificationStrategy(semanticDimensionName).AddClearances(clearanceExpressions)
    Me.ReapproveDismissedAssemblies()
  End Sub

  ''' <summary>
  '''   This will just check, if all assembly's classifications match the current clearance situation.
  '''   This will NOT approve the assembly! (It won't be added to the index).
  ''' </summary>
  ''' <param name="assemblyFullFilename"> The assembly to verify. </param>
  ''' <returns> True, if it's a match. </returns>
  Public Function VerifyAssemblyWithinOneDimension(assemblyFullFilename As String, semanticDimensionName As String) As Boolean
    Return Me.ClassificationStrategy(semanticDimensionName).VerifyAssembly(assemblyFullFilename)
  End Function


  ''' <summary>
  '''   Fetches the classification expressions from an assembly and adds them as clearance expressions.
  ''' </summary>
  ''' <remarks> 
  '''   Adding clearances could implicitely approve additional assemblies.
  '''   THIS METHOD DOES NOT APPROVE THE INCOMING ASSEMBLY. IF YOU WANT TO DO THIS YOU MUST USE <seealso cref="AddAssemblyAndImportClearances"/>!
  ''' </remarks>
  ''' <seealso cref="AddAssemblyAndImportClearances"/>
  Public Sub AddClearancesFromAssembly(assembly As Assembly)
    Trace.TraceInformation(String.Format("AssemblyIndexer: Importing scopevalues of '{0}' to whitelists...", assembly.GetName().Name))
    Dim expanded As Boolean = False
    For Each strategy In Me.ClassificationStrategies
      expanded = expanded Or strategy.AddClearancesFromAssembly(assembly.Location)
    Next
    If (expanded) Then
      Me.ReapproveDismissedAssemblies()
    End If
  End Sub

  Public Sub AddDimension(semanticDimensionName As String, assemblyAnalyzerMethod As Action(Of String, List(Of String)))

    Dim lowerDimensionName = semanticDimensionName.ToLower()
    For Each registeredName In _ClassificationStrategiesBySemanticDimension.Keys
      If (registeredName.ToLower() = lowerDimensionName) Then
        Throw New Exception("This dimensionName is already registered!")
      End If
    Next

    Me.AddDimension(semanticDimensionName, New DelegateBasedClassificationStrategy(assemblyAnalyzerMethod))

  End Sub

  ''' <summary>
  '''   Adds a Dimension to the classification/approval algorithm.
  ''' </summary>
  ''' <remarks>
  '''   Only assemblies matching all clearances of dimensions will be approved.
  '''   So adding dimensions will tend to narrowing down the set of approvable assemblies.
  ''' </remarks>
  Public Sub AddDimension(semanticDimensionName As String, classificationStrategy As IAssemblyClassificationStrategy)

    Dim lowerDimensionName = semanticDimensionName.ToLower()
    For Each existingDimensionName In _ClassificationStrategiesBySemanticDimension.Keys
      If (existingDimensionName.ToLower() = lowerDimensionName) Then
        Throw New Exception("This scopeName is already enabled!")
      End If
    Next

    For Each alreadyApprovedAssembly In Me.ApprovedAssemblies
      If (Not classificationStrategy.VerifyAssembly(alreadyApprovedAssembly.Location)) Then
        Throw New Exception(
        String.Format(
          "Cannot enable the scope '{0}' because the already approved assembly '{1}' would retroactively turn illegal!",
          semanticDimensionName, alreadyApprovedAssembly.FullName
        )
      )
      End If
    Next

    _ClassificationStrategiesBySemanticDimension.Add(semanticDimensionName, classificationStrategy)
    System.Diagnostics.Trace.TraceInformation(String.Format("AssemblyIndexer: composition scope '{0}' was enabled!", semanticDimensionName))

  End Sub

  ''' <summary>
  '''   This can be hoocked to manipulate and break the strict conjunction (AND relation) of the scopes by implementing a custom predicate logic
  ''' </summary>
  Protected Overridable Function SummarizeMatchingResults(matchingResultsPerDimension As Dictionary(Of String, Boolean)) As Boolean

    For Each result In matchingResultsPerDimension.Values
      If (result = False) Then
        Return False
      End If
    Next

    Return True
  End Function

  ''' <summary>
  '''   Fetches the classification expressions from an assembly and adds them as clearance expressions.
  ''' </summary>
  ''' <remarks> 
  '''   Importing clearances will implicitely approve the incoming assembly PLUS other assemblies that match the newly created clearance situation.
  ''' </remarks>
  Public Overridable Sub AddAssemblyAndImportClearances(moduleEntryAssembly As Assembly)

    System.Diagnostics.Trace.TraceInformation(String.Format("AssemblyIndexer: Expanding clearance to imply the scope values of assembly '{0}'", moduleEntryAssembly.GetName().Name))
    Me.AddClearancesFromAssembly(moduleEntryAssembly) 'unlock the scopes (new way over attributes)

    'now lets add the assembly (which will be approved successfully)
    Me.TryApproveAssembly(moduleEntryAssembly.Location, True)

  End Sub

  ''' <summary>
  '''   This will just check, if all assembly's classifications match the current clearances situation.
  '''   This is done for all dimensions - all dimensions have to match.
  '''   This will NOT approve the assembly! (It won't be added to the index).
  ''' </summary>
  ''' <param name="assemblyFullFilename"> The assembly to verify. </param>
  ''' <returns> True, if it's a match. </returns>
  Protected Overrides Function VerifyAssembly(assemblyFullFilename As String, isReapprove As Boolean) As Boolean
    Dim title = Path.GetFileNameWithoutExtension(assemblyFullFilename)

    If (Not isReapprove AndAlso Me.IsExternalFrameworkAssembly(assemblyFullFilename)) Then
      System.Diagnostics.Trace.TraceInformation(String.Format("AssemblyIndexer: approving of '{0}' failed because the assembly is on the blacklist for 'external framework components'!", title))
      Return False 'the assembly is on the blacklist of external framework components
    End If

    Dim matchingResultsPerDimension As New Dictionary(Of String, Boolean)
    For Each dimensionName In _ClassificationStrategiesBySemanticDimension.Keys

      If (_ClassificationStrategiesBySemanticDimension(dimensionName).VerifyAssembly(assemblyFullFilename)) Then
        matchingResultsPerDimension.Add(dimensionName, True)
      Else
        matchingResultsPerDimension.Add(dimensionName, False)

        'If (Not isReapprove) Then
        'WARNING: THIS LINE WILL SLOW DOWN THE PERFORMANCE EXTREEMLELY:
        'Trace.TraceInformation(String.Format("AssemblyIndexer: assembly '{0}' does not match into composition scope '{1}'!", title, scopeName))
        'End If

      End If
    Next

    If (Me.SummarizeMatchingResults(matchingResultsPerDimension)) Then
      Return True
    Else
      Return False
    End If

  End Function

#Region " Blacklist (for components of external Frameworks) "

  'HACK: needs to come from outside!
  Protected Overridable Function IsExternalFrameworkAssembly(assemblyFullFilename As String) As Boolean
    With (Path.GetFileNameWithoutExtension(assemblyFullFilename).ToLower())

      Select Case (True)

        Case .StartsWith("system.")
        Case .StartsWith("microsoft*")
        Case .StartsWith("mscorlib*")
        Case .StartsWith("vshost*")
        Case .StartsWith("entityframework*")
        Case .StartsWith("anonymously*")
        Case .StartsWith("webgrease*")
        Case .StartsWith("aspnet*")
        Case .StartsWith("antlr3*")
        Case .StartsWith("owin*")
        Case .StartsWith("newtonsoft*")
        Case .StartsWith("msvcr90")
        Case .StartsWith("sqlceca40")
        Case .StartsWith("sqlcecompact40")
        Case .StartsWith("sqlceer40EN")
        Case .StartsWith("sqlceme40")
        Case .StartsWith("sqlceqp40")
        Case .StartsWith("sqlcese40")
        Case .StartsWith("libeay32")
        Case .StartsWith("libgcc_s_dw2-1")
        Case .StartsWith("mingwm10")
        Case .StartsWith("msvcp120")
        Case .StartsWith("msvcr120")
        Case .StartsWith("ssleay32")
        Case .StartsWith("wkhtmltox0")

        Case Else
          Return False
      End Select

      Return True
    End With
  End Function

#End Region

#Region " Nested Class 'DelegateBasedClassificationStrategy' "

  <DebuggerDisplay("DelegateBasedClassificationStrategy '{Name}'")>
  Protected Class DelegateBasedClassificationStrategy
    Implements IAssemblyClassificationStrategy

    <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
    Private _ClearanceExpressions As New List(Of String)

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _AssemblyAnalyzerMethod As Action(Of String, List(Of String)) = Nothing

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _AllowUnscoped As Boolean = False

    Public Sub New(assemblyAnalyzerMethod As Action(Of String, List(Of String)), Optional allowUnscoped As Boolean = False)
      _AssemblyAnalyzerMethod = assemblyAnalyzerMethod
      _AllowUnscoped = allowUnscoped
    End Sub

    Public ReadOnly Property AssemblyAnalyzerMethod As Action(Of String, List(Of String))
      Get
        Return _AssemblyAnalyzerMethod
      End Get
    End Property

    Public Function GetClassifications(assemblyFullFilename As String) As String() Implements IAssemblyClassificationStrategy.GetClassifications
      Dim buffer As New List(Of String)
      _AssemblyAnalyzerMethod.Invoke(assemblyFullFilename, buffer)
      Return buffer.ToArray()
    End Function

    Public ReadOnly Property AllowUnscoped As Boolean
      Get
        Return _AllowUnscoped
      End Get
    End Property

    Public Function AddClearancesFromAssembly(assemblyFullFilename As String) As Boolean Implements IAssemblyClassificationStrategy.AddClearancesFromAssembly
      Dim classificationExpressions As New List(Of String)
      Me.AssemblyAnalyzerMethod.Invoke(assemblyFullFilename, classificationExpressions)
      Return Me.AddClearances(classificationExpressions.ToArray())
    End Function

    Public Function AddClearances(ParamArray addingExpressions() As String) As Boolean Implements IAssemblyClassificationStrategy.AddClearances
      If (addingExpressions.Length < 1) Then
        Return False
      End If
      Dim addedExpressions As New List(Of String)
      System.Diagnostics.Trace.TraceInformation(String.Format("AssemblyIndexer: unlocking scopevalues '{0}'", String.Join("', '", addingExpressions)))
      Dim newExpressionsDetected As Boolean = False
      For Each addingExpression In addingExpressions
        addingExpression = addingExpression.ToLower()
        If (Not _ClearanceExpressions.Contains(addingExpression)) Then
          newExpressionsDetected = True
          _ClearanceExpressions.Add(addingExpression)
          addedExpressions.Add(addingExpression)
        End If
      Next

      Me.OnClearanceExpressionsAdded(addedExpressions.ToArray())
      Return newExpressionsDetected
    End Function

    Public Function VerifyAssembly(assemblyFullFilename As String) As Boolean Implements IAssemblyClassificationStrategy.VerifyAssembly
      Dim classificationExpressions As New List(Of String)
      Me.AssemblyAnalyzerMethod.Invoke(assemblyFullFilename, classificationExpressions)
      Return Me.CheckMatch(classificationExpressions.ToArray())
    End Function

    Public Function CheckMatch(ParamArray classificationExpressions() As String) As Boolean
      If (classificationExpressions.Length = 0) Then
        Return Me.AllowUnscoped
      End If
      For Each classificationExpression In classificationExpressions
        If (Not Me.CheckMatch(classificationExpression)) Then
          Return False
        End If
      Next
      Return True
    End Function

    Public Function CheckMatch(classificationExpressions As IEnumerable(Of String)) As Boolean
      Return Me.CheckMatch(classificationExpressions.ToArray())
    End Function

    Public Function CheckMatch(classificationExpression As String) As Boolean
      classificationExpression = classificationExpression.ToLower()

      If (_ClearanceExpressions.Count > 0) Then
        Dim isMatch As Boolean = False
        For Each clearanceExpression In _ClearanceExpressions
          If (classificationExpression.StartsWith(clearanceExpression)) Then
            isMatch = True
          End If
        Next
        If (Not isMatch) Then
          Return False
        End If
      End If

      Return True
    End Function

    Public Event ClearancesAdded(addedExpression() As String) Implements IAssemblyClassificationStrategy.ClearancesAdded

    Protected Sub OnClearanceExpressionsAdded(addedExpression() As String)
      If (ClearancesAddedEvent IsNot Nothing) Then
        RaiseEvent ClearancesAdded(addedExpression)
      End If
    End Sub

    Public ReadOnly Property Clearances As String() Implements IAssemblyClassificationStrategy.Clearances
      Get
        Return _ClearanceExpressions.ToArray()
      End Get
    End Property

  End Class

#End Region

#Region " Import / Export "

  Public Function ExportClearances() As String()
    Dim result As New List(Of String)
    For Each dimensionName In Me.SemanticDimensionNames
      For Each clearanceExpression In Me.ClassificationStrategy(dimensionName).Clearances
        result.Add(String.Format("{0}:{1}", dimensionName, clearanceExpression))
      Next
    Next
    Return result.ToArray()
  End Function

  ''' <summary>
  '''   Adds clearances to the internal clearance collection which will broaden the set of approvable assemblies.
  ''' </summary>
  ''' <param name="qualifiedClearanceExpressions">A string like "DimensionName:ClearanceExpression".</param>
  ''' <remarks> Importing clearances could implicitely approve additional assemblies. </remarks>
  Public Sub ImportClearances(qualifiedClearanceExpressions As String())
    For Each expression In qualifiedClearanceExpressions
      If (expression.Contains(":")) Then

        Dim dimensionName = expression.Substring(0, expression.IndexOf(":"))
        Dim clearanceExpression = expression.Substring(expression.IndexOf(":") + 1)

        If (Me.SemanticDimensionNames.Contains(dimensionName)) Then
          Dim approvalStrategy = Me.ClassificationStrategy(dimensionName)
          approvalStrategy.AddClearances(clearanceExpression)
        End If

      End If
    Next
    Me.ReapproveDismissedAssemblies()
  End Sub

  ''' <summary>
  '''   Adds clearances to the internal clearance collection which will broaden the set of approvable assemblies.
  ''' </summary>
  ''' <param name="clearancesSortedByDimension">A Dictionary(Of "DimensionName", {"ClearanceExpression1", "ClearanceExpression2"})</param>
  ''' <remarks> Importing clearances could implicitely approve additional assemblies. </remarks>
  Public Sub ImportClearances(clearancesSortedByDimension As Dictionary(Of String, String()))
    For Each dimensionName In clearancesSortedByDimension.Keys
      For Each clearanceExpression In clearancesSortedByDimension(dimensionName)
        If (Me.SemanticDimensionNames.Contains(dimensionName)) Then
          Dim approvalStrategy = Me.ClassificationStrategy(dimensionName)
          approvalStrategy.AddClearances(clearanceExpression)
        End If
      Next
    Next
    Me.ReapproveDismissedAssemblies()
  End Sub

#End Region

End Class

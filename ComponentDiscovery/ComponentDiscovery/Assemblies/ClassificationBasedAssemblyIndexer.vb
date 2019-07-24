'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports ComponentDiscovery.ClassificationApproval
Imports ComponentDiscovery.ClassificationDetection

Public Class ClassificationBasedAssemblyIndexer
  Inherits AssemblyIndexer

#Region " Fields & Constructors "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _TaxonomicDimensionsByName As New Dictionary(Of String, TaxonomicDimension)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _OverwrittenApplicationWorkDir As DirectoryInfo = Nothing

  Public Sub New()
    MyBase.New()
  End Sub

  Public Sub New(
    preferAssemblyLoadingViaFusion As Boolean,
    autoImportAllAssembliesFromResolvePaths As Boolean,
    enableAppDomainBinding As Boolean
  )

    MyBase.New(
      preferAssemblyLoadingViaFusion,
      autoImportAllAssembliesFromResolvePaths,
      enableAppDomainBinding
    )

  End Sub

  Public Sub New(applicationWorkDir As String)
    MyBase.New()
    _OverwrittenApplicationWorkDir = New DirectoryInfo(applicationWorkDir)
  End Sub

  'HACK: this hook should be placed in the baseclass
  Protected Overrides Function GetApplicationWorkDir() As DirectoryInfo
    If (_OverwrittenApplicationWorkDir Is Nothing) Then
      Return MyBase.GetApplicationWorkDir()
    Else
      Return _OverwrittenApplicationWorkDir
    End If
  End Function

#End Region

#Region " Dimension Setup "

  <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
  Public ReadOnly Property TaxonomicDimensions As TaxonomicDimension()
    Get
      Return _TaxonomicDimensionsByName.Values.ToArray()
    End Get
  End Property

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Public ReadOnly Property TaxonomicDimensions(dimensionName As String) As TaxonomicDimension
    Get
      Dim lowerDimensionName = dimensionName.ToLower()
      For Each existingName In _TaxonomicDimensionsByName.Keys
        If (existingName.ToLower() = lowerDimensionName) Then
          Return _TaxonomicDimensionsByName(existingName)
        End If
      Next
      Throw New KeyNotFoundException(String.Format("There is no TaxonomicDimension registered with name '{0}'!", dimensionName))
    End Get
  End Property

  Public ReadOnly Property TaxonomicDimensionNames As String()
    Get
      Return _TaxonomicDimensionsByName.Keys.ToArray()
    End Get
  End Property

  ''' <summary>
  ''' this is a Convinence method which adds a new instance of a 'AttributeBasedAssemblyClassificationDetectionStrategy'
  ''' </summary>
  Public Sub AddTaxonomicDimension(taxonomicDimensionName As String, ParamArray defaultClearances() As String)

    Dim lowerDimensionName = taxonomicDimensionName.ToLower()
    For Each registeredName In _TaxonomicDimensionsByName.Keys
      If (registeredName.ToLower() = lowerDimensionName) Then
        Throw New Exception("This dimensionName is already registered!")
      End If
    Next

    Me.AddTaxonomicDimension(
      taxonomicDimensionName,
      New AttributeBasedAssemblyClassificationDetectionStrategy(),
      New DemandCentricClassificationApprovalStrategy(),
      defaultClearances
    )

  End Sub

  ''' <summary>
  ''' this is a Convinence method which adds a new instance of a 'DelegateBasedClassificationStrategy'
  ''' </summary>
  ''' <param name="assemblyClassificationEvaluationMethod">this is how a assembly will be classified. You can specify a method to run your own code evaluating classifications for a given assembly file name into a list of string)</param>
  ''' <param name="classificationApprovalStrategy">this is how a assembly will be approved (opposing the assembly's classifications against the enabled clearances).</param>
  Public Sub AddTaxonomicDimension(
    taxonomicDimensionName As String,
    assemblyClassificationEvaluationMethod As Func(Of String, List(Of String), Boolean),
    classificationApprovalStrategy As IClassificationApprovalStrategy,
    ParamArray defaultClearances() As String
  )

    Dim lowerDimensionName = taxonomicDimensionName.ToLower()
    For Each registeredName In _TaxonomicDimensionsByName.Keys
      If (registeredName.ToLower() = lowerDimensionName) Then
        Throw New Exception("This dimensionName is already registered!")
      End If
    Next

    Me.AddTaxonomicDimension(
      taxonomicDimensionName,
      New DelegateBasedAssemblyClassificationDetectionStrategy(assemblyClassificationEvaluationMethod),
      classificationApprovalStrategy,
      defaultClearances
    )

  End Sub

  ''' <summary>
  '''   Adds a Dimension to the classification/approval algorithm.
  ''' </summary>
  ''' <param name="taxonomicDimensionName"></param>
  ''' <param name="classificationStrategy">this is how a assembly will be classified. in default you can choose between 
  ''' the 'DelegateBasedClassificationStrategy' (which improves you run your own code returning a string() of classifications) OR 
  ''' the 'AttributeBasedAssemblyClassificationStrategy' (which reads the clearances for any assembly out of "AssemblyClassificationAttributes" within the AssemblyInfo)</param>
  ''' <param name="defaultClearances">enable this clearances by default</param>
  ''' <param name="classificationApprovalStrategy">this is how a assembly will be approved (opposing the assembly's classifications against the enabled clearances).</param>
  ''' <remarks>
  '''   Only assemblies matching all clearances of dimensions will be approved.
  '''   So adding dimensions will tend to narrowing down the set of approvable assemblies.
  ''' </remarks>
  Public Sub AddTaxonomicDimension(
    taxonomicDimensionName As String,
    classificationStrategy As IAssemblyClassificationDetectionStrategy,
    classificationApprovalStrategy As IClassificationApprovalStrategy,
    ParamArray defaultClearances() As String
  )

    Dim lowerDimensionName = taxonomicDimensionName.ToLower()
    For Each existingDimensionName In _TaxonomicDimensionsByName.Keys
      If (existingDimensionName.ToLower() = lowerDimensionName) Then
        Throw New Exception("This dimensionName is already registered!")
      End If
    Next

    Dim newDimension As New TaxonomicDimension(taxonomicDimensionName, classificationStrategy, classificationApprovalStrategy)
    If (defaultClearances IsNot Nothing) Then
      newDimension.AddClearances(defaultClearances)
    End If

    For Each alreadyApprovedAssembly In Me.ApprovedAssemblies
      If (Not newDimension.VerifyAssembly(alreadyApprovedAssembly.Location)) Then
        Throw New Exception(
          String.Format(
            "Cannot enable the TaxonomicDimension '{0}' because the already approved assembly '{1}' would be dismissed retroactively!",
            taxonomicDimensionName, alreadyApprovedAssembly.FullName
          )
        )
      End If
    Next

    AddHandler newDimension.ClearancesAdded, AddressOf Me.Dimension_OnClearancesChanged
    _TaxonomicDimensionsByName.Add(taxonomicDimensionName, newDimension)

    Me.OnTaxonomicDimensionAdded(taxonomicDimensionName)

    If (Me.EnableTracing) Then
      Trace.TraceInformation(String.Format("AssemblyIndexer: TaxonomicDimension '{0}' was enabled!", taxonomicDimensionName))
    End If

  End Sub

  Public Event TaxonomicDimensionAdded(taxonomicDimensionName As String)

  Protected Sub OnTaxonomicDimensionAdded(taxonomicDimensionName As String)
    If (TaxonomicDimensionAddedEvent IsNot Nothing) Then
      RaiseEvent TaxonomicDimensionAdded(taxonomicDimensionName)
    End If
  End Sub

#End Region

#Region " dealing with Clearances "

  Public Function GetClearances(taxonomicDimensionName As String) As String()
    Return _TaxonomicDimensionsByName(taxonomicDimensionName).Clearances
  End Function

  ''' <summary>
  '''   Adds clearances to the internal clearance collection which will broaden the set of approvable assemblies.
  ''' </summary>
  ''' <remarks> Adding clearances could implicitely approve additional assemblies. </remarks>
  Public Sub AddClearances(taxonomicDimensionName As String, ParamArray clearanceExpressions() As String)
    Me.TaxonomicDimensions(taxonomicDimensionName).AddClearances(clearanceExpressions)
  End Sub

  ''' <summary>
  '''   Fetches the classification expressions from an assembly and adds them as clearance expressions.
  ''' </summary>
  ''' <remarks> 
  '''   Adding clearances could implicitely approve additional assemblies.
  '''   THIS METHOD DOES NOT APPROVE THE INCOMING ASSEMBLY. IF YOU WANT TO DO THIS YOU MUST USE <seealso cref="AddAssemblyAndImportClearances"/>!
  ''' </remarks>
  ''' <seealso cref="AddAssemblyAndImportClearances"/>
  Public Sub AddClearancesFromAssembly(assembly As Assembly)
    If (Me.EnableTracing) Then
      Trace.TraceInformation(String.Format("AssemblyIndexer: Importing scopevalues of '{0}' to whitelists...", assembly.GetName().Name))
    End If
    Try
      Me.SuspendAutoReapprove()

      For Each dimension In Me.TaxonomicDimensions
        dimension.AddClearancesFromAssembly(assembly.Location)
      Next

    Finally
      Me.ResumeAutoReapprove()
    End Try
  End Sub

#End Region

#Region " dealing with Assemblies "

  ''' <summary>
  '''   Fetches the classification expressions from an assembly and adds them as clearance expressions.
  ''' </summary>
  ''' <remarks> 
  '''   Importing clearances will implicitely approve the incoming assembly PLUS other assemblies that match the newly created clearance situation.
  ''' </remarks>
  Public Overridable Sub AddAssemblyAndImportClearances(moduleEntryAssembly As Assembly)

    If (Me.EnableTracing) Then
      Trace.TraceInformation(String.Format("AssemblyIndexer: Expanding clearance to imply the scope values of assembly '{0}'", moduleEntryAssembly.GetName().Name))
    End If

    Me.AddClearancesFromAssembly(moduleEntryAssembly) 'unlock the scopes (new way over attributes)

    'now lets add the assembly (which will be approved successfully)
    Me.TryApproveAssembly(moduleEntryAssembly.Location, True)

  End Sub

  ''' <summary>
  '''   This will just check, if all assembly's classifications match the current clearance situation.
  '''   This will NOT approve the assembly! (It won't be added to the index).
  ''' </summary>
  ''' <param name="assemblyFullFilename"> The assembly to verify. </param>
  ''' <returns> True, if it's a match. </returns>
  Public Function VerifyAssemblyWithinOneDimension(assemblyFullFilename As String, taxonomicDimensionName As String) As Boolean
    Return Me.TaxonomicDimensions(taxonomicDimensionName).VerifyAssembly(assemblyFullFilename)
  End Function

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
      If (Me.EnableTracing) Then
        Trace.TraceInformation(String.Format("AssemblyIndexer: approving of '{0}' failed because the assembly is on the blacklist for 'external framework components'!", title))
      End If
      Return False 'the assembly is on the blacklist of external framework components
    End If

    Dim matchingResultsPerDimension As New Dictionary(Of String, Boolean)
    For Each dimensionName In _TaxonomicDimensionsByName.Keys

      If (_TaxonomicDimensionsByName(dimensionName).VerifyAssembly(assemblyFullFilename)) Then
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

#End Region

  ''' <summary>
  '''   This can be hoocked to manipulate and break the strict conjunction (AND relation) of the scopes by implementing a custom predicate logic
  ''' </summary>
  Protected Friend Overridable Function SummarizeMatchingResults(matchingResultsPerDimension As Dictionary(Of String, Boolean)) As Boolean

    For Each result In matchingResultsPerDimension.Values
      If (result = False) Then
        Return False
      End If
    Next

    Return True
  End Function

#Region " Blacklist (for components of external Frameworks) "

  'HACK: needs to come from outside!
  Protected Overridable Function IsExternalFrameworkAssembly(assemblyFullFilename As String) As Boolean
    With (Path.GetFileNameWithoutExtension(assemblyFullFilename).ToLower())

      Select Case (True)

        Case .StartsWith("anonymously")
        Case .StartsWith("vshost")

        Case .StartsWith("system.")
        Case .StartsWith("microsoft.")
        Case .StartsWith("mscorlib")

        Case .StartsWith("entityframework")
        Case .StartsWith("newtonsoft")

        Case .StartsWith("webgrease")
        Case .StartsWith("aspnet")
        Case .StartsWith("antlr3")
        Case .StartsWith("owin")

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

#Region " Import / Export "

  Public Function ExportClearances() As String()
    Dim result As New List(Of String)
    For Each dimensionName In Me.TaxonomicDimensionNames
      For Each clearanceExpression In Me.TaxonomicDimensions(dimensionName).Clearances
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
    Me.SuspendAutoReapprove()
    Try

      For Each expression In qualifiedClearanceExpressions
        If (expression.Contains(":")) Then

          Dim dimensionName = expression.Substring(0, expression.IndexOf(":"))
          Dim clearanceExpression = expression.Substring(expression.IndexOf(":") + 1)

          If (Me.TaxonomicDimensionNames.Contains(dimensionName)) Then
            Dim approvalStrategy = Me.TaxonomicDimensions(dimensionName)
            approvalStrategy.AddClearances(clearanceExpression)
          End If

        End If
      Next

    Finally
      Me.ResumeAutoReapprove()
    End Try
  End Sub

#Region " Auto Reapprove on Clearance Change "

  Protected Sub SuspendAutoReapprove()
    _AutoReapproveSuspended = True
  End Sub

  Protected Sub ResumeAutoReapprove()
    _AutoReapproveSuspended = False
    If (_AutoReapproveScheduled) Then
      Me.ReapproveDismissedAssemblies()
    End If
  End Sub

  Private _AutoReapproveSuspended As Boolean = False
  Private _AutoReapproveScheduled As Boolean = False

  Private Sub Dimension_OnClearancesChanged(dimensionName As String, addedClearanceExpressions As String())
    Me.ReapproveDismissedAssemblies()
  End Sub

  Public Overrides Sub ReapproveDismissedAssemblies()
    If (_AutoReapproveSuspended) Then
      _AutoReapproveScheduled = True
    Else
      MyBase.ReapproveDismissedAssemblies()
    End If
  End Sub

#End Region

  ''' <summary>
  '''   Adds clearances to the internal clearance collection which will broaden the set of approvable assemblies.
  ''' </summary>
  ''' <param name="clearancesSortedByDimension">A Dictionary(Of "DimensionName", {"ClearanceExpression1", "ClearanceExpression2"})</param>
  ''' <remarks> Importing clearances could implicitely approve additional assemblies. </remarks>
  Public Sub ImportClearances(clearancesSortedByDimension As Dictionary(Of String, String()))
    Me.SuspendAutoReapprove()
    Try

      For Each dimensionName In clearancesSortedByDimension.Keys
        For Each clearanceExpression In clearancesSortedByDimension(dimensionName)
          If (Me.TaxonomicDimensionNames.Contains(dimensionName)) Then
            Dim approvalStrategy = Me.TaxonomicDimensions(dimensionName)
            approvalStrategy.AddClearances(clearanceExpression)
          End If
        Next
      Next

    Finally
      Me.ResumeAutoReapprove()
    End Try
  End Sub

#End Region

  Protected Overrides Sub Dispose(disposing As Boolean)
    For Each d In Me.TaxonomicDimensions
      RemoveHandler d.ClearancesAdded, AddressOf Me.Dimension_OnClearancesChanged
    Next
    MyBase.Dispose(disposing)
  End Sub

End Class

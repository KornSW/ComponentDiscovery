'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports ComponentDiscovery.ClassificationApproval
Imports ComponentDiscovery.ClassificationDetection

Public Class ClassificationBasedAssemblyIndexer
  Inherits AssemblyIndexer

#Region " Fields & Constructors "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _TaxonomicDimensionsByName As New Dictionary(Of String, TaxonomicDimension)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _ClearanceImportScourceAssemblies As New List(Of Assembly)

  Public Sub New()
    MyClass.New({}, False, False, True)
  End Sub

  Public Sub New(taxonomicDimensionNames As String())
    MyClass.New(taxonomicDimensionNames, False, False, True)
  End Sub

  Public Sub New(
    taxonomicDimensionNames As String(),
    enableResolvePathsBinding As Boolean,
    enableAppDomainBinding As Boolean,
    Optional preferAssemblyLoadingViaFusion As Boolean = True
  )

    MyBase.New(False, False, preferAssemblyLoadingViaFusion)

    For Each taxonomicDimensionName In taxonomicDimensionNames
      Me.AddTaxonomicDimension(taxonomicDimensionName)
    Next

    'this triggers assembly-add
    Me.AppDomainBindingEnabled = enableAppDomainBinding

    'this triggers assembly-add
    Me.ResolvePathsBindingEnabled = enableResolvePathsBinding

  End Sub

#End Region

#Region " Dimension Setup "

  ''' <summary>
  ''' Dimensions to scope the set of available assemblies based on a taxonomy/wording to declare assembly classification expressions
  ''' (independend form other dimensions). At Runtime, the clearances of EACH taxonomic dimension needs to match before an assembly becomes approved.
  ''' </summary>
  <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
  Public ReadOnly Property TaxonomicDimensions As TaxonomicDimension()
    Get
      Return _TaxonomicDimensionsByName.Values.ToArray()
    End Get
  End Property

  ''' <summary>
  ''' Dimensions to scope the set of available assemblies based on a taxonomy/wording to declare assembly classification expressions
  ''' (independend form other dimensions). At Runtime, the clearances of EACH taxonomic dimension needs to match before an assembly becomes approved.
  ''' </summary>
  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Public ReadOnly Property TaxonomicDimensions(dimensionName As String) As TaxonomicDimension
    Get
      Dim lowerDimensionName = dimensionName.ToLower()
      For Each existingName In _TaxonomicDimensionsByName.Keys
        If (existingName.ToLower() = lowerDimensionName) Then
          Return _TaxonomicDimensionsByName(existingName)
        End If
      Next
      Throw New KeyNotFoundException($"There is no TaxonomicDimension registered with name '{dimensionName}'!")
    End Get
  End Property

  ''' <summary>
  ''' Dimensions to scope the set of available assemblies based on a taxonomy/wording to declare assembly classification expressions
  ''' (independend form other dimensions). At Runtime, the clearances of EACH taxonomic dimension needs to match before an assembly becomes approved.
  ''' </summary>
  Public ReadOnly Property TaxonomicDimensionNames As String()
    Get
      Return _TaxonomicDimensionsByName.Keys.ToArray()
    End Get
  End Property

  ''' <summary>
  ''' Declares a new dimension to scope the set of available assemblies based on a taxonomy/wording to declare assembly classification expressions
  ''' (independend form other dimensions). At Runtime, the clearances of EACH taxonomic dimension needs to match before an assembly becomes approved.
  ''' This is a overloaded convenience method which adds a new dimension based on 
  ''' a 'AttributeBasedAssemblyClassificationDetectionStrategy' (which reads the clearances for any assembly out of 
  ''' "AssemblyClassificationAttributes" within the AssemblyInfo) in combination with
  ''' a 'DemandCentricClassificationApprovalStrategy'
  ''' </summary>
  Public Sub AddTaxonomicDimension(taxonomicDimensionName As String, ParamArray defaultClearances() As String)
    Me.ThrowIfDimensionNameAlreadyExists(taxonomicDimensionName)

    If (taxonomicDimensionName.ToLower() = "namespace") Then
      Me.AddTaxonomicDimension(
        taxonomicDimensionName,
        New NamespaceBasedAssemblyClassificationDetectionStrategy(),
        New DemandCentricClassificationApprovalStrategy(),
        defaultClearances
      )
    Else
      Me.AddTaxonomicDimension(
        taxonomicDimensionName,
        New AttributeBasedAssemblyClassificationDetectionStrategy(),
        New DemandCentricClassificationApprovalStrategy(),
        defaultClearances
      )
    End If

  End Sub

  ''' <summary>
  ''' Declares a new dimension to scope the set of available assemblies based on a taxonomy/wording to declare assembly classification expressions
  ''' (independend form other dimensions). At Runtime, the clearances of EACH taxonomic dimension needs to match before an assembly becomes approved.
  ''' This is a overloaded convenience method which adds a new dimension based on
  ''' a 'DelegateBasedClassificationStrategy' in combination with
  ''' a 'DemandCentricClassificationApprovalStrategy'
  ''' </summary>
  ''' <param name="assemblyClassificationEvaluationMethod"> this is how a assembly will be classified. You can specify a 
  ''' method to run your own code evaluating classifications for a given assembly file name into a list of string)</param>
  ''' <param name="classificationApprovalStrategy">this is how a assembly will be approved 
  ''' (opposing the assembly's classifications against the enabled clearances).</param>
  Public Sub AddTaxonomicDimension(
    taxonomicDimensionName As String,
    assemblyClassificationEvaluationMethod As Func(Of String, List(Of String), Boolean),
    classificationApprovalStrategy As IClassificationApprovalStrategy,
    ParamArray defaultClearances() As String
  )

    Me.ThrowIfDimensionNameAlreadyExists(taxonomicDimensionName)

    Me.AddTaxonomicDimension(
      taxonomicDimensionName,
      New DelegateBasedAssemblyClassificationDetectionStrategy(assemblyClassificationEvaluationMethod),
      classificationApprovalStrategy,
      defaultClearances
    )

  End Sub

  ''' <summary>
  ''' Declares a new dimension to scope the set of available assemblies based on a taxonomy/wording to declare assembly classification expressions
  ''' (independend form other dimensions). At Runtime, the clearances of EACH taxonomic dimension needs to match before an assembly becomes approved.
  ''' </summary>
  ''' <param name="taxonomicDimensionName"></param>
  ''' <param name="classificationStrategy">this is how a assembly will be classified. in default you can choose between 
  ''' the 'DelegateBasedClassificationStrategy' (which improves you run your own code returning a string() of classifications) OR 
  ''' the 'AttributeBasedAssemblyClassificationStrategy' (which reads the clearances for any assembly out of 
  ''' "AssemblyClassificationAttributes" within the AssemblyInfo)</param>
  ''' <param name="defaultClearances">enable this clearances by default</param>
  ''' <param name="classificationApprovalStrategy">this is how a assembly will be approved (opposing the assembly's classifications 
  ''' against the enabled clearances).</param>
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

    Me.ThrowIfDimensionNameAlreadyExists(taxonomicDimensionName)

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
    Diag.Verbose(Function() $"AssemblyIndexer: TaxonomicDimension '{taxonomicDimensionName}' was enabled!")

  End Sub

  Public Event TaxonomicDimensionAdded(taxonomicDimensionName As String)

  Protected Sub OnTaxonomicDimensionAdded(taxonomicDimensionName As String)
    If (TaxonomicDimensionAddedEvent IsNot Nothing) Then
      RaiseEvent TaxonomicDimensionAdded(taxonomicDimensionName)
    End If
  End Sub

  Private Sub ThrowIfDimensionNameAlreadyExists(taxonomicDimensionName As String)
    Dim lowerDimensionName = taxonomicDimensionName.ToLower()
    For Each existingDimensionName In _TaxonomicDimensionsByName.Keys
      If (existingDimensionName.ToLower() = lowerDimensionName) Then
        Throw New Exception("This dimensionName is already registered!")
      End If
    Next
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
  Public Function AddClearances(taxonomicDimensionName As String, ParamArray clearanceExpressions() As String) As Boolean
    Dim newClearencesAdded = Me.TaxonomicDimensions(taxonomicDimensionName).AddClearances(clearanceExpressions)
    If (newClearencesAdded) Then
      Me.OnReapproveDismissedAssembliesRequired()
    End If
    Return newClearencesAdded
  End Function

  ''' <summary>
  '''   Fetches the classification expressions from an assembly and adds them as clearance expressions.
  ''' </summary>
  ''' <remarks> 
  '''   Adding clearances could implicitely approve additional assemblies.
  '''   THIS METHOD DOES NOT APPROVE THE INCOMING ASSEMBLY. IF YOU WANT TO DO THIS YOU MUST USE <seealso cref="AddAssemblyAndImportClearances"/>!
  ''' </remarks>
  ''' <seealso cref="AddAssemblyAndImportClearances"/>
  Public Function AddClearancesFromAssembly(assembly As Assembly) As Boolean
    Diag.Info($"AssemblyIndexer: Importing scopevalues of '{assembly.GetName().Name}' to whitelists...")

    SyncLock _ClearanceImportScourceAssemblies
      _ClearanceImportScourceAssemblies.Add(assembly)
    End SyncLock

    Dim newClearencesAdded = False

    'Try
    'Me.SuspendAutoReapprove()

    For Each dimension In Me.TaxonomicDimensions
      newClearencesAdded = newClearencesAdded Or dimension.AddClearancesFromAssembly(assembly.Location)
    Next

    If (newClearencesAdded) Then
      Me.OnReapproveDismissedAssembliesRequired()
    End If

    'Finally
    '  Me.ResumeAutoReapprove()
    'End Try

    Return newClearencesAdded
  End Function

#End Region

#Region " Auto Reapprove Suspension (for better Performance) "

  <ThreadStatic>
  Private _SuspendAutoReapproveOnClearanceAdded As Boolean = False

  <ThreadStatic>
  Private _ClearanceHaveBeenAddedDuringSuspendedAutoReapprove As Boolean = False

  ''' <summary>
  ''' Invoke the given delegate without automatically calling 'ReapproveDismissedAssemblies()' when clearances were added.
  ''' After the given delegate has been invoked, the 'ReapproveDismissedAssemblies()' will be called exact once (if necessary).
  ''' The return value is true, if one or more clearances were added during the invoke.
  ''' </summary>
  Public Function ExecuteWithoutAutoReapprove(method As Action) As Boolean

    'protects the situation of a cascaded call of 'ExecuteWithoutAutoReapprove'
    If (Me.SuspendAutoReapproveOnClearanceAdded) Then
      method.Invoke()
      Return _ClearanceHaveBeenAddedDuringSuspendedAutoReapprove
    End If

    Try
      Me.SuspendAutoReapproveOnClearanceAdded = True
      method.Invoke()
      Return _ClearanceHaveBeenAddedDuringSuspendedAutoReapprove
    Finally
      Me.SuspendAutoReapproveOnClearanceAdded = False
    End Try

  End Function

  Private Property SuspendAutoReapproveOnClearanceAdded As Boolean
    Get
      Return _SuspendAutoReapproveOnClearanceAdded
    End Get
    Set(value As Boolean)
      If (Not _SuspendAutoReapproveOnClearanceAdded = value) Then
        _SuspendAutoReapproveOnClearanceAdded = value
        If (_SuspendAutoReapproveOnClearanceAdded = False AndAlso _ClearanceHaveBeenAddedDuringSuspendedAutoReapprove) Then
          Me.ReapproveDismissedAssemblies()
        End If
        _ClearanceHaveBeenAddedDuringSuspendedAutoReapprove = False
      End If
    End Set
  End Property

  Protected Sub OnReapproveDismissedAssembliesRequired()
    If (_SuspendAutoReapproveOnClearanceAdded) Then
      _ClearanceHaveBeenAddedDuringSuspendedAutoReapprove = True
    Else
      Me.ReapproveDismissedAssemblies()
    End If
  End Sub

#End Region
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

#Region " dealing with Assemblies "

  ''' <summary>
  '''   Fetches the classification expressions from an assembly and adds them as clearance expressions.
  ''' </summary>
  ''' <remarks> 
  '''   Importing clearances will implicitely approve the incoming assembly PLUS other assemblies that match the newly created clearance situation.
  ''' </remarks>
  Public Overridable Sub AddAssemblyAndImportClearances(disclosingAssembly As Assembly)

    Diag.Verbose(Function() $"AssemblyIndexer: Expanding clearance to imply the scope values of assembly '{disclosingAssembly.GetName().Name}'")
    Me.AddClearancesFromAssembly(disclosingAssembly) 'unlock the scopes (new way over attributes)

    'now lets add the assembly (which will be approved successfully)
    Me.TryApproveAssembly(disclosingAssembly.Location, True)

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

      Diag.Verbose(
        Function() $"AssemblyIndexer: approving of '{title}' failed because the assembly is on the blacklist for 'external framework components'!"
      )

      Return False 'the assembly is on the blacklist of external framework components

    End If

    Dim matchingResultsPerDimension As New Dictionary(Of String, Boolean)

    'dont ask why - weve had some strange constellation in which 
    'our constructor was calling MyBase.New() and this was calling VerifyAssembly
    'and we came here BEFORE the initializer in line 22 has been executed WTF!
    If (_TaxonomicDimensionsByName Is Nothing) Then
      _TaxonomicDimensionsByName = New Dictionary(Of String, TaxonomicDimension)
    End If

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

  Public Function DumpFullState() As String
    Dim result As New StringBuilder

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

    result.AppendLine("#### CLEARANCES ###")
    result.AppendLine(Me.DumpClearances(False))

    result.AppendLine("#### FULLY IMPORTED DIRECTORIES ###")
    For Each p In Me.FullyImportedDirectories
      result.AppendLine(p)
    Next
    result.AppendLine()

    result.AppendLine("#### DISCLOSING ASSEMBLIES ###")
    Dim disclosingAssemblies As Assembly() = Me.ClearanceImportScourceAssemblies
    For Each a In disclosingAssemblies
      result.Append(a.Location)
      result.Append(New String(" "c, indent - a.Location.Length))
      result.Append("(")
      Me.DumpClassificationsForAssembly(a.Location, result)
      result.AppendLine(" )")
    Next
    result.AppendLine()

    result.AppendLine("#### APPROVED ASSEMBLIES ###")
    For Each a In Me.ApprovedAssemblies.Except(disclosingAssemblies)
      result.Append(a.Location)
      result.Append(New String(" "c, indent - a.Location.Length))
      result.Append("(")
      Me.DumpClassificationsForAssembly(a.Location, result)
      result.AppendLine(" )")
    Next
    result.AppendLine()

    result.AppendLine("#### DISMISSED ASSEMBLIES ###")
    For Each a In Me.DismissedAssemblies
      result.Append(a)
      result.Append(New String(" "c, indent - a.Length))
      result.Append("(")
      Me.DumpClassificationsForAssembly(a, result)
      result.AppendLine(" )")
    Next

    Return result.ToString()
  End Function

  Public Function DumpClassificationsForAssembly(assemblyFileFullName As String) As String
    Dim result As New StringBuilder
    Me.DumpClassificationsForAssembly(assemblyFileFullName, result)
    Return result.ToString()
  End Function

  Public Sub DumpClassificationsForAssembly(assemblyFileFullName As String, target As StringBuilder)
    Dim first As Boolean = True
    For Each dimensionName In Me.TaxonomicDimensionNames
      Dim strat = Me.TaxonomicDimensions(dimensionName).AssemblyClassificationDetectionStrategy
      If (first) Then
        first = False
      Else
        target.Append(" | ")
      End If
      target.Append(dimensionName)
      target.Append(":"c)
      Dim classifications As String() = Nothing
      If (strat.TryDetectClassificationsForAssembly(assemblyFileFullName, dimensionName, classifications)) Then
        For Each classification In classifications
          target.Append(" "c)
          target.Append(classification)
        Next
      End If
    Next
  End Sub

  Public ReadOnly Property ClearanceImportScourceAssemblies As Assembly()
    Get
      SyncLock _ClearanceImportScourceAssemblies
        Return _ClearanceImportScourceAssemblies.ToArray()
      End SyncLock
    End Get
  End Property

  Public Function DumpClearances(Optional oneLinePerClearance As Boolean = False) As String
    Dim result As New StringBuilder
    For Each dimensionName In Me.TaxonomicDimensionNames
      If (Not oneLinePerClearance) Then
        result.Append(dimensionName + ":")
      End If
      For Each clearanceExpression In Me.TaxonomicDimensions(dimensionName).Clearances
        If (oneLinePerClearance) Then
          result.AppendLine(String.Format("{0}: {1}", dimensionName, clearanceExpression))
        Else
          result.Append(" " + clearanceExpression)
        End If
      Next
      If (Not oneLinePerClearance) Then
        result.AppendLine()
      End If
    Next
    Return result.ToString()
  End Function

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
  Public Overridable Sub ImportClearances(qualifiedClearanceExpressions As String())

    If ((qualifiedClearanceExpressions IsNot Nothing) AndAlso (qualifiedClearanceExpressions.Any)) Then

      Me.ExecuteWithoutAutoReapprove(
          Sub()

            Dim added As Boolean = False
            For Each expression In qualifiedClearanceExpressions
              If (expression.Contains(":")) Then

                Dim dimensionName = expression.Substring(0, expression.IndexOf(":"))
                Dim clearanceExpression = expression.Substring(expression.IndexOf(":") + 1)

                If (Me.TaxonomicDimensionNames.Contains(dimensionName)) Then
                  Dim dimension = Me.TaxonomicDimensions(dimensionName)
                  added = added Or dimension.AddClearances(clearanceExpression)
                End If

              End If
            Next
            If (added) Then
              Me.OnReapproveDismissedAssembliesRequired()
            End If

          End Sub
        )

    End If

  End Sub

  ''' <summary>
  '''   Adds clearances to the internal clearance collection which will broaden the set of approvable assemblies.
  ''' </summary>
  ''' <param name="clearancesSortedByDimension">A Dictionary(Of "DimensionName", {"ClearanceExpression1", "ClearanceExpression2"})</param>
  ''' <remarks> Importing clearances could implicitely approve additional assemblies. </remarks>
  Public Overridable Sub ImportClearances(clearancesSortedByDimension As Dictionary(Of String, String()))

    Me.ExecuteWithoutAutoReapprove(
      Sub()

        Dim added As Boolean = False
        For Each dimensionName In clearancesSortedByDimension.Keys
          For Each clearanceExpression In clearancesSortedByDimension(dimensionName)
            If (Me.TaxonomicDimensionNames.Contains(dimensionName)) Then
              Dim dimension = Me.TaxonomicDimensions(dimensionName)
              dimension.AddClearances(clearanceExpression)
              added = added Or dimension.AddClearances(clearanceExpression)
            End If
          Next
        Next
        If (added) Then
          Me.OnReapproveDismissedAssembliesRequired()
        End If

      End Sub
  )

  End Sub

#End Region

  Protected Overrides Sub Dispose(disposing As Boolean)
    For Each d In Me.TaxonomicDimensions
      RemoveHandler d.ClearancesAdded, AddressOf Me.Dimension_OnClearancesChanged
    Next
    MyBase.Dispose(disposing)
  End Sub

End Class

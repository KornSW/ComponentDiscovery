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

Partial Class ClassificationBasedAssemblyIndexer

  <DebuggerDisplay("TaxonomicDimension '{DimensionName}'")>
  Public NotInheritable Class TaxonomicDimension

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _DimensionName As String

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _AssemblyClassificationStrategy As IAssemblyClassificationDetectionStrategy

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _ClassificationApprovalStrategy As IClassificationApprovalStrategy

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _ClassificationExpressionsPerAssembly As New Dictionary(Of String, String())

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _ClearanceExpressions As New List(Of String)

    Public Sub New(
      dimensionName As String,
      assemblyClassificationStrategy As IAssemblyClassificationDetectionStrategy,
      classificationApprovalStrategy As IClassificationApprovalStrategy
    )

      _DimensionName = dimensionName
      _AssemblyClassificationStrategy = assemblyClassificationStrategy
      _ClassificationApprovalStrategy = classificationApprovalStrategy

    End Sub

    Public ReadOnly Property DimensionName As String
      Get
        Return _DimensionName
      End Get
    End Property

    Public ReadOnly Property AssemblyClassificationDetectionStrategy As IAssemblyClassificationDetectionStrategy
      Get
        Return _AssemblyClassificationStrategy
      End Get
    End Property

    Public ReadOnly Property ClassificationApprovalStrategy As IClassificationApprovalStrategy
      Get
        Return _ClassificationApprovalStrategy
      End Get
    End Property

    Public ReadOnly Property Clearances As String()
      Get
        Return _ClearanceExpressions.ToArray()
      End Get
    End Property

    Public Delegate Sub ClearancesAddedEventHandler(dimensionName As String, addedClearanceExpressions() As String)

    ''' <summary>
    '''   Occurs when clearance expressions have actually been added to the ClearanceLabels collection.
    ''' </summary>
    ''' <param name="addedClearanceExpressions"> An array of the effectively added labels. </param>
    Public Event ClearancesAdded As ClearancesAddedEventHandler

    Protected Sub OnClearancesAdded(addedExpressions() As String)
      If (ClearancesAddedEvent IsNot Nothing AndAlso addedExpressions.Length > 0) Then
        RaiseEvent ClearancesAdded(_DimensionName, addedExpressions)
      End If
    End Sub

    ''' <summary>
    '''   Adds the classification expressions of an assembly to the ClearanceLabels collection.
    '''   This will instantly make the assembly approvable.
    ''' </summary>
    ''' <returns>
    '''   True, if at least one new expression has actually been added to the ClearanceLabels collection.
    ''' </returns>
    Public Function AddClearancesFromAssembly(assemblyFullFilename As String) As Boolean
      Dim classificationExpressions = Me.GetClassificationsWithRuntimeCaching(assemblyFullFilename)
      If (classificationExpressions Is Nothing) Then
        Return False
      End If
      Return Me.AddClearances(classificationExpressions)
    End Function

    ''' <summary>
    '''   Adds further labels to the ClearanceLabels collection. Duplicates will be ignored.
    '''   Expanding the clearance labels collection will broaden the set of approvable assemblies.
    ''' </summary>
    ''' <returns>
    '''   True, if at least one new expression has actually been added to the ClearanceLabels collection.
    ''' </returns>
    Public Function AddClearances(ParamArray addingExpressions() As String) As Boolean
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

      If (newExpressionsDetected) Then
        Me.OnClearancesAdded(addedExpressions.ToArray())
      End If

      Return newExpressionsDetected
    End Function

    ''' <summary>
    ''' </summary>
    ''' <param name="assemblyFullFilename"></param>
    ''' <returns>CAN BE NOTHING !</returns>
    Protected Function GetClassificationsWithRuntimeCaching(assemblyFullFilename As String) As String()

      SyncLock _ClassificationExpressionsPerAssembly
        If (_ClassificationExpressionsPerAssembly.ContainsKey(assemblyFullFilename)) Then
          Return _ClassificationExpressionsPerAssembly(assemblyFullFilename)
        End If
      End SyncLock

      Dim classificationExpressions As String() = Nothing

      Dim successfullyDetected = Me.AssemblyClassificationDetectionStrategy.TryDetectClassificationsForAssembly(
        assemblyFullFilename,
        Me.DimensionName,
        classificationExpressions
      )

      If (Not successfullyDetected) Then
        classificationExpressions = Nothing
      End If

      SyncLock _ClassificationExpressionsPerAssembly
        _ClassificationExpressionsPerAssembly.Add(assemblyFullFilename, classificationExpressions)
      End SyncLock

      Return classificationExpressions
    End Function

    Public Function VerifyAssembly(assemblyFullFilename As String) As Boolean
      Dim classificationExpressions = Me.GetClassificationsWithRuntimeCaching(assemblyFullFilename)

      If (classificationExpressions Is Nothing) Then
        'if there was an error during the evaluation, the assembly will be handled as un-marked
        classificationExpressions = New String() {}
      End If

      Return Me.ClassificationApprovalStrategy.VerifyTarget(classificationExpressions, Me.Clearances, Me.DimensionName)
    End Function

  End Class

End Class

'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports ComponentDiscovery.ClassificationDetection

Public Class ClassificationBasedTypeIndexer
  Inherits TypeIndexer

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _TaxonomicDimensionsByName As New Dictionary(Of String, TaxonomicDimension)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _TypeClassificationStrategyFactory As Func(Of String, ITypeClassificationDetectionStrategy)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _DismissedTypes As New List(Of Type)

  Private Shared Function DefaultTypeClassificationStrategyFactory(dimensionName As String) As ITypeClassificationDetectionStrategy
    Return New AttributeBasedTypeClassificationDetectionStrategy() 'used for all dimensions
  End Function

  Public Sub New(assemblyIndexer As ClassificationBasedAssemblyIndexer)
    MyClass.New(assemblyIndexer, AddressOf DefaultTypeClassificationStrategyFactory)
  End Sub

  Public Sub New(assemblyIndexer As ClassificationBasedAssemblyIndexer, typeClassificationStrategyFactory As Func(Of String, ITypeClassificationDetectionStrategy))

    'mybase.enablePersistentCache=False because the doens not support multiple results for differenct classifications!
    MyBase.New(assemblyIndexer)

    _TypeClassificationStrategyFactory = typeClassificationStrategyFactory

    For Each dimensionName In assemblyIndexer.TaxonomicDimensionNames
      Me.WrapTaxonomicDimensionNames(dimensionName)
    Next

    AddHandler Me.AssemblyIndexer.TaxonomicDimensionAdded, AddressOf Me.WrapTaxonomicDimensionNames

  End Sub

  Protected Sub WrapTaxonomicDimensionNames(dimensionName As String)
    If (Not _TaxonomicDimensionsByName.ContainsKey(dimensionName)) Then
      Dim classificationStrategy = _TypeClassificationStrategyFactory.Invoke(dimensionName)
      Dim innerDimension = Me.AssemblyIndexer.TaxonomicDimensions(dimensionName)
      Dim newDimensionWrapper = New TaxonomicDimension(innerDimension, classificationStrategy)
      _TaxonomicDimensionsByName.Add(dimensionName, newDimensionWrapper)

      AddHandler newDimensionWrapper.ClearancesAdded, AddressOf Me.Dimension_OnClearancesChanged

    End If
  End Sub

  Public Shadows ReadOnly Property AssemblyIndexer As ClassificationBasedAssemblyIndexer
    Get
      Return DirectCast(MyBase.AssemblyIndexer, ClassificationBasedAssemblyIndexer)
    End Get
  End Property

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
      Throw New KeyNotFoundException($"There is no TaxonomicDimension registered with name '{dimensionName}'!")
    End Get
  End Property

  Public ReadOnly Property TaxonomicDimensionNames As String()
    Get
      Return _TaxonomicDimensionsByName.Keys.ToArray()
    End Get
  End Property

  Private Sub Dimension_OnClearancesChanged(dimensionName As String, addedClearanceExpressions As String())

    Dim typesToReapprove = _DismissedTypes.ToArray()
    _DismissedTypes.Clear()

    For Each typeToReapprove In typesToReapprove
      If (Me.DefaultApprovingMethod(typeToReapprove)) Then
        For Each index In Me.ApplicablesPerSelector
          index.TryRegisterCandidate(typeToReapprove, skipExternalApproving:=True)
        Next
      End If
    Next

  End Sub

  Protected Overrides Function DefaultApprovingMethod(t As Type) As Boolean

    If (MyBase.DefaultApprovingMethod(t)) Then

      If (_DismissedTypes.Contains(t)) Then
        Return False
      End If

      Dim matchingResultsPerDimension As New Dictionary(Of String, Boolean)
      For Each dimensionName In _TaxonomicDimensionsByName.Keys
        If (_TaxonomicDimensionsByName(dimensionName).VerifyType(t)) Then
          matchingResultsPerDimension.Add(dimensionName, True)
        Else
          matchingResultsPerDimension.Add(dimensionName, False)
        End If
      Next

      If (Me.AssemblyIndexer.SummarizeMatchingResults(matchingResultsPerDimension)) Then
        Return True
      Else
        _DismissedTypes.Add(t)
        Return False
      End If

    End If

    Return False
  End Function

  Protected Overrides Sub Dispose(disposing As Boolean)
    For Each d In Me.TaxonomicDimensions
      RemoveHandler d.ClearancesAdded, AddressOf Me.Dimension_OnClearancesChanged
    Next
    RemoveHandler Me.AssemblyIndexer.TaxonomicDimensionAdded, AddressOf Me.WrapTaxonomicDimensionNames
    MyBase.Dispose(disposing)
  End Sub

End Class

'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports ComponentDiscovery.ClassificationApproval
Imports ComponentDiscovery.ClassificationDetection

Namespace ComponentDiscovery

  Partial Class ClassificationBasedTypeIndexer

    <DebuggerDisplay("TaxonomicDimension '{DimensionName}'")>
    Public NotInheritable Class TaxonomicDimension

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _AssemblyLevelTaxonomicDimension As ClassificationBasedAssemblyIndexer.TaxonomicDimension

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _TypeClassificationStrategy As ITypeClassificationDetectionStrategy

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _ClassificationExpressionsPerType As New Dictionary(Of Type, String())

      Public Sub New(assemblyLevelTaxonomicDimension As ClassificationBasedAssemblyIndexer.TaxonomicDimension, typeClassificationStrategy As ITypeClassificationDetectionStrategy)
        _AssemblyLevelTaxonomicDimension = assemblyLevelTaxonomicDimension
        _TypeClassificationStrategy = typeClassificationStrategy
      End Sub

      Public ReadOnly Property DimensionName As String
        Get
          Return _AssemblyLevelTaxonomicDimension.DimensionName
        End Get
      End Property

      Public ReadOnly Property TypeClassificationStrategy As ITypeClassificationDetectionStrategy
        Get
          Return _TypeClassificationStrategy
        End Get
      End Property

      Public ReadOnly Property ClassificationApprovalStrategy As IClassificationApprovalStrategy
        Get
          Return _AssemblyLevelTaxonomicDimension.ClassificationApprovalStrategy
        End Get
      End Property

      Public ReadOnly Property Clearances As String()
        Get
          Return _AssemblyLevelTaxonomicDimension.Clearances
        End Get
      End Property

      ''' <summary>
      '''   Occurs when clearance expressions have actually been added to the ClearanceLabels collection.
      ''' </summary>
      ''' <param name="addedClearanceExpressions"> An array of the effectively added labels. </param>
      Public Custom Event ClearancesAdded As ClassificationBasedAssemblyIndexer.TaxonomicDimension.ClearancesAddedEventHandler
        AddHandler(value As ClassificationBasedAssemblyIndexer.TaxonomicDimension.ClearancesAddedEventHandler)
          AddHandler _AssemblyLevelTaxonomicDimension.ClearancesAdded, value
        End AddHandler
        RemoveHandler(value As ClassificationBasedAssemblyIndexer.TaxonomicDimension.ClearancesAddedEventHandler)
          RemoveHandler _AssemblyLevelTaxonomicDimension.ClearancesAdded, value
        End RemoveHandler
        RaiseEvent()
          'clearance handling can only be done by the assemblyindexer, so we will never need to fire this event from here!
        End RaiseEvent
      End Event

      ''' <summary>
      ''' </summary>
      ''' <param name="t"></param>
      ''' <returns>CAN BE NOTHING !</returns>
      Protected Function GetClassificationsWithRuntimeCaching(t As Type) As String()

        SyncLock _ClassificationExpressionsPerType
          If (_ClassificationExpressionsPerType.ContainsKey(t)) Then
            Dim expr = _ClassificationExpressionsPerType(t)
            Return _ClassificationExpressionsPerType(t)
          End If
        End SyncLock

        Dim classificationExpressions As String() = Nothing

        If (Not Me.TypeClassificationStrategy.TryDetectClassificationsForType(t, Me.DimensionName, classificationExpressions)) Then
          classificationExpressions = Nothing
        End If

        SyncLock _ClassificationExpressionsPerType
          _ClassificationExpressionsPerType.Add(t, classificationExpressions)
        End SyncLock

        Return classificationExpressions
      End Function

      Public Function VerifyType(t As Type) As Boolean
        Dim classificationExpressions = Me.GetClassificationsWithRuntimeCaching(t)

        'fallback to assembly clearances
        If (classificationExpressions Is Nothing) Then
          'assembly clearances have already been evauated!
          Return True
        End If

        Return Me.ClassificationApprovalStrategy.VerifyTarget(classificationExpressions, Me.Clearances, Me.DimensionName)
      End Function

    End Class

  End Class

End Namespace

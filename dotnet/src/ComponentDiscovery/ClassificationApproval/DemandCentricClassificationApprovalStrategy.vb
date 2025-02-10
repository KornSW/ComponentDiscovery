'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq

Namespace ComponentDiscovery.ClassificationApproval

  ''' <summary>
  ''' A 'ClassificationApprovalStrategy' opposes classifications with clearances and returns true if conformity is given.
  ''' This one does a 'demand based' evaluation, which requires at least one clearance for EACH classification to reach conformity!
  ''' </summary>
  Public Class DemandCentricClassificationApprovalStrategy
    Inherits ClassificationApprovalStrategyBase
    Implements IClassificationApprovalStrategy

#Region " Constructors "

    Public Sub New()
      MyBase.New()
    End Sub

    ''' <summary>
    ''' </summary>
    ''' <param name="resultIfNoClassifications">what should be done with unclassified targets? (default=False)</param>
    ''' <param name="enableRecursionForArborescentClassifications">
    ''' activate a logic which is similar to 'namespaces':  
    ''' if we have a classification "Foo.Bar.XYZ" then it will also pass the approval
    ''' for a clearance like "Foo.Bar" or "Foo", but not for a clearance like "Foo.Bar.ABC"
    ''' </param>
    Public Sub New(resultIfNoClassifications As Boolean, enableRecursionForArborescentClassifications As Boolean)
      MyBase.New(resultIfNoClassifications, enableRecursionForArborescentClassifications)
    End Sub

#End Region

    ''' <summary>
    ''' Opposes classifications with clearances and returns true if conformity is given.
    ''' </summary>
    Public Function VerifyTarget(classificationExpressions() As String, clearanceExpressions() As String, taxonomicDimensionName As String) As Boolean Implements IClassificationApprovalStrategy.VerifyTarget

      If (classificationExpressions.Length = 0) Then
        Return Me.ResultIfNoClassifications
      End If

      Dim anyClearenceLocked As Boolean = False

      For Each classification In classificationExpressions

        Dim resultForCurrentClearanceExpression = Me.RequireClearanceForConcreteClassification(
          classification,
          clearanceExpressions
        )

        'ww are 'demand based', which requires at least one clearance for EACH classification to reach conformity!

        Select Case resultForCurrentClearanceExpression
          Case EvaluationConclusion.Dismissed
            Return False
          Case EvaluationConclusion.Unlocked
            'REQUIRED
          Case EvaluationConclusion.Inconclusive
            If (Not classification.StartsWith(NegationChar)) Then
              Return False
            End If
        End Select

      Next

      Return True
    End Function

  End Class

End Namespace

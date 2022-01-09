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
  ''' This one does a 'expression based' evaluation, which requires conformity with each clearance and supports extended clearance 
  ''' expressions like AND-Reations('Foo+Bar') or NOT-Operators('!For').
  ''' </summary>
  Public Class ExpressionCentricClassificationApprovalStrategy
    Inherits ClassificationApprovalStrategyBase
    Implements IClassificationApprovalStrategy

    Protected Const AndChar As Char = "+"c

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

      Dim unlocked As Boolean = False

      For Each clearanceExpression In clearanceExpressions

        Dim resultForCurrentClearanceExpression = Me.RequireClassificationForAndLinkedClearances(
          clearanceExpression.Split(AndChar),
          classificationExpressions
        )

        'in the "expression based" iterpretation, ALL classifications have to be unlocked!!!
        Select Case resultForCurrentClearanceExpression
          Case EvaluationConclusion.Dismissed
            Return False
          Case EvaluationConclusion.Unlocked
            unlocked = True
        End Select

      Next

      Return unlocked
    End Function

    Protected Function RequireClassificationForAndLinkedClearances(andLinkedClearances As String(), allAvailableClassifications As String()) As EvaluationConclusion

      Dim dismissed As Boolean = False
      Dim unlocked As Boolean = False

      For Each clearance In andLinkedClearances
        Select Case (Me.RequireClassificationForConcreteClearance(clearance, allAvailableClassifications))
          Case EvaluationConclusion.Dismissed
            dismissed = True
          Case EvaluationConclusion.Unlocked
            unlocked = True
          Case EvaluationConclusion.Inconclusive
            'the whole and-block will only have affect, if ALL expressions have a result
            Return EvaluationConclusion.Inconclusive
        End Select
      Next

      If (dismissed) Then
        Return EvaluationConclusion.Dismissed
      ElseIf (unlocked) Then
        Return EvaluationConclusion.Unlocked
      Else
        Return EvaluationConclusion.Inconclusive
      End If

    End Function

  End Class

End Namespace

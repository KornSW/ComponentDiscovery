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

  Public MustInherit Class ClassificationApprovalStrategyBase

    Protected Enum EvaluationConclusion As Integer
      Dismissed = -1
      Inconclusive = 0
      Unlocked = 1
    End Enum

    Protected Const HierarchySeparatorChar As Char = "."c
    Protected Const NegationChar As Char = "!"c

#Region " Fields / Constructors / Properties "

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _ResultIfNoClassifications As Boolean

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _EnableRecursionForArborescentClassifications As Boolean

    Public Sub New()
      MyClass.New(False, False)
    End Sub

    ''' <summary>
    ''' </summary>
    ''' <param name="resultIfNoClassifications">waht should be done with unclassified targets? (default=False)</param>
    ''' <param name="enableRecursionForArborescentClassifications">Activate a logic which is similar to Namespaces - 
    ''' if we have a target with classification "Foo.Bar.Car"
    ''' then it will also pass the approval for a clearance "Foo.Bar" or "Foo", but not for a clearance like "Foo.Bar.Bike" </param>
    Public Sub New(resultIfNoClassifications As Boolean, enableRecursionForArborescentClassifications As Boolean)
      _ResultIfNoClassifications = resultIfNoClassifications
      _EnableRecursionForArborescentClassifications = enableRecursionForArborescentClassifications
    End Sub

    Public ReadOnly Property ResultIfNoClassifications As Boolean
      Get
        Return _ResultIfNoClassifications
      End Get
    End Property

    Public ReadOnly Property EnableRecursionForArborescentClassifications As Boolean
      Get
        Return _EnableRecursionForArborescentClassifications
      End Get
    End Property

#End Region

    Protected Function RequireClassificationForConcreteClearance(clearance As String, allAvailableClassifications As String()) As EvaluationConclusion

      Dim dismissed As Boolean = False
      Dim unlocked As Boolean = False

      For Each classificationExpression In allAvailableClassifications
        Select Case (Me.OpposeTags(clearance, classificationExpression))
          Case EvaluationConclusion.Dismissed
            dismissed = True
            Exit For
          Case EvaluationConclusion.Unlocked
            unlocked = True
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

    Protected Function RequireClearanceForConcreteClassification(classificationExpression As String, allAvailableClearances As String()) As EvaluationConclusion

      Dim dismissed As Boolean = False
      Dim unlocked As Boolean = False

      For Each clearance In allAvailableClearances
        Select Case (Me.OpposeTags(clearance, classificationExpression))
          Case EvaluationConclusion.Dismissed
            dismissed = True
            Exit For
          Case EvaluationConclusion.Unlocked
            unlocked = True
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

    Protected Function OpposeTags(clearanceTag As String, classificationTag As String) As EvaluationConclusion

      Dim isDenyingClearance As Boolean = clearanceTag.StartsWith(NegationChar)
      If (isDenyingClearance) Then
        clearanceTag = clearanceTag.Substring(1)
      End If

      Dim isNegatedClassification As Boolean = classificationTag.StartsWith(NegationChar)
      If (isNegatedClassification) Then
        classificationTag = classificationTag.Substring(1)
      End If

      If (isNegatedClassification AndAlso isDenyingClearance) Then
        Return EvaluationConclusion.Inconclusive
      End If

      Dim oneSideIsNegated = (isDenyingClearance OrElse isNegatedClassification)

      'if we have a simple match
      If (String.Equals(classificationTag, clearanceTag, StringComparison.CurrentCultureIgnoreCase)) Then
        If (oneSideIsNegated) Then
          Return EvaluationConclusion.Dismissed
        Else
          Return EvaluationConclusion.Unlocked
        End If
      End If

      'advanced matches
      If (_EnableRecursionForArborescentClassifications) Then
        If (classificationTag.IsSubPathOf(clearanceTag, HierarchySeparatorChar)) Then

          If (oneSideIsNegated) Then
            Return EvaluationConclusion.Dismissed
          Else
            Return EvaluationConclusion.Unlocked
          End If
        End If
        If (clearanceTag.IsSubPathOf(classificationTag, HierarchySeparatorChar)) Then

          If (oneSideIsNegated) Then
            Return EvaluationConclusion.Dismissed
          Else
            Return EvaluationConclusion.Inconclusive
          End If
        End If
      End If

      Return EvaluationConclusion.Inconclusive
    End Function

  End Class

End Namespace

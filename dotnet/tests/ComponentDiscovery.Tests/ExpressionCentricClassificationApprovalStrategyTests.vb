'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports ComponentDiscovery
Imports ComponentDiscovery.ClassificationApproval
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()>
Public Class ExpressionCentricClassificationApprovalStrategyTests

  <TestMethod()>
  Public Sub ExpressionCentricClassificationApprovalStrategyTest()

    Dim strategy As New ExpressionCentricClassificationApprovalStrategy(False, True)

    Dim classifications As String()
    classifications = {"A", "B", "C.x"}

    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A"}, String.Empty))
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A", "B", "C"}, String.Empty))
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A", "B", "C", "D"}, String.Empty))
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A", "B", "C.x"}, String.Empty))
    'Assert.IsFalse(strategy.VerifyTarget(classifications, {"A", "B", "C.y"}, String.Empty))
    'Assert.IsFalse(strategy.VerifyTarget(classifications, {"A.q", "B", "C"}, String.Empty))
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A", "B", "C", "!D"}, String.Empty))
    Assert.IsFalse(strategy.VerifyTarget(classifications, {"!A", "B", "C"}, String.Empty))
    Assert.IsFalse(strategy.VerifyTarget(classifications, {"A", "B", "!C.x"}, String.Empty))
    'Assert.IsFalse(strategy.VerifyTarget(classifications, {"A", "B", "!C.y"}, String.Empty))
    Assert.IsFalse(strategy.VerifyTarget(classifications, {"A", "B", "!C"}, String.Empty))

    classifications = {"A", "!B"}
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A", "!B"}, String.Empty))
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A"}, String.Empty))
    Assert.IsFalse(strategy.VerifyTarget(classifications, {"A", "B"}, String.Empty))
    Assert.IsFalse(strategy.VerifyTarget(classifications, {"!A"}, String.Empty))
    Assert.IsFalse(strategy.VerifyTarget(classifications, {"!B"}, String.Empty))

    classifications = {"A", "!B.x"}
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A"}, String.Empty))
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A", "!B"}, String.Empty))
    Assert.IsFalse(strategy.VerifyTarget(classifications, {"A", "B"}, String.Empty))
    Assert.IsFalse(strategy.VerifyTarget(classifications, {"A", "B.x"}, String.Empty))
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A", "B.y"}, String.Empty))
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A", "!B.x"}, String.Empty))
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A", "!B.y"}, String.Empty))

    classifications = {"A", "B"}
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A", "B", "C"}, String.Empty))
    Assert.IsTrue(strategy.VerifyTarget(classifications, {"A+B", "C"}, String.Empty))
    'Assert.IsFalse(strategy.VerifyTarget(classifications, {"A", "B+C"}, String.Empty))

  End Sub

End Class

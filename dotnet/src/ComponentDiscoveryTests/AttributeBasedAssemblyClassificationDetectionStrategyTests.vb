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
Imports ComponentDiscovery.ClassificationDetection
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()>
Public Class AttributeBasedAssemblyClassificationDetectionStrategyTests

  <TestMethod()>
  Public Sub ExpressionCentricClassificationApprovalStrategyTest()
    Dim useCache As Boolean = True
    Dim useSandbox As Boolean = False
    Dim strat As New AttributeBasedAssemblyClassificationDetectionStrategy(useSandbox, useCache, "Fallback")

    Dim assemblyFileFullName As String = Assembly.GetExecutingAssembly().Location

    Dim classifications As String() = Nothing
    Dim succes = strat.TryDetectClassificationsForAssembly(assemblyFileFullName, "BusinessConcern", classifications)

    Assert.AreEqual(2, classifications.Length)
    Assert.AreEqual("ConcernA", classifications(0))
    Assert.AreEqual("ConcernB", classifications(1))

  End Sub

End Class

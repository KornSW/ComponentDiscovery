'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Text
Imports ComponentDiscovery
Imports ComponentDiscovery.ClassificationApproval
Imports ComponentDiscovery.ClassificationDetection
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Newtonsoft.Json

<TestClass()>
Public Class ManifestBasedAssemblyClassificationDetectionStrategyTests

  <TestMethod()>
  Public Sub ManifestBasedAssemblyClassificationDetectionStrategyTest()
    Dim useCache As Boolean = True
    Dim useSandbox As Boolean = False
    Dim strat As New ManifestBasedAssemblyClassificationDetectionStrategy("Fallback")

    Dim assemblyFileFullName As String = Assembly.GetExecutingAssembly().Location

    Dim businessConcerns As String() = Nothing
    Dim succes1 = strat.TryDetectClassificationsForAssembly(assemblyFileFullName, "BusinessConcern", businessConcerns)

    Dim tenantSpecific As String() = Nothing
    Dim succes2 = strat.TryDetectClassificationsForAssembly(assemblyFileFullName, "TenantSpecific", tenantSpecific)

    Dim explicitEmpty As String() = Nothing
    Dim succes3 = strat.TryDetectClassificationsForAssembly(assemblyFileFullName, "ExplicitEmpty", ExplicitEmpty)

    Dim notExisiting As String() = Nothing
    Dim succes4 = strat.TryDetectClassificationsForAssembly(assemblyFileFullName, "notExisiting", notExisiting)

    Assert.IsTrue(succes1)
    Assert.IsTrue(succes2)
    Assert.IsTrue(succes3)
    Assert.IsTrue(succes4)

    Assert.AreEqual(2, businessConcerns.Length)
    Assert.AreEqual("ConcernA", businessConcerns(0))
    Assert.AreEqual("ConcernB", businessConcerns(1))

    Assert.AreEqual(1, tenantSpecific.Length)
    Assert.AreEqual("TenantA", tenantSpecific(0))

    Assert.AreEqual(0, explicitEmpty.Length)

    Assert.AreEqual(1, notExisiting.Length)
    Assert.AreEqual("Fallback", notExisiting(0))

  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestEmptyString()

    Dim actual = Me.Parse(
      ""
    )

    Assert.AreEqual("", actual)
  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestEmptyObject()

    Dim actual = Me.Parse(
      "{ }"
    )

    Assert.AreEqual("", actual)
  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestNullCdNode()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": null }"
    )

    Assert.AreEqual("", actual)
  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestEmptyCdNode()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": { } }"
    )

    Assert.AreEqual("", actual)
  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestDimensionNull()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": { ""dimension1"": null } }"
    )

    Assert.AreEqual("[dimension1]", actual)

  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestDimensionAsObject()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": { ""dimension1"": { } } }"
    )

    Assert.AreEqual("[dimension1]", actual)

  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestSingleStringExpression()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": { ""dimension1"": ""Expression1""} }"
    )

    Assert.AreEqual("[dimension1:Expression1]", actual)

  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestEmptyArray()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": { ""dimension1"": []} }"
    )

    Assert.AreEqual("[dimension1]", actual)

  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestArrayContainingNull()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": { ""dimension1"": [null]} }"
    )

    Assert.AreEqual("[dimension1]", actual)

  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestArrayWithContainingEmptyString()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": { ""dimension1"": [""""]} }"
    )

    Assert.AreEqual("[dimension1]", actual)

  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestArrayContainingNullAndValue()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": { ""dimension1"": [null,123]} }"
    )

    Assert.AreEqual("[dimension1:123]", actual)

  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestArrayWithContainingEmptyStringAndValue()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": { ""dimension1"": ["""", 123]} }"
    )

    Assert.AreEqual("[dimension1:123]", actual)

  End Sub


  <TestMethod()>
  Public Sub ManifestParsingTestStringArrayExpressions()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": { ""dimension1"": [""Expression1"", ""Expression2"", 123]} }"
    )

    Assert.AreEqual("[dimension1:Expression1][dimension1:Expression2][dimension1:123]", actual)

  End Sub

  <TestMethod()>
  Public Sub ManifestParsingTestMixedArrayWithDetailObject()

    Dim actual = Me.Parse(
      "{ ""componentDiscovery"": { ""dimension1"": [""Expression1"", { ""Expression"": ""Expression2"", ""Namespace"": ""Foo"", ""exclude"":[""*System*"",""*Microsoft*""] }]} }"
    )

    Assert.AreEqual("[dimension1:Expression1][dimension1:Expression2|Foo|!*System*|!*Microsoft*]", actual)

  End Sub

  Private Function Parse(rawJsonContent As String) As String
    Dim collector As New StringBuilder()

    ManifestParser.Parse(
      rawJsonContent,
      Sub(dimensionName As String, classificationName As String, namespaceIncludePatterns As String(), namespaceExludePatterns As String())
        collector.AppendFormat("[{0}:{1}", dimensionName, classificationName)
        If (namespaceIncludePatterns.Length > 0 OrElse namespaceIncludePatterns.Length > 0) Then
          For Each p In namespaceIncludePatterns
            collector.Append("|" + p)
          Next
          For Each p In namespaceExludePatterns
            collector.Append("|!" + p)
          Next
        End If
        collector.Append("]")
      End Sub,
      Sub(emptyDimensionName As String)
        collector.AppendFormat("[{0}]", emptyDimensionName)
      End Sub
    )

    Return collector.ToString()
  End Function

End Class

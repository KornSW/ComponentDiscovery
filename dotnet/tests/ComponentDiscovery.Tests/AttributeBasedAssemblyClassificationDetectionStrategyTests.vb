﻿'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Threading.Tasks
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
    Dim success = strat.TryDetectClassificationsForAssembly(assemblyFileFullName, "BusinessConcern", classifications)

    Assert.AreEqual(True, success)
    Assert.AreEqual(2, classifications.Length)
    Assert.AreEqual("ConcernA", classifications(0))
    Assert.AreEqual("ConcernB", classifications(1))

  End Sub

  <TestMethod()> <Ignore()>
  Public Sub MassTest_AttributeBasedStrategy()
    Dim useCache As Boolean = False
    Dim useSandbox As Boolean = True
    Dim strat As New AttributeBasedAssemblyClassificationDetectionStrategy(useSandbox, useCache, "Fallback")

    Dim di = New DirectoryInfo("c:\temp")

    Dim allFileFullNames = di.GetFiles("*.dll").Select(Function(f) f.FullName).ToArray()

    Dim numberOfThreads = 5
    Dim start = DateTime.Now

    MultiTask.RunAndWait(
      allFileFullNames.ToList(),
      numberOfThreads,
      Sub(fileFullName As String)
        Try
          Dim classifications As String() = Nothing
          Dim success = strat.TryDetectClassificationsForAssembly(
            fileFullName, "BusinessConcern", classifications
          )

          Console.WriteLine(Path.GetFileName(fileFullName))
          If (success) Then
            For Each cl In classifications
              Console.WriteLine("   " + cl)
            Next
          Else
            Console.WriteLine("   <ERROR>")
          End If

        Catch ex As Exception
        End Try
      End Sub
    )

    Dim ms = DateTime.Now.Subtract(start).TotalMilliseconds
    Console.WriteLine($"Took (ms): {ms}")

  End Sub

End Class

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
Public Class TypeIndexerTests
#Region "..."

  Private _ThisAssembly As Assembly
  Private _AssemblyIndexer As ClassificationBasedAssemblyIndexer
  Private _TypeIndexer As ClassificationBasedTypeIndexer

  <TestInitialize()>
  Public Sub Initialize()

    _ThisAssembly = Me.GetType().Assembly

    _AssemblyIndexer = New ClassificationBasedAssemblyIndexer()

    _AssemblyIndexer.AddTaxonomicDimension(
      "BusinessConcern",
      New AttributeBasedAssemblyClassificationDetectionStrategy(),
      New DemandCentricClassificationApprovalStrategy()
    )

    _TypeIndexer = New ClassificationBasedTypeIndexer(_AssemblyIndexer)

  End Sub

  <TestCleanup()>
  Public Sub Cleanup()

    _TypeIndexer.Dispose()
    _TypeIndexer = Nothing

    _AssemblyIndexer.Dispose()
    _AssemblyIndexer = Nothing

    _ThisAssembly = Nothing

  End Sub

#End Region

  'Info: see Assemblyinfo.vb:
  '  <Assembly: AssemblyClassification("BusinessConcern", "ConcernA")>
  '  <Assembly: AssemblyClassification("BusinessConcern", "ConcernB")>

  <TestMethod()>
  Public Sub TypeIndexerWithoutPicFilter()

    Dim foundTypes As New List(Of Type)

    Dim onTypeFound = (
      Sub(foundType As Type)
        foundTypes.Add(foundType)
      End Sub
    )

    _TypeIndexer.SubscribeForApplicableTypeFound(Of MyBusinessInterfaceA)(
      parameterlessInstantiableClassesOnly:=False,
      onApplicableTypeFoundMethod:=onTypeFound
    )

    _AssemblyIndexer.TryApproveAssembly(_ThisAssembly)

    Assert.IsTrue(_AssemblyIndexer.DismissedAssemblies.Length = 1)
    Assert.IsTrue(_AssemblyIndexer.ApprovedAssemblies.Length = 0)
    Assert.IsTrue(foundTypes.Count = 0)

    _AssemblyIndexer.AddClearances("BusinessConcern", "ConcernA")
    Assert.IsTrue(_AssemblyIndexer.DismissedAssemblies.Length = 1)
    Assert.IsTrue(_AssemblyIndexer.ApprovedAssemblies.Length = 0)
    Assert.IsTrue(foundTypes.Count = 0)

    _AssemblyIndexer.AddClearances("BusinessConcern", "ConcernB")
    Assert.IsTrue(_AssemblyIndexer.DismissedAssemblies.Length = 0)
    Assert.IsTrue(_AssemblyIndexer.ApprovedAssemblies.Length = 1)
    Assert.IsTrue(foundTypes.Count = 3)

    _AssemblyIndexer.AddClearances("BusinessConcern", "ConcernC")
    Assert.IsTrue(foundTypes.Count = 4) 'TypeClassificationAttribute

  End Sub

  <TestMethod()>
  Public Sub TypeIndexerWithPicFilter()

    Dim foundTypes As New List(Of Type)

    Dim onTypeFound = (
      Sub(foundType As Type)
        foundTypes.Add(foundType)
      End Sub
    )

    _TypeIndexer.SubscribeForApplicableTypeFound(Of MyBusinessInterfaceA)(
      parameterlessInstantiableClassesOnly:=True,
      onApplicableTypeFoundMethod:=onTypeFound
    )

    _AssemblyIndexer.TryApproveAssembly(_ThisAssembly)
    Assert.IsTrue(foundTypes.Count = 0)

    _AssemblyIndexer.AddClearances("BusinessConcern", "ConcernA")
    Assert.IsTrue(foundTypes.Count = 0)

    _AssemblyIndexer.AddClearances("BusinessConcern", "ConcernB")
    Assert.IsTrue(foundTypes.Count = 1)

  End Sub

End Class

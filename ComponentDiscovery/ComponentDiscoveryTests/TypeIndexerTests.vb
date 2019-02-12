Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports ComponentDiscovery
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
    Dim businessConcernStrategy As New AttributeBasedApprovalStrategy("BusinessConcern")
    _AssemblyIndexer.AddDimension(businessConcernStrategy.DimensionName, businessConcernStrategy)

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
  Public Sub TestMethod1()

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
    Assert.IsTrue(foundTypes.Count = 0)

    _AssemblyIndexer.AddClearances("BusinessConcern", "ConcernA")
    Assert.IsTrue(foundTypes.Count = 0)

    _AssemblyIndexer.AddClearances("BusinessConcern", "ConcernB")
    Assert.IsTrue(foundTypes.Count = 3)

  End Sub

  <TestMethod()>
  Public Sub TestMethod2()

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

'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Threading.Tasks
Imports ComponentDiscovery
Imports ComponentDiscovery.ClassificationDetection
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()>
Public Class PersistentIndexCacheTests

  <TestMethod()>
  Public Sub PersistentIndexCacheTest()
    Dim myAssembly = Assembly.GetExecutingAssembly()

    Dim cacheDir As String = Path.GetDirectoryName(myAssembly.Location)
    Dim cache As New PersistentIndexCache(cacheDir)
    Dim dummyDimensionName As String = "MyDimension"
    Dim cacheFullFileName = cache.BuildCacheFileFullName(myAssembly.Location)

    Dim loadSuccess As Boolean = False
    Dim loadedClassifications As String() = Nothing

    If (File.Exists(cacheFullFileName)) Then
      'if this test is executed more than once (on a developer-machine)
      File.Delete(cacheFullFileName)
      Thread.Sleep(200)
    End If

    loadSuccess = cache.TryGetClassificationExpressionsFromCache(
      myAssembly.Location, dummyDimensionName, loadedClassifications
    )

    Assert.IsFalse(loadSuccess)

    Dim classificationsToAppend As String() = {"ClassA", "ClassB"}
    cache.AppendClassificationExpressionToCache(
      myAssembly.Location, dummyDimensionName, classificationsToAppend
    )

    Thread.Sleep(200) 'let the filesystem work...
    Dim rawCacheContent = IO.File.ReadAllText(cacheFullFileName)

    Assert.IsTrue(rawCacheContent.Contains("<AssCl>|MyDimension|ClassA,ClassB"))

    loadSuccess = cache.TryGetClassificationExpressionsFromCache(
      myAssembly.Location, dummyDimensionName, loadedClassifications
    )

    Assert.IsTrue(loadSuccess)
    Assert.AreEqual(2, loadedClassifications.Length)

    'DESTROY FILE AND OVERWRITE!!! 

    IO.File.WriteAllText(cacheFullFileName, "FOO<BAR>BAZZZ/BOOM")
    Thread.Sleep(200) 'let the filesystem work...

    Assert.IsTrue(File.Exists(cacheFullFileName))

    loadSuccess = cache.TryGetClassificationExpressionsFromCache(
      myAssembly.Location, dummyDimensionName, loadedClassifications
    )

    Assert.IsFalse(loadSuccess)

    cache.AppendClassificationExpressionToCache(
      myAssembly.Location, dummyDimensionName, classificationsToAppend
    )

    loadSuccess = cache.TryGetClassificationExpressionsFromCache(
      myAssembly.Location, dummyDimensionName, loadedClassifications
    )

    Assert.IsTrue(loadSuccess)
    Assert.AreEqual(2, loadedClassifications.Length)

    rawCacheContent = IO.File.ReadAllText(cacheFullFileName)
    Assert.IsTrue(rawCacheContent.Contains("<AssCl>|MyDimension|ClassA,ClassB"))

    cache.AppendClassificationExpressionToCache(
      myAssembly.Location, "otherDimension", {"otherClass"}
    )

    rawCacheContent = IO.File.ReadAllText(cacheFullFileName)
    Assert.IsTrue(rawCacheContent.Contains("<AssCl>|otherDimension|otherClass"))

  End Sub

End Class

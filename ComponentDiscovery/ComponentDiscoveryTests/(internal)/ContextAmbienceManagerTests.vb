'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System.Threading
Imports System.Threading.Tasks
Imports Composition.InstanceDiscovery
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()>
Public Class ContextAmbienceManagerTests

  <TestMethod()>
  Public Sub ContextAmbienceManagerTest()

    Assert.IsNull(InstanceDiscoveryContext.Current)

    Using c1 As New InstanceDiscoveryContext

      Assert.IsTrue(ReferenceEquals(InstanceDiscoveryContext.Current, c1))

      Task.Run(
        Sub()

          Thread.Sleep(10)
          Assert.IsTrue(ReferenceEquals(InstanceDiscoveryContext.Current, c1))

        End Sub
      )

      Using c2 As New InstanceDiscoveryContext

        Assert.IsTrue(ReferenceEquals(InstanceDiscoveryContext.Current, c2))

        Task.Run(
          Sub()

            Thread.Sleep(10)
            Assert.IsTrue(ReferenceEquals(InstanceDiscoveryContext.Current, c2))

          End Sub
        )

      End Using

      Assert.IsTrue(ReferenceEquals(InstanceDiscoveryContext.Current, c1))

    End Using

  End Sub

End Class

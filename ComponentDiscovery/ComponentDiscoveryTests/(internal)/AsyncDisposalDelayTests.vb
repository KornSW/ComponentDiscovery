'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()>
Public Class AsyncDisposalDelayTests

  <TestMethod()>
  Public Sub AsyncDisposalDelayTest()

    Dim subThread1Finished As Boolean = False
    Dim subThread2Finished As Boolean = False
    Dim isDisposed As Boolean = False

    Dim myDisposable As New DemoDisposable

    myDisposable.OnNotifyDispose = (
      Sub()

        'our object must be disposed AFTER
        'all threads have been finished
        Assert.IsTrue(subThread1Finished)
        Assert.IsTrue(subThread2Finished)

        isDisposed = True
      End Sub
    )

    Using inst As New AsyncDisposalDelay(myDisposable)

      Task.Run(
        Sub()

          Thread.Sleep(200)

          Task.Run(
            Sub()

              Thread.Sleep(500)
              subThread2Finished = True
            End Sub
          )

          Thread.Sleep(200)

          subThread1Finished = True
        End Sub
      )

    End Using

    Thread.Sleep(100)

    'at this time the threads are still running
    Assert.IsFalse(isDisposed)

    'keep the unit-test running, until the delayed disposal has been executed
    Dim timeout = DateTime.Now.AddSeconds(10)
    Do Until isDisposed
      Thread.Sleep(10)
      If (DateTime.Now > Timeout) Then
        Assert.Fail("Received no lifetime-end notification from 'CallTreeTracker'...")
      End If
    Loop

  End Sub

  Private Class DemoDisposable
    Implements IDisposable

    Public Property OnNotifyDispose As Action

    Public Sub Dispose() Implements IDisposable.Dispose
      Me.OnNotifyDispose.Invoke()
    End Sub
  End Class

End Class

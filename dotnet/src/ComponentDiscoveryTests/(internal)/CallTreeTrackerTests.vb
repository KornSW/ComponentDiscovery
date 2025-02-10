'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()>
Public Class CallTreeTrackerTests

  <TestMethod()> <Ignore("flaky on azure devops...")>
  Public Sub CallTreeTrackerTest()

    Dim timeLog As New List(Of String)

    Dim allTrackedThreadsAreFinished As Boolean = False

    Using tracker As New CallTreeTracker(
      Sub()
        allTrackedThreadsAreFinished = True
        SyncLock timeLog
          timeLog.Add("LIFETIME-ENDED-CALLBACK")
        End SyncLock
      End Sub
    )

      SyncLock timeLog
        timeLog.Add("TRACKER-ENTERED")
      End SyncLock

      Task.Run(
        Sub()

          SyncLock timeLog
            timeLog.Add("CHILD A ENTERED")
          End SyncLock

          Thread.Sleep(1000)

          SyncLock timeLog
            timeLog.Add("CHILD A ENDING NOW")
          End SyncLock

        End Sub
      )

      Task.Run(
        Sub()

          SyncLock timeLog
            timeLog.Add("CHILD B ENTERED")
          End SyncLock

          Thread.Sleep(200)

          Task.Run(
            Sub()

              SyncLock timeLog
                timeLog.Add("CHILD B.1 ENTERED")
              End SyncLock

              Thread.Sleep(1100)

              SyncLock timeLog
                timeLog.Add("CHILD B.1 ENDING NOW")
              End SyncLock

            End Sub
          )

          Thread.Sleep(200)

          SyncLock timeLog
            timeLog.Add("CHILD B ENDING NOW")
          End SyncLock

        End Sub
      )

      SyncLock timeLog
        timeLog.Add("TRACKER-LEAVING")
      End SyncLock

    End Using

    SyncLock timeLog
      timeLog.Add("TRACKER-DISPOSED")
    End SyncLock

    Task.Run(
      Sub()

        '2 secs should be enough to be the last thread here
        '(after the point of 'allTrackedThreadsAreFinished' below)!
        Thread.Sleep(2000)

        'we are outside of the using block, so nobody should have
        'waited so long -> the timeLog.Add should have come too late!
        SyncLock timeLog
          timeLog.Add("WE DONT WANT TO HAVE THIS TRACKED!")
        End SyncLock

      End Sub
      )

    Dim timeout = DateTime.Now.AddSeconds(10)
    Do Until allTrackedThreadsAreFinished
      Thread.Sleep(10)
      If (DateTime.Now > timeout) Then
        Assert.Fail("Received no lifetime-end notification from 'CallTreeTracker'...")
      End If
    Loop

    Dim log = String.Join(" > ", timeLog)

    Assert.AreEqual(
      "TRACKER-ENTERED > TRACKER-LEAVING > CHILD A ENTERED > CHILD B ENTERED > " +
      "TRACKER-DISPOSED > CHILD B.1 ENTERED > CHILD B ENDING NOW > CHILD A ENDING NOW > " +
      "CHILD B.1 ENDING NOW > LIFETIME-ENDED-CALLBACK", log
    )

  End Sub

End Class

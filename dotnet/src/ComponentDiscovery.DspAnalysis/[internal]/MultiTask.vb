Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.InteropServices

Namespace System.Threading.Tasks

  Friend Module MultiTask

    Public Delegate Function TryGetNextItemMethod(Of TItem)(<Out> ByRef nextItem As TItem) As Boolean

    Public Sub RunAndWait(Of TItem)(nextItemGetter As TryGetNextItemMethod(Of TItem), numberOfThreads As Integer, processorMethod As Action(Of TItem))
      Call Run(nextItemGetter, numberOfThreads, processorMethod).Wait()
    End Sub

    Public Sub RunAndWait(Of TItem)(nextItemGetter As TryGetNextItemMethod(Of TItem), numberOfThreads As Integer, processorMethod As Action(Of TItem), cancellationToken As CancellationToken)
      Run(nextItemGetter, numberOfThreads, processorMethod).Wait(CancellationToken.None)
    End Sub

    Public Function Run(Of TItem)(nextItemGetter As TryGetNextItemMethod(Of TItem), numberOfThreads As Integer, processorMethod As Action(Of TItem)) As Task
      Dim tasks = New List(Of Task)()

      Dim syncLockedItemGetter As TryGetNextItemMethod(Of TItem) = (
        Function(ByRef nextItem As TItem)
          SyncLock tasks
            Return nextItemGetter.Invoke(nextItem)
          End SyncLock
        End Function
      )

      For blockIndex = 0 To numberOfThreads - 1
        Dim offset = blockIndex

        tasks.Add(
          Task.Run(
            Sub()
              Thread.CurrentThread.Name = $"Block#{offset}"
              Dim nextItem As TItem = Nothing

              While syncLockedItemGetter.Invoke(nextItem)
                processorMethod.Invoke(nextItem)
              End While
            End Sub
          )
        )

      Next

      Return Task.WhenAll(tasks.ToArray())
    End Function

    Public Sub RunAndWait(Of TItem)(itemsToProcess As IEnumerable(Of TItem), numberOfThreads As Integer, processorMethod As Action(Of TItem))
      Call Run(itemsToProcess, numberOfThreads, processorMethod).Wait()
    End Sub

    Public Sub RunAndWait(Of TItem)(itemsToProcess As IEnumerable(Of TItem), numberOfThreads As Integer, processorMethod As Action(Of TItem), cancellationToken As CancellationToken)
      Run(itemsToProcess, numberOfThreads, processorMethod).Wait(CancellationToken.None)
    End Sub

    Public Function Run(Of TItem)(itemsToProcess As IEnumerable(Of TItem), numberOfThreads As Integer, processorMethod As Action(Of TItem)) As Task
      Dim enumerator As IEnumerator(Of TItem) = itemsToProcess.GetEnumerator()
      Dim nextItemGetter As TryGetNextItemMethod(Of TItem) = (
        Function(ByRef nextItem)
          'lock (enumerator) { //not neccessarry, because there is already a semaphore which locks the nextItemGetter call
          If Not enumerator.MoveNext() Then
            nextItem = Nothing
            Return False
          End If
          nextItem = enumerator.Current
          Return True
          '}
        End Function
      )
      Return Run(nextItemGetter, numberOfThreads, processorMethod)
    End Function

    Public Sub RunAndWait(Of TItem)(itemsToProcess As TItem(), numberOfThreads As Integer, processorMethod As Action(Of TItem))
      Call Run(itemsToProcess, numberOfThreads, processorMethod).Wait()
    End Sub

    Public Sub RunAndWait(Of TItem)(itemsToProcess As TItem(), numberOfThreads As Integer, processorMethod As Action(Of TItem), cancellationToken As CancellationToken)
      Run(itemsToProcess, numberOfThreads, processorMethod).Wait(CancellationToken.None)
    End Sub

    Public Function Run(Of TItem)(itemsToProcess As TItem(), numberOfThreads As Integer, processorMethod As Action(Of TItem)) As Task
      Dim inputLength = itemsToProcess.Length
      Dim tasks = New List(Of Task)()

      For blockIndex = 0 To numberOfThreads - 1
        Dim offset = blockIndex

        tasks.Add(
          Task.Run(
            Sub()
              Thread.CurrentThread.Name = $"Block#{offset}"
              Dim i = offset

              While i < inputLength
                processorMethod.Invoke(itemsToProcess(i))
                i += numberOfThreads
              End While
            End Sub
          )
        )

      Next

      Return Task.WhenAll(tasks)
    End Function

  End Module

End Namespace

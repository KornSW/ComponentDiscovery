
Namespace System.Threading

  Friend Class AsyncDisposalDelay
    Inherits CallTreeTracker

    Public ReadOnly Property ObjectToDispose As IDisposable

    Public Sub New(objectToDispose As IDisposable)
      MyBase.New(AddressOf objectToDispose.Dispose)

      Me.ObjectToDispose = objectToDispose

    End Sub

  End Class

End Namespace

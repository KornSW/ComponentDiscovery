'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics

Public NotInheritable Class ProviderRepository(Of TProvider)
  Implements IDisposable

#Region " Fields & Constructors "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _TypeIndexer As ITypeIndexer = Nothing

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _Providers As New List(Of TProvider)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _ProviderInitializedSubscribers As New List(Of Action(Of TProvider))

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _ProviderShutdownSubscribers As New List(Of Action(Of TProvider))

  Public Sub New(typeIndexer As ITypeIndexer)
    _TypeIndexer = typeIndexer
    _TypeIndexer.SubscribeForApplicableTypeFound(Of TProvider)(True, AddressOf Me.AddProviderType)
  End Sub

#End Region

#Region " Init "

  Protected Sub AddProviderType(providerType As Type)
    Dim provider = DirectCast(Activator.CreateInstance(providerType), TProvider)
    Me.AddProvider(provider)
  End Sub

  Public Sub AddProvider(provider As TProvider)
    SyncLock _Providers
      _Providers.Add(provider)
      SyncLock _ProviderInitializedSubscribers
        For Each subscriber In _ProviderInitializedSubscribers
          subscriber.Invoke(provider)
        Next
      End SyncLock
    End SyncLock
  End Sub

#End Region

#Region " Consume "

  <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
  Public ReadOnly Property Providers As TProvider()
    Get
      SyncLock _Providers
        Return _Providers.ToArray()
      End SyncLock
    End Get
  End Property

  Public Sub SubscribeForProviderInitialized(subscriber As Action(Of TProvider))
    SyncLock _ProviderInitializedSubscribers
      If (Not _ProviderInitializedSubscribers.Contains(subscriber)) Then
        _ProviderInitializedSubscribers.Add(subscriber)

        For Each provider In _Providers
          subscriber.Invoke(provider)
        Next

      End If
    End SyncLock
  End Sub

  Public Sub UnsubscribeFromProviderInitialized(subscriber As Action(Of TProvider))
    SyncLock _ProviderInitializedSubscribers
      If (_ProviderInitializedSubscribers.Contains(subscriber)) Then
        _ProviderInitializedSubscribers.Remove(subscriber)
      End If
    End SyncLock
  End Sub

  Public Sub SubscribeForProviderShutdown(subscriber As Action(Of TProvider))
    SyncLock _ProviderShutdownSubscribers
      If (Not _ProviderShutdownSubscribers.Contains(subscriber)) Then
        _ProviderShutdownSubscribers.Add(subscriber)
      End If
    End SyncLock
  End Sub

  Public Sub UnsubscribeFromProviderShutdown(subscriber As Action(Of TProvider))
    SyncLock _ProviderShutdownSubscribers
      If (_ProviderShutdownSubscribers.Contains(subscriber)) Then
        _ProviderShutdownSubscribers.Remove(subscriber)
      End If
    End SyncLock
  End Sub

#End Region

#Region " IDisposable "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AlreadyDisposed As Boolean = False

  ''' <summary>
  ''' Dispose the current object instance and suppress the finalizer
  ''' </summary>
  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Sub Dispose() Implements IDisposable.Dispose
    If (Not _AlreadyDisposed) Then
      Me.Disposing()
      _AlreadyDisposed = True
    End If
    GC.SuppressFinalize(Me)
  End Sub

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Protected Sub Disposing()
    If (_TypeIndexer IsNot Nothing) Then
      _TypeIndexer.UnsubscribeFromApplicableTypeFound(Of TProvider)(True, AddressOf Me.AddProviderType)
      _TypeIndexer = Nothing
    End If
    SyncLock _Providers
      For Each provider In _Providers
        SyncLock _ProviderShutdownSubscribers
          For Each subscriber In _ProviderShutdownSubscribers
            Try
              subscriber.Invoke(provider)
            Catch
            End Try
          Next
        End SyncLock
        If (provider IsNot Nothing AndAlso TypeOf (provider) Is IDisposable) Then
          Try
            DirectCast(provider, IDisposable).Dispose()
          Catch
          End Try
        End If
      Next
      _Providers.Clear()
    End SyncLock
  End Sub

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Protected Sub DisposedGuard()
    If (_AlreadyDisposed) Then
      Throw New ObjectDisposedException(Me.GetType.Name)
    End If
  End Sub

#End Region

End Class

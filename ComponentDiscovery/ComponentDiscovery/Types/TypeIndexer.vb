'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq

Public Class TypeIndexer
  Implements ITypeIndexer

#Region " Fields & Constructor "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AssemblyIndexer As IAssemblyIndexer

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _ApprovingMethod As Func(Of Type, Boolean) = AddressOf Me.DefaultApprovingMethod

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _ApplicablesPerSelector As New Dictionary(Of Type, ApplicableTypesIndex)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _EnablePersistentCache As Boolean = False

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _EnableAsyncIndexing As Boolean = False

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _EnableTracing As Boolean = False

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _ManuallyRegisteredCandidates As New List(Of Type)

  Public Sub New(assemblyIndexer As IAssemblyIndexer, Optional enablePersistentCache As Boolean = False, Optional customApprovingMethod As Func(Of Type, Boolean) = Nothing, Optional enableAsyncIndexing As Boolean = False)
    _AssemblyIndexer = assemblyIndexer
    _EnablePersistentCache = enablePersistentCache
    If (customApprovingMethod IsNot Nothing) Then
      _ApprovingMethod = customApprovingMethod
    End If
    _EnableAsyncIndexing = enableAsyncIndexing
  End Sub

  Protected Overridable Function DefaultApprovingMethod(t As Type) As Boolean
    Return (t.IsPublic AndAlso t.IsNested = False)
  End Function

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Property EnableTracing As Boolean
    Get
      Return _EnableTracing
    End Get
    Set(value As Boolean)
      _EnableTracing = value
      SyncLock _ApplicablesPerSelector
        For Each item In _ApplicablesPerSelector.Values
          item.EnableTracing = _EnableTracing
        Next
      End SyncLock
    End Set
  End Property

#End Region

#Region " Obervation (which types are needed to be indexed) "

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public ReadOnly Property AssemblyIndexer As IAssemblyIndexer
    Get
      Return _AssemblyIndexer
    End Get
  End Property

  <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
  Protected ReadOnly Property ApplicablesPerSelector As ApplicableTypesIndex()
    Get
      SyncLock _ApplicablesPerSelector
        Return _ApplicablesPerSelector.Values.ToArray()
      End SyncLock
    End Get
  End Property

  Protected ReadOnly Property ManuallyRegisteredCandidates() As Type()
    Get
      Return _ManuallyRegisteredCandidates.ToArray()
    End Get
  End Property

  ''' <summary>
  '''   Explicitely adds a type to the index.
  ''' </summary>
  ''' <remarks>
  '''   This is an exceptional use of the type indexer. Normally, types are found automatically within indexed assemblies.
  '''   This method can be used to add types to the index that - for some reasons - shouldn't be discovered by seeking assemblies.
  ''' </remarks>
  Public Sub RegisterCandidate(candidate As Type)
    If (Not _ManuallyRegisteredCandidates.Contains(candidate)) Then
      _ManuallyRegisteredCandidates.Add(candidate)
      Dim tps As ApplicableTypesIndex()
      SyncLock _ApplicablesPerSelector
        tps = _ApplicablesPerSelector.Values.ToArray()
      End SyncLock
      For Each index As ApplicableTypesIndex In tps
        index.TryRegisterCandidate(candidate)
      Next
    End If
  End Sub

  Public Function GetApplicableTypes(selector As Type, parameterlessInstantiableClassesOnly As Boolean) As Type() Implements ITypeIndexer.GetApplicableTypes

    If (_AssemblyIndexer Is Nothing) Then
      Throw New Exception("TypeIndexer not wired up!")
    End If

    If (parameterlessInstantiableClassesOnly) Then
      Return Me.GetApplicableTypes(selector).ApplicableTypes.Where(Function(t) t.IsClass AndAlso t.IsParameterlessInstantiable).ToArray()
    End If
    Return Me.GetApplicableTypes(selector).ApplicableTypes
  End Function

  Protected Function GetApplicableTypes(selector As Type) As ApplicableTypesIndex
    Me.EnableIndexingOf(selector)
    SyncLock _ApplicablesPerSelector
      Return _ApplicablesPerSelector(selector)
    End SyncLock
  End Function

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Sub EnableIndexingOf(selector As Type)
    SyncLock _ApplicablesPerSelector
      If (Not _ApplicablesPerSelector.ContainsKey(selector)) Then
        Dim newIndex As New ApplicableTypesIndex(selector, _AssemblyIndexer, _EnablePersistentCache, _ApprovingMethod, _EnableAsyncIndexing)
        newIndex.EnableTracing = Me.EnableTracing
        _ApplicablesPerSelector.Add(selector, newIndex)
        For Each manuallyRegisteredApplicableType In _ManuallyRegisteredCandidates
          newIndex.TryRegisterCandidate(manuallyRegisteredApplicableType)
        Next
      End If
    End SyncLock
  End Sub

  'TODO: WTF????
  Public ReadOnly Property IsUsable As Boolean
    Get
      Return (_AssemblyIndexer IsNot Nothing)
    End Get
  End Property

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public ReadOnly Property MonitoredSelectors As Type()
    Get
      SyncLock _ApplicablesPerSelector
        Return _ApplicablesPerSelector.Keys.ToArray()
      End SyncLock
    End Get
  End Property

#End Region

#Region " Individual Resolve "

  Private _IndividualResolveCache As New Dictionary(Of String, Type)

  Public Function TryResolveType(typeFullName As String, ByRef result As Type) As Boolean Implements ITypeIndexer.TryResolveType
    typeFullName = typeFullName.ToLower()

    SyncLock _IndividualResolveCache
      If (_IndividualResolveCache.ContainsKey(typeFullName)) Then
        result = _IndividualResolveCache(typeFullName)
        Return True
      End If
    End SyncLock

    Dim foundType As Type = Nothing

    For Each t In _ManuallyRegisteredCandidates
      If (String.Equals(t.FullName, typeFullName, StringComparison.CurrentCultureIgnoreCase)) Then
        foundType = t
        Exit For
      End If
    Next

    If (foundType Is Nothing) Then
      For Each ass In _AssemblyIndexer.ApprovedAssemblies.ToArray()
        For Each accessableType In ass.GetTypesAccessible()
          If (String.Equals(accessableType.FullName, typeFullName, StringComparison.CurrentCultureIgnoreCase)) Then
            foundType = accessableType
            Exit For
          End If
        Next
        If (foundType IsNot Nothing) Then
          Exit For
        End If
      Next
    End If

    If (foundType IsNot Nothing) Then
      SyncLock _IndividualResolveCache
        If (Not _IndividualResolveCache.ContainsKey(typeFullName)) Then
          _IndividualResolveCache.Add(typeFullName, foundType)
        End If
      End SyncLock
      result = foundType
      Return True
    End If

    Return False
  End Function

#End Region

#Region " Subscription "

  Public Sub SubscribeForApplicableTypeFound(selector As Type, parameterlessInstantiableClassesOnly As Boolean, onApplicableTypeFoundMethod As Action(Of Type)) _
  Implements ITypeIndexer.SubscribeForApplicableTypeFound

    Me.GetApplicableTypes(selector).AddSubscriber(onApplicableTypeFoundMethod, parameterlessInstantiableClassesOnly)

  End Sub

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Sub UnsubscribeFromApplicableTypeFound(selector As Type, parameterlessInstantiableClassesOnly As Boolean, onTypeIndexedMethod As Action(Of Type)) _
  Implements ITypeIndexer.UnsubscribeFromApplicableTypeFound

    Me.GetApplicableTypes(selector).RemoveSubscriber(onTypeIndexedMethod, parameterlessInstantiableClassesOnly)

  End Sub

#End Region

#Region " IDisposable "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _IsAlreadyDisposed As Boolean = False

  ''' <summary>
  '''   Dispose the current object instance
  ''' </summary>
  Protected Overridable Sub Dispose(disposing As Boolean)
    If (Not _IsAlreadyDisposed) Then
      If (disposing) Then
        Dim indexes = _ApplicablesPerSelector.Values.ToArray()
        _ApplicablesPerSelector.Clear()
        For Each index In indexes
          index.Dispose()
        Next
        indexes = Nothing
      End If
      _IsAlreadyDisposed = True
    End If
  End Sub

  ''' <summary>
  '''   Dispose the current object instance and suppress the finalizer
  ''' </summary>
  Public Sub Dispose() Implements IDisposable.Dispose
    Me.Dispose(True)
    GC.SuppressFinalize(Me)
  End Sub

#End Region

End Class

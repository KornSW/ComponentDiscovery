Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection
Imports System.Threading.Tasks

Partial Class TypeIndexer

  <DebuggerDisplay("ApplicableTypeIndex (for: {Selector})")>
  Protected Class ApplicableTypesIndex
    Implements IDisposable
    Implements IEnumerable

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _Selector As Type

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _EnableAsyncIndexing As Boolean

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _ApplicableTypes As New List(Of Type)

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _Subscribers As New List(Of Action(Of Type))

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _ParameterlessInstantiableClassesOnlySubscribers As New List(Of Action(Of Type))

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _EnablePersistentCache As Boolean = False

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _AssemblyIndexer As IAssemblyIndexer

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _ApprovingMethod As Func(Of Type, Boolean)

    Public Sub New(selector As Type, assemblyIndexer As IAssemblyIndexer, enablePersistentCache As Boolean, approvingMethod As Func(Of Type, Boolean), enableAsyncIndexing As Boolean)
      _EnableAsyncIndexing = enableAsyncIndexing
      _Selector = selector
      _AssemblyIndexer = assemblyIndexer
      _EnablePersistentCache = enablePersistentCache
      _ApprovingMethod = approvingMethod
      _AssemblyIndexer.SubscribeForAssemblyApproved(AddressOf Me.HandleAddedAssembly)
    End Sub

#Region " Properties "

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Public ReadOnly Property Selector As Type
      Get
        Return _Selector
      End Get
    End Property

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Public ReadOnly Property ApplicableTypes As Type()
      Get
        Return _ApplicableTypes.ToArray()
      End Get
    End Property

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Private ReadOnly Property SelectorIsAttribute As Boolean
      Get
        Return _Selector.IsSubclassOf(GetType(Attribute))
      End Get
    End Property

#End Region

    Public Sub TryRegisterCandidate(candidate As Type)
      Dim assName As String = candidate.Assembly.GetName().Name
      System.Diagnostics.Trace.TraceInformation(
      "TypeIndexer (for '{1}'): Manually registering candidate Type '{2}' for '{1}' (from assembly '{0}') ...",
      assName, _Selector.Name, candidate.Name
    )
      Me.TryRegisterCandidate(candidate, assName)
    End Sub

    Private Sub HandleAddedAssembly(assemblyToCrawl As Assembly)
      If (_EnableAsyncIndexing) Then
        Task.Run(Sub() Me.CrawlAssembly(assemblyToCrawl))
      Else
        Me.CrawlAssembly(assemblyToCrawl)
      End If
    End Sub

    Private Sub CrawlAssembly(assemblyToCrawl As Assembly)
      Dim an = assemblyToCrawl.GetName()
      Dim assName As String = an.Name

      'skip assemblies, which are not referencing the target-types assembly
      'If (Not Me.Selector.Assembly.IsDirectReferencedBy(assemblyToCrawl)) Then
      '  'Trace.TraceInformation("TypeIndexer (for '{1}'): Skipping assembly '{0}' because it has no reference to the assembly which is containing the selector type '{1}'", assName, _Selector.Name)
      '  Exit Sub
      'End If
      'GEHT NICHT DA ABLEITUNGEN VON IMPLEMENTIERENDEN KLASSEN KEINE REFERENZ AUF DIE URSPRUNGSASSEMBLY HABEN MÜSSEN!

      Dim foundTypes As List(Of Type)
      Dim cacheUpdateRequired As Boolean = False
      Dim persistentCacheTypeNames As String() = Nothing

      If (_EnablePersistentCache AndAlso PersistentIndexCache.GetInstance().TryGetTypesFromCache(assemblyToCrawl.Location, _Selector.FullName, persistentCacheTypeNames)) Then
        foundTypes = New List(Of Type)
        'Trace.TraceInformation("TypeIndexer (for '{1}'): Loading applicable types for '{1}' from persistent cache (for assembly '{0}')...", assName, _Selector.Name)
        For Each typeNameFromCache In persistentCacheTypeNames
          If (Not String.IsNullOrWhiteSpace(typeNameFromCache)) Then
            Try
              foundTypes.Add(assemblyToCrawl.GetType(typeNameFromCache))
            Catch ex As Exception
              Trace.TraceError(ex.Message)
            End Try
          End If
        Next
      Else
        'Trace.TraceInformation("TypeIndexer (for '{1}'): Scanning assembly '{0}' to find applicable type for '{1}'...", assName, _Selector.Name)
        foundTypes = assemblyToCrawl.GetTypesAccessible().ToList()
        cacheUpdateRequired = _EnablePersistentCache
      End If

      Try
        For Each foundType As Type In foundTypes
          If (foundType IsNot Nothing AndAlso foundType.IsPublic) Then
            Me.TryRegisterCandidate(foundType, assName)
          End If
        Next

        If (cacheUpdateRequired) Then
          persistentCacheTypeNames = _ApplicableTypes.Where(Function(t) t.Assembly = assemblyToCrawl).Select(Function(t) t.FullName).ToArray()
          PersistentIndexCache.GetInstance().WriteTypesToCache(assemblyToCrawl.Location, _Selector.FullName, persistentCacheTypeNames)
        End If

      Catch ex As ReflectionTypeLoadException
        Trace.TraceError(ex.Message)
        For Each le In ex.LoaderExceptions
          Trace.TraceError(ex.Message)
        Next
      Catch ex As Exception
        Trace.TraceError(ex.Message)
      End Try
    End Sub

    Private Sub TryRegisterCandidate(candidate As Type, assemblyName As String)

      Dim match As Boolean = False
      If (Me.SelectorIsAttribute) Then

        'this means that were including types, which have not the attribute byself,
        'but inheritting from any base which has the attribute!
        'so were implicitly collecing all derived types
        Const includeInherittingTypes As Boolean = True

        For Each atr In candidate.GetCustomAttributes(includeInherittingTypes)
          If (atr.GetType() = Me.Selector) Then
            match = True
            ' HACK: Optimierung durch "Exit For" hier möglich
          End If
        Next
      Else
        match = Me.Selector.IsAssignableFrom(candidate)
      End If

      ' HACK: OPTIMIERUNG MÖGLICH: If (Not match) Then Exit Sub?

      If (_ApprovingMethod.Invoke(candidate)) Then

        If (match AndAlso Not _ApplicableTypes.Contains(candidate)) Then
          'Trace.TraceInformation("TypeIndexer (for '{1}'): found applicable type '{0}' (for '{1}') from assembly '{2}'", candidate.Name, _Selector.Name, assemblyName)
          _ApplicableTypes.Add(candidate)
          For Each subscriber In _Subscribers
            If (_EnableAsyncIndexing) Then
              Task.Run(Sub() subscriber.Invoke(candidate))
            Else
              subscriber.Invoke(candidate)
            End If
          Next
          For Each subscriber In _ParameterlessInstantiableClassesOnlySubscribers
            If (candidate.IsClass AndAlso candidate.IsParameterlessInstantiable) Then
              If (_EnableAsyncIndexing) Then
                Task.Run(Sub() subscriber.Invoke(candidate))
              Else
                subscriber.Invoke(candidate)
              End If
            End If
          Next

        End If

      End If

    End Sub

    Private Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
      Return _ApplicableTypes.GetEnumerator()
    End Function

    Public Sub AddSubscriber(onApplicableTypeFoundMethod As Action(Of Type), parameterlessInstantiableClassesOnly As Boolean)
      Me.RemoveSubscriber(onApplicableTypeFoundMethod, parameterlessInstantiableClassesOnly)
      If (parameterlessInstantiableClassesOnly) Then
        _ParameterlessInstantiableClassesOnlySubscribers.Add(onApplicableTypeFoundMethod)
        For Each applicableType In _ApplicableTypes
          If (applicableType.IsClass AndAlso applicableType.IsParameterlessInstantiable()) Then
            onApplicableTypeFoundMethod.Invoke(applicableType)
          End If
        Next
      Else
        _Subscribers.Add(onApplicableTypeFoundMethod)
        For Each applicableType In _ApplicableTypes
          onApplicableTypeFoundMethod.Invoke(applicableType)
        Next
      End If
    End Sub

    Public Sub RemoveSubscriber(onApplicableTypeFoundMethod As Action(Of Type), parameterlessInstantiableClassesOnly As Boolean)
      If (parameterlessInstantiableClassesOnly) Then
        If (_ParameterlessInstantiableClassesOnlySubscribers.Contains(onApplicableTypeFoundMethod)) Then
          _ParameterlessInstantiableClassesOnlySubscribers.Remove(onApplicableTypeFoundMethod)
        End If
      Else
        If (_Subscribers.Contains(onApplicableTypeFoundMethod)) Then
          _Subscribers.Remove(onApplicableTypeFoundMethod)
        End If
      End If
    End Sub

#Region " IDisposable "

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _IsAlreadyDisposed As Boolean = False

    ''' <summary>
    '''   Dispose the current object instance
    ''' </summary>
    Protected Overridable Sub Dispose(disposing As Boolean)
      If (Not _IsAlreadyDisposed) Then
        If (disposing) Then
          If (_AssemblyIndexer IsNot Nothing) Then
            _AssemblyIndexer.UnsubscribeFromAssemblyApproved(AddressOf Me.HandleAddedAssembly)
          End If
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

End Class

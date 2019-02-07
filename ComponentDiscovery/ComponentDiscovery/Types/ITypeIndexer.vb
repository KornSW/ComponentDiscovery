Imports System
Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Public Interface ITypeIndexer
  Inherits IDisposable

  ''' <summary>
  '''   Adds a subscription for a selector (type).
  '''   If the selector is an interface, then we're indexing applicable implementations.
  '''   If the selector is a class, then we're indexing applicable specializations.
  '''   If the selector is an attribute, then we're indexing applicable types flagged with that.
  '''   In addition, we can restrict the subscription to only these applicable types which, are 'parameterless instantiable'
  '''   (means only non-abstract classes having a public default constrcutor or a public parameterless constructor).
  '''   For every applicable type which matches to this constraint, the 'reciver' method will be invoked exactly once.
  ''' </summary>
  Sub SubscribeForApplicableTypeFound(selector As Type, parameterlessInstantiableClassesOnly As Boolean, onApplicableTypeFoundMethod As Action(Of Type))

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Sub UnsubscribeFromApplicableTypeFound(selector As Type, parameterlessInstantiableClassesOnly As Boolean, onApplicableTypeFoundMethod As Action(Of Type))

  Function GetApplicableTypes(selector As Type, parameterlessInstantiableClassesOnly As Boolean) As Type()

  Function TryResolveType(typeFullName As String, ByRef result As Type) As Boolean

End Interface

Public Module TypeIndexerExtensions

  ''' <summary>
  '''   Subscribe Types on the receiver delegate. WARNING: the delegate needs to be thread-safe!!!
  ''' </summary>
  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Sub SubscribeForApplicableTypeFound(Of TSelector)(
    extendee As ITypeIndexer, parameterlessInstantiableClassesOnly As Boolean, onApplicableTypeFoundMethod As Action(Of Type)
  )
    extendee.SubscribeForApplicableTypeFound(GetType(TSelector), parameterlessInstantiableClassesOnly, onApplicableTypeFoundMethod)
  End Sub

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Sub UnsubscribeFromApplicableTypeFound(Of TSelector)(
    extendee As ITypeIndexer, parameterlessInstantiableClassesOnly As Boolean, onApplicableTypeFoundMethod As Action(Of Type)
  )
    extendee.UnsubscribeFromApplicableTypeFound(GetType(TSelector), parameterlessInstantiableClassesOnly, onApplicableTypeFoundMethod)
  End Sub

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function GetApplicableTypes(Of TSelector)(extendee As ITypeIndexer, parameterlessInstantiableClassesOnly As Boolean) As Type()
    Return extendee.GetApplicableTypes(GetType(TSelector), parameterlessInstantiableClassesOnly)
  End Function

End Module

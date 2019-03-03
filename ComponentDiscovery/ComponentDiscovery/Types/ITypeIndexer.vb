'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.ComponentModel

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

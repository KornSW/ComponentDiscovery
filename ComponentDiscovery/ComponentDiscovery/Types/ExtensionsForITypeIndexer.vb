'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Public Module ExtensionsForITypeIndexer

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

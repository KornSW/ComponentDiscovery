'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.ComponentModel
Imports System.IO
Imports System.Reflection

Public Interface IAssemblyIndexer
  Inherits IDisposable

  Sub SubscribeForAssemblyApproved(onAssemblyIndexedMethod As Action(Of Assembly))

  Sub UnsubscribeFromAssemblyApproved(onAssemblyIndexedMethod As Action(Of Assembly))

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Sub TryApproveCurrentAssembly()

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Sub TryApproveAssembly(assembly As Assembly)

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Sub TryApproveAssemblyFile(assemblyFullFilename As String)

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Function TryApproveAssemblyFile(fileInfo As FileInfo, Optional forceReapprove As Boolean = False) As Boolean

  ReadOnly Property ApprovedAssemblies As Assembly()

  ReadOnly Property DismissedAssemblies As String()

End Interface

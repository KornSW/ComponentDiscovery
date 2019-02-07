
Public Class ClassificationBasedTypeIndexer
  Inherits TypeIndexer

  Public Sub New(assemblyIndexer As ClassificationBasedAssemblyIndexer, Optional enableAsyncIndexing As Boolean = False)
    MyBase.New(assemblyIndexer, enableAsyncIndexing)
  End Sub

  Public Shadows ReadOnly Property AssemblyIndexer As ClassificationBasedAssemblyIndexer
    Get
      Return DirectCast(MyBase.AssemblyIndexer, ClassificationBasedAssemblyIndexer)
    End Get
  End Property

End Class

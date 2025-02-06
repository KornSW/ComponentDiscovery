Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Linq.Dynamic
Imports System.Reflection
Imports ComponentDiscovery
Imports ComponentDiscovery.ClassificationDetection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CSharp

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
' IN .NET-FX this will require Nuget-Pkg "Microsoft.CodeAnalysis.CSharp"
' DLLs to refer:
'    System.Collections.Immutable
'    Microsoft.CodeAnalysis.dll
'    Microsoft.CodeAnalysis.CSharp.dll 
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Namespace ClassificationDetection

  Public Class RoslynBasedAssemblyClassificationDetectionStrategy
    Implements IAssemblyClassificationDetectionStrategy

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _EnablePersistentCache As Boolean

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _DefaultsIfNoAttributeFound As String()

    Public Sub New(ParamArray defaultsIfNoAttributeFound As String())
      MyClass.New(False, defaultsIfNoAttributeFound)
    End Sub

    Public Sub New(enablePersistentCache As Boolean, ParamArray defaultsIfNoAttributeFound As String())

      _EnablePersistentCache = enablePersistentCache
      _DefaultsIfNoAttributeFound = defaultsIfNoAttributeFound

      If (_DefaultsIfNoAttributeFound Is Nothing) Then
        _DefaultsIfNoAttributeFound = {}
      End If

    End Sub

    Public ReadOnly Property DefaultsIfNoAttributeFound As String()
      Get
        Return _DefaultsIfNoAttributeFound
      End Get
    End Property

    Public Function TryDetectClassificationsForAssembly(
      assemblyFullFilename As String,
      taxonomicDimensionName As String,
      ByRef classifications As String()
    ) As Boolean Implements IAssemblyClassificationDetectionStrategy.TryDetectClassificationsForAssembly

      Return Me.TryDetectClassificationsForAssemblyCore(
        assemblyFullFilename,
        taxonomicDimensionName,
        classifications
      )

    End Function

    Private Function TryDetectClassificationsForAssemblyCore(
      assemblyFullFilename As String,
      taxonomicDimensionName As String,
      ByRef classifications As String()
    ) As Boolean

      Dim result As String() = Nothing

      'If (
      '  _EnablePersistentCache AndAlso
      '  PersistentIndexCache.GetInstance().TryGetClassificationExpressionsFromCache(assemblyFullFilename, taxonomicDimensionName, result)
      ') Then
      '  classifications = result
      '  Return True
      'End If

      Try
        result = FetchClassificationExpressionsFromAssembly(
          assemblyFullFilename, taxonomicDimensionName
        )

      Catch ex As Exception
        result = Nothing
      End Try

      If (result Is Nothing) Then
        'Nothing = ERROR
        Return False
      ElseIf (result.Length = 0) Then
        'no Attributes found -> take the default
        result = _DefaultsIfNoAttributeFound
      End If

      If (_EnablePersistentCache) Then
        'PersistentIndexCache.GetInstance().WriteClassificationExpressionToCache(assemblyFullFilename, taxonomicDimensionName, result)
      End If

      classifications = result
      Return True
    End Function

    Private Shared _Compilation As CSharpCompilation = Nothing

    Private Shared Function FetchClassificationExpressionsFromAssembly(assemblyFullFilename As String, dimensionName As String) As String()
      'Dim compilation As CSharpCompilation = Nothing
      Dim assemblyRefToAnalyze As MetadataReference = Nothing
      Try

        assemblyRefToAnalyze = MetadataReference.CreateFromFile(assemblyFullFilename)

        Dim tree As SyntaxTree = SyntaxFactory.ParseSyntaxTree("class C{}")

        If (_Compilation Is Nothing) Then
          Dim coreDllLocation As String = GetType(System.Object).Assembly.Location
          _Compilation = (
            CSharpCompilation.Create("InMemCompilation").
            AddSyntaxTrees(tree).
            AddReferences(MetadataReference.CreateFromFile(coreDllLocation))
          )
        End If

        'compilation = (
        '  CSharpCompilation.Create("InMemCompilation").
        '  AddSyntaxTrees(tree).
        '  AddReferences(MetadataReference.CreateFromFile(coreDllLocation)).
        '  AddReferences(assemblyRefToAnalyze)
        ')

        _Compilation.AddReferences(assemblyRefToAnalyze)
        Dim assemblySymbolToAnalyse As IAssemblySymbol = DirectCast(
          _Compilation.GetAssemblyOrModuleSymbol(assemblyRefToAnalyze), IAssemblySymbol
        )

        If (assemblySymbolToAnalyse IsNot Nothing) Then

          dimensionName = dimensionName.ToLower()
          Dim attribTypeFullName As String = GetType(AssemblyMetadataAttribute).FullName
          Dim legacyAttribTypeFullName As String = GetType(AssemblyMetadataAttribute).FullName
          Dim attribs As AttributeData() = assemblySymbolToAnalyse.GetAttributes().ToArray()
          Dim collectedExpressions As New List(Of String)

          For Each attrib As AttributeData In attribs
            If (attrib.AttributeClass.ToString() = attribTypeFullName OrElse attrib.AttributeClass.ToString() = legacyAttribTypeFullName) Then
              Dim attrDimension As String = DirectCast(attrib.ConstructorArguments(0).Value, String)
              Dim attrExpr As String = DirectCast(attrib.ConstructorArguments(1).Value, String)
              If (attrDimension.ToLower() = dimensionName) Then
                collectedExpressions.Add(attrExpr)
              End If
            End If
          Next

          Return collectedExpressions.Distinct().ToArray()
        End If

      Catch ex As BadImageFormatException 'non-.NET-dll
        'EXPECTED: happens on non-.NET-dll
      Catch ex As OutOfMemoryException
        Throw
      Catch ex As Exception
        'Diag.Error(ex)
        Diag.ErrorNotificationMethod.Invoke(ex.Message)
      Finally
        If (_Compilation IsNot Nothing AndAlso assemblyRefToAnalyze IsNot Nothing) Then
          _Compilation.RemoveReferences(assemblyRefToAnalyze)
        End If
      End Try

      Return Nothing '=ERROR
    End Function

  End Class

End Namespace

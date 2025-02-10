'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection
Imports System.Runtime
Imports System.Threading
Imports System.Threading.Tasks
Imports dnlib.DotNet

Namespace ComponentDiscovery.ClassificationDetection

  Public Class DnLibBasedAssemblyClassificationDetectionStrategy
    Implements IAssemblyClassificationDetectionStrategy

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _DefaultsIfNoAttributeFound As String()

    Public Sub New(ParamArray defaultsIfNoAttributeFound As String())

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

    'TODO AsselblyDefCache für 10 stück

    Private Function TryDetectClassificationsForAssemblyCore(
      assemblyFullFilename As String,
      taxonomicDimensionName As String,
      ByRef classifications As String()
    ) As Boolean

      Dim result As String() = Nothing
      Try

        AssemblyDef.Load(assemblyFullFilename)



        '  var assemblyDef = dnlib.DotNet.AssemblyDef.Load(fileFullName);
        'var attribs = AssemblyDef.CustomAttributes.
        'Where((a) >= a.TypeFullName == TypeOf (AssemblyMetadataAttribute).FullName).
        'Select(a >= a.ConstructorArguments[0].Value.ToString() + ": " + a.ConstructorArguments[1].Value.ToString()).
        'ToArray();

        'If (attribs.Length > 0) Then {
        '  Console.WriteLine();
        '  Console.WriteLine(Thread.CurrentThread.Name);
        '  Console.WriteLine(fileFullName);
        '}

        'foreach(var a In attribs) {
        '  Console.WriteLine("   " + a);
        '}










        result = Me.FetchMethod.Invoke(assemblyFullFilename, taxonomicDimensionName)

      Catch ex As Exception
        If (TypeOf (ex) Is TargetInvocationException) Then
          ex = DirectCast(ex, TargetInvocationException).InnerException
        End If
        Diag.Error($"AssemblyIndexer: Exception in '{NameOf(AttributeBasedAssemblyClassificationDetectionStrategy)}.{NameOf(TryDetectClassificationsForAssemblyCore)}()' (EnableAnalysisSandbox={_EnableAnalysisSandbox}) while invoking the '{NameOf(FetchMethod)}': {ex.Message}")
        Diag.Verbose(Function() ex.StackTrace)
        result = Nothing
      End Try

      If (result Is Nothing) Then
        'Nothing = ERROR
        Return False
      End If

      classifications = result
      If (classifications.Length = 0) Then
        classifications = _DefaultsIfNoAttributeFound
      End If

      Return True
    End Function

    Private Shared Function FetchClassificationExpressionsFromAssembly(assemblyFullFilename As String, dimensionName As String) As String()
      Try
        Dim assemblyToAnalyze = Assembly.LoadFile(assemblyFullFilename)
        dimensionName = dimensionName.ToLower()
        If (assemblyToAnalyze IsNot Nothing) Then

          Dim attribs = assemblyToAnalyze.GetCustomAttributes.Where(Function(a) a.GetType().Name = NameOf(AssemblyMetadataAttribute) OrElse a.GetType().Name = NameOf(AssemblyClassificationAttribute)).ToArray()

          Dim expressions = (
            attribs.OfType(Of AssemblyMetadataAttribute).
            Where(Function(a) a.Key.ToLower() = dimensionName).
            Select(Function(a) a.Value)
          )

          expressions = expressions.Union(
            attribs.OfType(Of AssemblyClassificationAttribute).
            Where(Function(a) a.TaxonomicDimensionName.ToLower() = dimensionName).
            Select(Function(a) a.ClassificationExpression)
          )

          Return expressions.Distinct().ToArray()

        End If
      Catch ex As BadImageFormatException 'non-.NET-dll
        'EXPECTED: happens on non-.NET-dll
        Diag.Verbose(Function() $"BadImageFormatException: '{assemblyFullFilename}' seems to be not a valid .NET Assembly!")
      Catch ex As Exception
        Diag.Error($"AssemblyIndexer: Exception in '{NameOf(AttributeBasedAssemblyClassificationDetectionStrategy)}.{NameOf(FetchClassificationExpressionsFromAssembly)}()': {ex.Message}")
        Diag.Verbose(Function() ex.StackTrace)
      End Try

      Return Nothing '=ERROR
    End Function

  End Class

End Namespace

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Security.Cryptography
Imports System.Text

Friend Module ExtensionMethods

  Private _Md5Provider As MD5CryptoServiceProvider = Nothing

  <Extension()>
  Public Function MD5(input As String) As String

    If (_Md5Provider Is Nothing) Then
      _Md5Provider = New MD5CryptoServiceProvider()
    End If

    Dim newdata As Byte() = Encoding.Default.GetBytes(input)
    Dim encrypted As Byte() = _Md5Provider.ComputeHash(newdata)

    Return BitConverter.ToString(encrypted).Replace("-", "").ToLower()
  End Function

  <Extension>
  Public Function FullFileName(assembly As Assembly) As String
    Dim uri As New Uri(assembly.CodeBase)
    Return uri.LocalPath
  End Function

  <Extension>
  Public Function IsParameterlessInstantiable(extendee As Type) As Boolean

    If (extendee.IsPrimitive) Then
      Return True
    End If

    If (Not extendee.IsClass OrElse extendee.IsAbstract) Then
      Return False
    End If

    If (extendee.GetConstructor(BindingFlags.CreateInstance Or BindingFlags.Public Or BindingFlags.Instance, Nothing, New Type(0 - 1) {}, Nothing) Is Nothing) Then
      Return False 'no parameterless constructor
    End If

    Return True
  End Function

  ''' <summary>
  '''   A really important secret is that the common method "GetTypes()" will fail
  '''   if the assembly contains one or more types with broken references (to non existing assemblies).
  '''   This method 'GetTypesAccessable()' will handle this problem and return at least those types,
  '''   which could be loaded!
  ''' </summary>
  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function GetTypesAccessible(assembly As Assembly) As Type()
    Return assembly.GetTypesAccessible(Sub(ex) Trace.TraceWarning(ex.Message))
  End Function

  ''' <summary>
  '''   A really important secret is that the common method "GetTypes()" will fail
  '''   if the assembly contains one or more types with broken references (to non existing assemblies).
  '''   This method 'GetTypesAccessable()' will handle this problem and return at least those types,
  '''   which could be loaded!
  ''' </summary>
  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function GetTypesAccessible(assembly As Assembly, loaderExceptionHandler As Action(Of Exception)) As Type()
    Try
      Return assembly.GetTypes()
    Catch ex As ReflectionTypeLoadException
      For Each le In ex.LoaderExceptions
        loaderExceptionHandler.Invoke(le)
      Next
      ' This ugly workarround is the only way to get the types from an asembly
      ' which contains one or more types with broken references f.e:
      '  [Type1] good!
      '  [Type2] good!
      '  [Type3] INHERITS [<non-exitings-assembly>.BaseType3] bad!
      Return ex.Types().Where(Function(t) t IsNot Nothing).ToArray()
    End Try
  End Function

  <Extension()>
  Public Function HasGenericConstraintClass(extendee As Type) As Boolean
    Return extendee.HasGenericConstraint(GenericParameterAttributes.ReferenceTypeConstraint)
  End Function

  <Extension()>
  Public Function HasGenericConstraint(extendee As Type, constraint As GenericParameterAttributes) As Boolean
    Return ((extendee.GenericParameterAttributes And constraint) = constraint)
  End Function

  <Extension()>
  Public Function HasGenericConstraintNew(extendee As Type) As Boolean
    Return extendee.HasGenericConstraint(GenericParameterAttributes.DefaultConstructorConstraint)
  End Function

  <Extension(), EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Function GetNonGenericBaseType(extendee As Type) As Type
    'actual the only indicator to identify generic parameter types
    If (extendee.FullName Is Nothing) Then
      Return extendee.BaseType.GetNonGenericBaseType()
    Else
      Return extendee
    End If
  End Function

  <Extension>
  Public Function HasDefaultConstructor(extendee As Type) As Boolean
    Return (
      extendee.IsPrimitive OrElse
      Not (
        extendee.GetConstructor(
          BindingFlags.CreateInstance Or BindingFlags.Public Or BindingFlags.Instance, Nothing, New Type(0 - 1) {}, Nothing
        ) Is Nothing
      )
    )
  End Function

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function GetMethod(methodInfos As IEnumerable(Of MethodInfo), methodName As String, ParamArray parameterTypeSignature() As Type) As MethodInfo
    Return methodInfos.FilterByName(methodName, True).Where(Function(m) m.SignatureMatches(parameterTypeSignature)).SingleOrDefault()
  End Function

  <Extension(), EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Function FilterByName(methodInfos As IEnumerable(Of MethodInfo), methodName As String, Optional ignoreCase As Boolean = True) As IEnumerable(Of MethodInfo)
    If (ignoreCase) Then
      Return From m In methodInfos Where m.Name.ToLower() = methodName.ToLower()
    Else
      Return From m In methodInfos Where m.Name = methodName
    End If
  End Function

  <Extension(), EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Function SignatureMatches(extendee As MethodInfo, ParamArray parameterTypeSignature() As Type) As Boolean
    Dim index As Integer = 0
    Dim parameters = extendee.GetParameters

    If (parameterTypeSignature Is Nothing) Then
      parameterTypeSignature = {}
    End If

    If (Not parameters.Length = parameterTypeSignature.Length) Then
      Return False
    End If

    For Each param In parameters
      If (Not param.ParameterType = parameterTypeSignature(index)) Then
        Return False
      End If
      index += 1
    Next

    Return True
  End Function

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function IsDirectReferencedBy(targetAssembly As Assembly, sourceAssembly As Assembly) As Boolean
    If (targetAssembly.FullName = sourceAssembly.FullName) Then
      Return True
    End If
    Return sourceAssembly.GetReferencedAssemblies.Where(Function(a) a.FullName = targetAssembly.FullName).Any()
  End Function

End Module

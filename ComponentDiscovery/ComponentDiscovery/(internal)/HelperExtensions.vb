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
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions

Namespace ComponentDiscovery

  Friend Module HelperExtensions

    Private _Md5Provider As MD5CryptoServiceProvider = Nothing

    <Extension()>
    Public Function MD5(input As String) As String
      SyncLock _Md5Provider
        If (_Md5Provider Is Nothing) Then
          _Md5Provider = New MD5CryptoServiceProvider()
        End If

        Dim newdata As Byte() = Encoding.Default.GetBytes(input)
        Dim encrypted As Byte() = _Md5Provider.ComputeHash(newdata)

        Return BitConverter.ToString(encrypted).Replace("-", "").ToLower()
      End SyncLock
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

      If (Not extendee.IsClass OrElse extendee.IsAbstract OrElse extendee.IsGenericTypeDefinition) Then
        Return False
      End If

      Dim ctor = extendee.GetConstructor(
      BindingFlags.CreateInstance Or BindingFlags.Public Or BindingFlags.Instance,
      Nothing,
      New Type(0 - 1) {},
      Nothing
    )

      If (ctor Is Nothing) Then
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
      Return assembly.GetTypesAccessible(Sub(ex) Diag.Warning(ex.Message))
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
    Public Function IsGenericBaseTypeOf(extendee As Type, candidate As Type) As Boolean

      If (candidate.IsGenericType AndAlso extendee.IsAssignableFrom(candidate.GetGenericTypeDefinition)) Then
        Return True
      End If

      If (extendee.IsInterface) Then
        For Each implementedInterface In candidate.GetInterfaces()
          If (extendee.IsGenericBaseTypeOf(implementedInterface)) Then
            Return True
          End If
        Next
      End If

      If (candidate.BaseType IsNot Nothing) Then
        Return extendee.IsGenericBaseTypeOf(candidate.BaseType)
      End If

      Return False
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
    Public Function FilterByName(
    methodInfos As IEnumerable(Of MethodInfo), methodName As String, Optional ignoreCase As Boolean = True
  ) As IEnumerable(Of MethodInfo)

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

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Function MatchesWildcardMask(stringToEvaluate As String, pattern As String, Optional ignoreCasing As Boolean = True) As Boolean

      Dim indexOfDoubleDot = pattern.IndexOf("..", StringComparison.Ordinal)
      If (indexOfDoubleDot >= 0) Then
        For i = indexOfDoubleDot To pattern.Length - 1
          If (Not pattern(i) = "."c) Then
            Return False
          End If
        Next
      End If

      Dim normalizedPatternString As String = Regex.Replace(pattern, "\.+$", "")
      Dim endsWithDot As Boolean = (Not normalizedPatternString.Length = pattern.Length)
      Dim endCharCount As Integer = 0

      If (endsWithDot) Then
        Dim lastNonWildcardPosition = normalizedPatternString.Length - 1

        While lastNonWildcardPosition >= 0
          Dim currentChar = normalizedPatternString(lastNonWildcardPosition)
          If (currentChar = "*"c) Then
            endCharCount += Short.MaxValue
          ElseIf (currentChar = "?"c) Then
            endCharCount += 1
          Else
            Exit While
          End If
          lastNonWildcardPosition -= 1
        End While

        If (endCharCount > 0) Then
          normalizedPatternString = normalizedPatternString.Substring(0, lastNonWildcardPosition + 1)
        End If

      End If

      Dim endsWithWildcardDot As Boolean = endCharCount > 0
      Dim endsWithDotWildcardDot As Boolean = (endsWithWildcardDot AndAlso normalizedPatternString.EndsWith("."))

      If (endsWithDotWildcardDot) Then
        normalizedPatternString = normalizedPatternString.Substring(0, normalizedPatternString.Length - 1)
      End If

      normalizedPatternString = Regex.Replace(normalizedPatternString, "(?!^)(\.\*)+$", ".*")

      Dim escapedPatternString = Regex.Escape(normalizedPatternString)
      Dim prefix As String
      Dim suffix As String

      If (endsWithDotWildcardDot) Then
        prefix = "^" & escapedPatternString
        suffix = "(\.[^.]{0," & endCharCount & "})?$"
      ElseIf (endsWithWildcardDot) Then
        prefix = "^" & escapedPatternString
        suffix = "[^.]{0," & endCharCount & "}$"
      Else
        prefix = "^" & escapedPatternString
        suffix = "$"
      End If

      If (prefix.EndsWith("\.\*") AndAlso prefix.Length > 5) Then
        prefix = prefix.Substring(0, prefix.Length - 4)
        suffix = Convert.ToString("(\..*)?") & suffix
      End If

      Dim expressionString = prefix.Replace("\*", ".*").Replace("\?", "[^.]?") & suffix

      If (ignoreCasing) Then
        Return Regex.IsMatch(stringToEvaluate, expressionString, RegexOptions.IgnoreCase)
      Else
        Return Regex.IsMatch(stringToEvaluate, expressionString)
      End If

    End Function

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Function Contains(inputArray As String(), search As String, ignoreCasing As Boolean) As Boolean

      If (Not ignoreCasing) Then
        Return inputArray.Contains(search)
      End If

      For Each element In inputArray
        If (String.Equals(element, search, StringComparison.CurrentCultureIgnoreCase)) Then
          Return True
        End If
      Next

      Return False
    End Function

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Function IsSubPathOf(subPathString As String, parentPathString As String, pathSeparatorChar As Char) As Boolean
      Dim childArray = subPathString.Split(pathSeparatorChar)
      Dim parentArray = parentPathString.Split(pathSeparatorChar)

      If (childArray.Length < parentArray.Length) Then
        Return False
      End If

      For i As Integer = 0 To (parentArray.Length - 1)
        If (Not String.Equals(parentArray(i), childArray(i), StringComparison.CurrentCultureIgnoreCase)) Then
          Return False
        End If
      Next

      Return True
    End Function

  End Module

End Namespace

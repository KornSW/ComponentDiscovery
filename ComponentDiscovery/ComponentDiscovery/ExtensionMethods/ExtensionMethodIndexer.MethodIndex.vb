'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.CompilerServices

Partial Class ExtensionMethodIndexer

  Private Class MethodIndex
    Implements IEnumerable(Of MethodInfo)

#Region " Static Part "

    Private Shared _Instances As New Dictionary(Of Type, MethodIndex)
    Private Shared _AppDomainListenerEnabled As Boolean = False

    Public Shared Function OfType(Of TExtendee)() As MethodIndex
      Return OfType(GetType(TExtendee))
    End Function

    Public Shared Function OfType(tExtendee As Type) As MethodIndex

      If (_AppDomainListenerEnabled = False) Then
        _AppDomainListenerEnabled = True
        AddHandler AppDomain.CurrentDomain.AssemblyLoad, AddressOf AppDomain_AssemblyLoad
      End If

      If (_Instances.ContainsKey(tExtendee)) Then
        Return _Instances(tExtendee)
      Else
        Dim newInstance As New MethodIndex(tExtendee, AddressOf MethodIndex.OfType)
        _Instances.Add(tExtendee, newInstance)
        For Each ass In AppDomain.CurrentDomain.GetAssemblies().ToArray()
          If (tExtendee.Assembly.IsDirectReferencedBy(ass)) Then
            newInstance.AddAssembly(ass)
          End If
        Next
        Return newInstance
      End If
    End Function

    Private Shared Sub AppDomain_AssemblyLoad(sender As Object, args As AssemblyLoadEventArgs)
      Dim ass = args.LoadedAssembly
      For Each alreadyAnalyzedExtendee In _Instances.Keys.ToArray()
        If (alreadyAnalyzedExtendee.Assembly.IsDirectReferencedBy(ass)) Then
          _Instances(alreadyAnalyzedExtendee).AddAssembly(ass)
        End If
      Next
    End Sub

#End Region

#Region " Declarations & Constructor "

    Private _ExtendeeType As Type
    Private _ExtensionMethods As New List(Of MethodInfo)
    Private _InterfaceExtensionMethods As New Dictionary(Of Type, MethodIndex)
    Private _BaseTypeExtensionMethods As MethodIndex = Nothing

    Friend Sub New(extendeeType As Type, indexLookupMethod As Func(Of Type, MethodIndex))
      _ExtendeeType = extendeeType

      If (extendeeType.BaseType IsNot Nothing AndAlso Not extendeeType.BaseType = GetType(Object)) Then
        _BaseTypeExtensionMethods = indexLookupMethod.Invoke(extendeeType.BaseType)
      End If

      For Each implementedInterface In extendeeType.GetInterfaces
        _InterfaceExtensionMethods.Add(implementedInterface, indexLookupMethod.Invoke(implementedInterface))
      Next

    End Sub

#End Region

#Region " Evaluation (Type Crawler) "

    Friend Sub AddAssembly(a As Assembly)

      If (a.IsDynamic) Then
        Exit Sub
      End If

      Dim allTypes() As Type

      Try
        allTypes = a.GetTypes()
      Catch ex As ReflectionTypeLoadException
        For Each le In ex.LoaderExceptions
          Diag.Error("LoaderException bei GetTypes für Assembly '" + a.FullName + "': " + le.ToString())
        Next
        'This ugly workarround is the only way to get the types from a assembly
        'which contains one or more types with broken references for example:
        '  [Type1] good!
        '  [Type2] good!
        '  [Type3] INHERITS [<non-exitings-assembly>.BaseType3] bad!
        'In this case the GetTypes() method will throw this exception. To get
        'the good types anyway, we need to pick and clean the partially result:
        Dim partiallyLoadedTypeList As Type() = ex.Types
        allTypes = partiallyLoadedTypeList.Where(Function(t) t IsNot Nothing).ToArray()
      End Try

      Dim allExtensionMethodsOfAssembly As New List(Of MethodInfo)

      For Each type In allTypes
        If (type.IsSealed AndAlso Not type.IsGenericType AndAlso Not type.IsNested) Then
          For Each method In type.GetMethods(BindingFlags.[Static] Or BindingFlags.[Public] Or BindingFlags.NonPublic)
            Try
              If (method.IsDefined(GetType(ExtensionAttribute), False) AndAlso method.GetParameters.Any()) Then
                allExtensionMethodsOfAssembly.Add(method)
              End If
            Catch
              'this occours when the containing assembly of a parameter type is not available
            End Try
          Next
        End If
      Next

      Dim matchingExtensionMethods =
      allExtensionMethodsOfAssembly.Where(
        Function(method)
          Dim firstParam = method.GetParameters()(0)
          Select Case method.GetGenericArguments().Count
            Case 0
              Return firstParam.ParameterType = _ExtendeeType
            Case 1
              Dim genArgType = method.GetGenericArguments(0)

              'lets find something like:
              ' Function MyExtension(Of T As ExtendeeType)(arg1 as T)

              If (Not genArgType = firstParam.ParameterType) Then
                'the generic argument is not used for the first param...
                Return False
              End If

              If (Not _ExtendeeType.IsClass AndAlso genArgType.HasGenericConstraintClass()) Then
                'the extension is written only for classes, but our extendee is no class
                Return False
              End If

              If (Not genArgType.GetNonGenericBaseType().IsAssignableFrom(_ExtendeeType)) Then
                'there is a generic type constraint, which requires a basetype that is not assignable from our extendee 
                Return False
              End If

              If (genArgType.HasGenericConstraintNew()) Then
                'the extension is written only for classes with a default constructor
                If (Not _ExtendeeType.IsClass) Then
                  'our extendee is no class
                  Return False
                End If
                If (Not _ExtendeeType.HasDefaultConstructor()) Then
                  'our extendee has no default constructor
                  Return False
                End If
              End If

              For Each interfaceConstraint In genArgType.GetInterfaces()
                If (Not interfaceConstraint.IsAssignableFrom(_ExtendeeType)) Then
                  'there is a generic type constraint, which requires a interface that is not assignable from our extendee 
                  Return False
                End If
              Next

              Return True
            Case Else
              Return False
          End Select
        End Function)

      For Each matchingExtensionMethod In matchingExtensionMethods
        If (matchingExtensionMethod.GetGenericArguments().Any()) Then
          'if we have an generic extension, the we need to generate a specific instance
          Try
            Dim genericMethod = matchingExtensionMethod.MakeGenericMethod(_ExtendeeType)
            _ExtensionMethods.Add(genericMethod)
          Catch
            'this occours due missmatches of special type constraints
            'extension methods doing such hardcore magic with generic constraints,
            'will not be included in our search result
          End Try
        Else
          _ExtensionMethods.Add(matchingExtensionMethod)
        End If
      Next

    End Sub

#End Region

#Region " Consume "

    Private Function GetEnumeratorUntyped() As IEnumerator Implements IEnumerable.GetEnumerator
      Return Me.GetEnumerator
    End Function

    Private Function GetEnumerator() As IEnumerator(Of MethodInfo) Implements IEnumerable(Of MethodInfo).GetEnumerator
      Dim result As IEnumerable(Of MethodInfo)
      result = _ExtensionMethods

      'add the extensionmethods from the baseclass
      If (_BaseTypeExtensionMethods IsNot Nothing) Then
        result = result.Union(_BaseTypeExtensionMethods)
      End If

      For Each interfaceExtensionMethods In _InterfaceExtensionMethods.Values
        result = result.Union(interfaceExtensionMethods)
      Next

      Return result.Distinct().GetEnumerator()
    End Function

    Default ReadOnly Property Item(methodName As String, ParamArray parameterTypeSignature() As Type) As MethodInfo
      Get
        Return Me.GetMethod(methodName, parameterTypeSignature)
      End Get
    End Property

#End Region

  End Class

End Class

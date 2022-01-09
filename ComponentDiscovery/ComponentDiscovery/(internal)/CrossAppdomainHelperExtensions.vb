'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Text

Namespace ComponentDiscovery
#If NET461 Then

  Friend Module ExtensionsForAppDomain

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Sub Invoke(appDomain As AppDomain, method As Action)
      CrossAppDomainActionProxy.Invoke(appDomain, method)
    End Sub

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Sub Invoke(Of TArg1)(appDomain As AppDomain, method As Action(Of TArg1), arg1 As TArg1)
      CrossAppdomainActionProxy(Of TArg1).Invoke(appDomain, method, arg1)
    End Sub

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Sub Invoke(Of TArg1, TArg2)(appDomain As AppDomain, method As Action(Of TArg1, TArg2), arg1 As TArg1, arg2 As TArg2)
      CrossAppdomainActionProxy(Of TArg1, TArg2).Invoke(appDomain, method, arg1, arg2)
    End Sub

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Function Invoke(Of TReturn)(appDomain As AppDomain, method As Func(Of TReturn)) As TReturn
      Return CrossAppdomainFuncProxy(Of TReturn).Invoke(appDomain, method)
    End Function

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Function Invoke(Of TArg1, TReturn)(appDomain As AppDomain, method As Func(Of TArg1, TReturn), arg1 As TArg1) As TReturn
      Return CrossAppdomainFuncProxy(Of TArg1, TReturn).Invoke(appDomain, method, arg1)
    End Function

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Function Invoke(Of TArg1, TArg2, TReturn)(
      appDomain As AppDomain, method As Func(Of TArg1, TArg2, TReturn), arg1 As TArg1, arg2 As TArg2
    ) As TReturn

      Return CrossAppdomainFuncProxy(Of TArg1, TArg2, TReturn).Invoke(appDomain, method, arg1, arg2)
    End Function

  End Module

#Region " CrossAppdomainActionProxy '0 "

  <Serializable>
  Friend Class CrossAppDomainActionProxy

    Public Shared Sub Invoke(targetDomain As AppDomain, targetMethod As Action)

      Dim crossDomainCallId As String = Guid.NewGuid.ToString()

      Dim xadActionUntyped As Object =
      targetDomain.CreateInstanceAndUnwrap(
        GetType(CrossAppDomainActionProxy).Assembly.FullName,
        GetType(CrossAppDomainActionProxy).FullName, False,
        BindingFlags.CreateInstance,
        Type.DefaultBinder,
        {targetMethod, crossDomainCallId},
        System.Globalization.CultureInfo.CurrentUICulture,
        {}
      )

      Dim xadAction As CrossAppDomainActionProxy = DirectCast(xadActionUntyped, CrossAppDomainActionProxy)
      targetDomain.DoCallBack(New CrossAppDomainDelegate(AddressOf xadAction.CallEntry))

      Dim sharedDataBuffer = targetDomain.GetData(crossDomainCallId)
      If (TypeOf (sharedDataBuffer) Is Exception) Then
        Dim ex = DirectCast(sharedDataBuffer, Exception)
        Throw New TargetInvocationException(ex.Message, ex)
      End If

    End Sub

    Private _TargetMethod As Action
    Private _CrossDomainCallId As String

    Public Sub New(targetMethod As Action, crossDomainCallId As String)
      _TargetMethod = targetMethod
      _CrossDomainCallId = crossDomainCallId
    End Sub

    Public Sub CallEntry()
      Try
        _TargetMethod.Invoke()
      Catch ex As Exception
        AppDomain.CurrentDomain.SetData(_CrossDomainCallId, ex)
      End Try
    End Sub

  End Class

#End Region

#Region " CrossAppdomainActionProxy '1 "

  <Serializable>
  Friend Class CrossAppdomainActionProxy(Of TArg1)

    <Serializable>
    Friend Class CrossAppdomainsharedData
      Public Property Arg1 As TArg1
    End Class

    Public Shared Sub Invoke(targetDomain As AppDomain, targetMethod As Action(Of TArg1), arg1 As TArg1)

      Dim crossDomainCallId As String = Guid.NewGuid.ToString()

      Dim sharedData As New CrossAppdomainsharedData
      With sharedData
        .Arg1 = arg1
      End With
      targetDomain.SetData(crossDomainCallId, sharedData)

      Dim xadActionUntyped As Object =
      targetDomain.CreateInstanceAndUnwrap(
        GetType(CrossAppdomainActionProxy(Of TArg1)).Assembly.FullName,
        GetType(CrossAppdomainActionProxy(Of TArg1)).FullName, False,
        BindingFlags.CreateInstance,
        Type.DefaultBinder,
        {targetMethod, crossDomainCallId},
        System.Globalization.CultureInfo.CurrentUICulture,
        {}
      )

      Dim xadAction As CrossAppdomainActionProxy(Of TArg1) = DirectCast(xadActionUntyped, CrossAppdomainActionProxy(Of TArg1))
      targetDomain.DoCallBack(New CrossAppDomainDelegate(AddressOf xadAction.CallEntry))

      Dim sharedDataBuffer = targetDomain.GetData(crossDomainCallId)
      If (TypeOf (sharedDataBuffer) Is Exception) Then
        Dim ex = DirectCast(sharedDataBuffer, Exception)
        Throw New TargetInvocationException(ex.Message, ex)
      End If

      targetDomain.SetData(crossDomainCallId, Nothing)
    End Sub

    Private _TargetMethod As Action(Of TArg1)
    Private _CrossDomainCallId As String

    Public Sub New(targetMethod As Action(Of TArg1), crossDomainCallId As String)
      _TargetMethod = targetMethod
      _CrossDomainCallId = crossDomainCallId
    End Sub

    Public Sub CallEntry()
      Try
        Dim sharedData As CrossAppdomainsharedData
        sharedData = DirectCast(AppDomain.CurrentDomain.GetData(_CrossDomainCallId), CrossAppdomainsharedData)
        With sharedData
          _TargetMethod.Invoke(.Arg1)
        End With
      Catch ex As Exception
        AppDomain.CurrentDomain.SetData(_CrossDomainCallId, ex)
      End Try
    End Sub

  End Class

#End Region

#Region " CrossAppdomainActionProxy '2 "

  <Serializable>
  Friend Class CrossAppdomainActionProxy(Of TArg1, TArg2)

    <Serializable>
    Friend Class CrossAppdomainsharedData
      Public Property Arg1 As TArg1
      Public Property Arg2 As TArg2
    End Class

    Public Shared Sub Invoke(targetDomain As AppDomain, targetMethod As Action(Of TArg1, TArg2), arg1 As TArg1, arg2 As TArg2)

      Dim crossDomainCallId As String = Guid.NewGuid.ToString()

      Dim sharedData As New CrossAppdomainsharedData
      With sharedData
        .Arg1 = arg1
        .Arg2 = arg2
      End With
      targetDomain.SetData(crossDomainCallId, sharedData)

      Dim xadActionUntyped As Object =
      targetDomain.CreateInstanceAndUnwrap(
        GetType(CrossAppdomainActionProxy(Of TArg1, TArg2)).Assembly.FullName,
        GetType(CrossAppdomainActionProxy(Of TArg1, TArg2)).FullName, False,
        BindingFlags.CreateInstance,
        Type.DefaultBinder,
        {targetMethod, crossDomainCallId},
        System.Globalization.CultureInfo.CurrentUICulture,
        {}
      )

      Dim xadAction As CrossAppdomainActionProxy(Of TArg1, TArg2) = DirectCast(xadActionUntyped, CrossAppdomainActionProxy(Of TArg1, TArg2))
      targetDomain.DoCallBack(New CrossAppDomainDelegate(AddressOf xadAction.CallEntry))

      Dim sharedDataBuffer = targetDomain.GetData(crossDomainCallId)
      If (TypeOf (sharedDataBuffer) Is Exception) Then
        Dim ex = DirectCast(sharedDataBuffer, Exception)
        Throw New TargetInvocationException(ex.Message, ex)
      End If

      targetDomain.SetData(crossDomainCallId, Nothing)
    End Sub

    Private _TargetMethod As Action(Of TArg1, TArg2)
    Private _CrossDomainCallId As String

    Public Sub New(targetMethod As Action(Of TArg1, TArg2), crossDomainCallId As String)
      _TargetMethod = targetMethod
      _CrossDomainCallId = crossDomainCallId
    End Sub

    Public Sub CallEntry()
      Try
        Dim sharedData As CrossAppdomainsharedData
        sharedData = DirectCast(AppDomain.CurrentDomain.GetData(_CrossDomainCallId), CrossAppdomainsharedData)
        With sharedData
          _TargetMethod.Invoke(.Arg1, .Arg2)
        End With
      Catch ex As Exception
        AppDomain.CurrentDomain.SetData(_CrossDomainCallId, ex)
      End Try
    End Sub

  End Class

#End Region

#Region " CrossAppdomainFuncProxy '0 "

  <Serializable>
  Friend Class CrossAppdomainFuncProxy(Of TReturn)

    <Serializable>
    Friend Class CrossAppdomainsharedData
      Public Property Result As TReturn
    End Class

    Public Shared Function Invoke(targetDomain As AppDomain, targetMethod As Func(Of TReturn)) As TReturn

      Dim crossDomainCallId As String = Guid.NewGuid.ToString()

      Dim sharedData As New CrossAppdomainsharedData
      targetDomain.SetData(crossDomainCallId, sharedData)

      Dim xadActionUntyped As Object =
      targetDomain.CreateInstanceAndUnwrap(
        GetType(CrossAppdomainFuncProxy(Of TReturn)).Assembly.FullName,
        GetType(CrossAppdomainFuncProxy(Of TReturn)).FullName, False,
        BindingFlags.CreateInstance,
        Type.DefaultBinder,
        {targetMethod, crossDomainCallId},
        System.Globalization.CultureInfo.CurrentUICulture,
        {}
      )

      Dim xadAction As CrossAppdomainFuncProxy(Of TReturn) = DirectCast(xadActionUntyped, CrossAppdomainFuncProxy(Of TReturn))
      targetDomain.DoCallBack(New CrossAppDomainDelegate(AddressOf xadAction.CallEntry))

      Dim sharedDataBuffer = targetDomain.GetData(crossDomainCallId)
      If (TypeOf (sharedDataBuffer) Is Exception) Then
        Dim ex = DirectCast(sharedDataBuffer, Exception)
        Throw New TargetInvocationException(ex.Message, ex)
      End If
      sharedData = DirectCast(sharedDataBuffer, CrossAppdomainsharedData)

      targetDomain.SetData(crossDomainCallId, Nothing)
      Return sharedData.Result
    End Function

    Private _TargetMethod As Func(Of TReturn)
    Private _CrossDomainCallId As String

    Public Sub New(targetMethod As Func(Of TReturn), crossDomainCallId As String)
      _TargetMethod = targetMethod
      _CrossDomainCallId = crossDomainCallId
    End Sub

    Public Sub CallEntry()
      Try
        Dim sharedData As CrossAppdomainsharedData
        sharedData = DirectCast(AppDomain.CurrentDomain.GetData(_CrossDomainCallId), CrossAppdomainsharedData)
        With sharedData
          .Result = _TargetMethod.Invoke()
        End With
        AppDomain.CurrentDomain.SetData(_CrossDomainCallId, sharedData)
      Catch ex As Exception
        AppDomain.CurrentDomain.SetData(_CrossDomainCallId, ex)
      End Try
    End Sub

  End Class

#End Region

#Region " CrossAppdomainFuncProxy '1 "

  <Serializable>
  Friend Class CrossAppdomainFuncProxy(Of TArg1, TReturn)

    <Serializable>
    Friend Class CrossAppdomainsharedData
      Public Property Arg1 As TArg1
      Public Property Result As TReturn
    End Class

    Public Shared Function Invoke(targetDomain As AppDomain, targetMethod As Func(Of TArg1, TReturn), arg1 As TArg1) As TReturn

      Dim crossDomainCallId As String = Guid.NewGuid.ToString()

      Dim sharedData As New CrossAppdomainsharedData
      With sharedData
        .Arg1 = arg1
      End With
      targetDomain.SetData(crossDomainCallId, sharedData)

      Dim xadActionUntyped As Object =
      targetDomain.CreateInstanceAndUnwrap(
        GetType(CrossAppdomainFuncProxy(Of TArg1, TReturn)).Assembly.FullName,
        GetType(CrossAppdomainFuncProxy(Of TArg1, TReturn)).FullName, False,
        BindingFlags.CreateInstance,
        Type.DefaultBinder,
        {targetMethod, crossDomainCallId},
        System.Globalization.CultureInfo.CurrentUICulture,
        {}
      )

      Dim xadAction As CrossAppdomainFuncProxy(Of TArg1, TReturn) = DirectCast(xadActionUntyped, CrossAppdomainFuncProxy(Of TArg1, TReturn))
      targetDomain.DoCallBack(New CrossAppDomainDelegate(AddressOf xadAction.CallEntry))

      Dim sharedDataBuffer = targetDomain.GetData(crossDomainCallId)
      If (TypeOf (sharedDataBuffer) Is Exception) Then
        Dim ex = DirectCast(sharedDataBuffer, Exception)
        Throw New TargetInvocationException(ex.Message, ex)
      End If
      sharedData = DirectCast(sharedDataBuffer, CrossAppdomainsharedData)

      targetDomain.SetData(crossDomainCallId, Nothing)
      Return sharedData.Result
    End Function

    Private _TargetMethod As Func(Of TArg1, TReturn)
    Private _CrossDomainCallId As String

    Public Sub New(targetMethod As Func(Of TArg1, TReturn), crossDomainCallId As String)
      _TargetMethod = targetMethod
      _CrossDomainCallId = crossDomainCallId
    End Sub

    Public Sub CallEntry()
      Try
        Dim sharedData As CrossAppdomainsharedData
        sharedData = DirectCast(AppDomain.CurrentDomain.GetData(_CrossDomainCallId), CrossAppdomainsharedData)
        With sharedData
          .Result = _TargetMethod.Invoke(.Arg1)
        End With
        AppDomain.CurrentDomain.SetData(_CrossDomainCallId, sharedData)
      Catch ex As Exception
        AppDomain.CurrentDomain.SetData(_CrossDomainCallId, ex)
      End Try
    End Sub

  End Class

#End Region

#Region " CrossAppdomainFuncProxy '2 "

  <Serializable>
  Friend Class CrossAppdomainFuncProxy(Of TArg1, TArg2, TReturn)

    <Serializable>
    Friend Class CrossAppdomainsharedData
      Public Property Arg1 As TArg1
      Public Property Arg2 As TArg2
      Public Property Result As TReturn
    End Class

    Public Shared Function Invoke(targetDomain As AppDomain, targetMethod As Func(Of TArg1, TArg2, TReturn), arg1 As TArg1, arg2 As TArg2) As TReturn

      Dim crossDomainCallId As String = Guid.NewGuid.ToString()

      Dim sharedData As New CrossAppdomainsharedData
      With sharedData
        .Arg1 = arg1
        .Arg2 = arg2
      End With
      targetDomain.SetData(crossDomainCallId, sharedData)

      Dim xadActionUntyped As Object =
      targetDomain.CreateInstanceAndUnwrap(
        GetType(CrossAppdomainFuncProxy(Of TArg1, TArg2, TReturn)).Assembly.FullName,
        GetType(CrossAppdomainFuncProxy(Of TArg1, TArg2, TReturn)).FullName, False,
        BindingFlags.CreateInstance,
        Type.DefaultBinder,
        {targetMethod, crossDomainCallId},
        System.Globalization.CultureInfo.CurrentUICulture,
        {}
      )

      Dim xadAction As CrossAppdomainFuncProxy(Of TArg1, TArg2, TReturn) = DirectCast(xadActionUntyped, CrossAppdomainFuncProxy(Of TArg1, TArg2, TReturn))
      targetDomain.DoCallBack(New CrossAppDomainDelegate(AddressOf xadAction.CallEntry))

      Dim sharedDataBuffer = targetDomain.GetData(crossDomainCallId)
      If (TypeOf (sharedDataBuffer) Is Exception) Then
        Dim ex = DirectCast(sharedDataBuffer, Exception)
        Throw New TargetInvocationException(ex.Message, ex)
      End If
      sharedData = DirectCast(sharedDataBuffer, CrossAppdomainsharedData)

      targetDomain.SetData(crossDomainCallId, Nothing)
      Return sharedData.Result
    End Function

    Private _TargetMethod As Func(Of TArg1, TArg2, TReturn)
    Private _CrossDomainCallId As String

    Public Sub New(targetMethod As Func(Of TArg1, TArg2, TReturn), crossDomainCallId As String)
      _TargetMethod = targetMethod
      _CrossDomainCallId = crossDomainCallId
    End Sub

    Public Sub CallEntry()
      Try
        Dim sharedData As CrossAppdomainsharedData
        sharedData = DirectCast(AppDomain.CurrentDomain.GetData(_CrossDomainCallId), CrossAppdomainsharedData)
        With sharedData
          .Result = _TargetMethod.Invoke(.Arg1, .Arg2)
        End With
        AppDomain.CurrentDomain.SetData(_CrossDomainCallId, sharedData)
      Catch ex As Exception
        AppDomain.CurrentDomain.SetData(_CrossDomainCallId, ex)
      End Try
    End Sub

  End Class

#End Region

#End If
End Namespace

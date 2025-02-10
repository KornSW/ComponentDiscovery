'Imports System.Collections.Generic
'Imports System.Linq
'Imports System.Runtime.InteropServices
'Imports System
'Imports System.Reflection

'Namespace dnlib.DotNet

'  Public MustInherit Class AssemblyDef

'    Public Shared Function Load(fileName As String, Optional options As ModuleCreationOptions = Nothing) As AssemblyDef
'      If (fileName Is Nothing) Then
'        Throw New ArgumentNullException(NameOf(fileName),)
'      End If

'      Dim [module] As ModuleDef = Nothing

'      Try
'        [module] = ModuleDefMD.Load(fileName, options)

'        Dim asm As AssemblyDef = [module].Assembly
'        If (asm IsNot Nothing) Then
'          Return asm
'        End If

'      Catch ex As Exception
'        If ([module] IsNot Nothing) Then
'          [module].Dispose()
'        End If
'      End Try

'      Throw New BadImageFormatException($"{fileName} is only a .NET module, not a .NET assembly. Use ModuleDef.Load().");
'		End Function






'  End Class

'End Namespace

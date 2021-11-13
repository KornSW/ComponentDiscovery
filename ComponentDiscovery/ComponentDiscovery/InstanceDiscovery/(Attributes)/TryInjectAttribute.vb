'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System

Namespace Composition.InstanceDiscovery

  ''' <summary>
  ''' Declares the demand to get an discovered instance injected if possible.
  ''' If the instance discovery fails, the behaviour for PARAMETER-INJECTION will be that
  ''' a null-reference will be passed into required parameters and optional parameters will not be provided.
  ''' Any MEMBER-INJECTION will simply be skipped.
  ''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
  ''' </summary>
  <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Parameter, AllowMultiple:=False)>
  Public Class TryInjectAttribute
    Inherits Attribute

    ''' <summary>
    ''' Declares the demand to get an discovered instance injected if possible.
    ''' If the instance discovery fails, the behaviour for PARAMETER-INJECTION will be that
    ''' a null-reference will be passed into required parameters and optional parameters will not be provided.
    ''' Any MEMBER-INJECTION will simply be skipped.
    ''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
    ''' </summary>
    ''' <param name="typeToDiscover">an more specific type which shuld be requested from the instance discovery framework (and which is assignable to the declative type on the target)</param>
    Public Sub New(Optional typeToDiscover As Type = Nothing)
      Me.TypeToDiscover = typeToDiscover
    End Sub

    Public ReadOnly Property TypeToDiscover As Type

  End Class

End Namespace

'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System

Namespace Composition.InstanceDiscovery

  ''' <summary>
  ''' Declares the demand to get an discovered instance injected.
  ''' If the instance discovery fails, an Exception will be thrown!
  ''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
  ''' </summary>
  <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Parameter, AllowMultiple:=False)>
  Public Class InjectAttribute
    Inherits Attribute

    ''' <summary>
    ''' Declares the demand to get an discovered instance injected.
    ''' If the instance discovery fails, an Exception will be thrown!
    ''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
    ''' </summary>
    ''' <param name="typeToDiscover">an more specific type which shuld be requested from the instance discovery framework (and which is assignable to the declative type on the target)</param>
    Public Sub New(Optional typeToDiscover As Type = Nothing)
      Me.TypeToDiscover = typeToDiscover
    End Sub

    Public ReadOnly Property TypeToDiscover As Type

  End Class

End Namespace

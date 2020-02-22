'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System

<AttributeUsage(
  AttributeTargets.Class Or AttributeTargets.Interface Or AttributeTargets.Module Or AttributeTargets.Struct Or AttributeTargets.Enum,
  AllowMultiple:=True, Inherited:=True
)>
Public Class IgnoreNamespaceClassificationsAttribute
  Inherits Attribute

  Private _TaxonomicDimensionName As String

  ''' <param name="taxonomicDimensionName"></param>
  Public Sub New(taxonomicDimensionName As String)
    MyBase.New()

    _TaxonomicDimensionName = taxonomicDimensionName

  End Sub

  Public ReadOnly Property TaxonomicDimensionName As String
    Get
      Return _TaxonomicDimensionName
    End Get
  End Property

End Class

'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System

<AttributeUsage(AttributeTargets.Assembly, AllowMultiple:=True)>
Public Class AssemblyClassificationAttribute
  Inherits Attribute

  Private _TaxonomicDimensionName As String
  Private _ClassificationExpression As String

  Public Sub New(taxonomicDimensionName As String, classificationExpression As String)
    MyBase.New()

    _TaxonomicDimensionName = taxonomicDimensionName
    _ClassificationExpression = classificationExpression

  End Sub

  Public ReadOnly Property TaxonomicDimensionName As String
    Get
      Return _TaxonomicDimensionName
    End Get
  End Property

  Public ReadOnly Property ClassificationExpression As String
    Get
      Return _ClassificationExpression
    End Get
  End Property

End Class

Imports System

<AttributeUsage(AttributeTargets.Class Or AttributeTargets.Interface Or AttributeTargets.Module Or AttributeTargets.Struct Or AttributeTargets.Enum, AllowMultiple:=True)>
Public Class TypeClassificationAttribute
  Inherits Attribute

  Private _DimensionName As String
  Private _ClassificationExpression As String

  ''' <param name="dimensionName"></param>
  ''' <param name="classificationExpression"></param>
  Public Sub New(dimensionName As String, classificationExpression As String)
    MyBase.New()

    _DimensionName = dimensionName
    _ClassificationExpression = classificationExpression

  End Sub

  Public ReadOnly Property DimensionName As String
    Get
      Return _DimensionName
    End Get
  End Property

  Public ReadOnly Property ClassificationExpression As String
    Get
      Return _ClassificationExpression
    End Get
  End Property

End Class

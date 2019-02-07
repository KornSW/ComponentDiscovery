Imports System

<AttributeUsage(AttributeTargets.Class Or AttributeTargets.Interface Or AttributeTargets.Module Or AttributeTargets.Struct Or AttributeTargets.Enum, AllowMultiple:=True)>
Public Class TypeClassificationAttribute
  Inherits Attribute

  Private _SemanticDimensionName As String
  Private _ClassificationExpression As String

  ''' <param name="semanticDimensionName"></param>
  ''' <param name="classificationExpression"></param>
  Public Sub New(semanticDimensionName As String, classificationExpression As String)
    MyBase.New()

    _SemanticDimensionName = semanticDimensionName
    _ClassificationExpression = classificationExpression

  End Sub

  Public ReadOnly Property SemanticDimensionName As String
    Get
      Return _SemanticDimensionName
    End Get
  End Property

  Public ReadOnly Property ClassificationExpression As String
    Get
      Return _ClassificationExpression
    End Get
  End Property

End Class

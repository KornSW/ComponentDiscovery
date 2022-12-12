'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports Newtonsoft.Json

Namespace ComponentDiscovery.ClassificationDetection

  Friend Class ManifestParser

    Public Delegate Sub PerExpressionCallback(dimensionName As String, classificationName As String, namespaceIncludePatterns As String(), namespaceExludePatterns As String())
    Public Delegate Sub EmptyDimensionCallback(dimensionName As String)

    Public Shared Sub Parse(rawJsonContent As String, perExpressionCallback As PerExpressionCallback, emptyDimensionCallback As EmptyDimensionCallback)

      Dim level As JsonReadingLevel = JsonReadingLevel.Root
      Dim currentDimensionName As String = Nothing

      'Dim deseriualizedStructure As New Dictionary(Of String, Dictionary(Of String, NamespaceFilter))

      Dim propName As String = String.Empty

      Dim currentDetailExpression As String = String.Empty
      Dim currentDetailNsBlacklist As New List(Of String)
      Dim currentDetailNsWhitelist As New List(Of String)
      Dim expressionCounterForCurrentDimension As Integer = 0
      Using sr As New StringReader(rawJsonContent)
        Using jr As New Newtonsoft.Json.JsonTextReader(sr)

          While jr.Read()

            If (jr.TokenType = JsonToken.PropertyName) Then
              propName = jr.Value.ToString()
            End If

            Select Case level
              Case JsonReadingLevel.Root
                If (jr.TokenType = JsonToken.StartObject AndAlso Not String.IsNullOrWhiteSpace(propName)) Then
                  If (propName.Equals("componentDiscovery", StringComparison.InvariantCultureIgnoreCase)) Then
                    level = JsonReadingLevel.DimensionNames
                  Else
                    jr.Skip()
                  End If
                End If
              Case JsonReadingLevel.DimensionNames
                If (jr.TokenType = JsonToken.String) Then
                  currentDimensionName = propName
                  If (Not String.IsNullOrWhiteSpace(jr.Value.ToString())) Then
                    perExpressionCallback.Invoke(currentDimensionName, jr.Value.ToString(), {}, {})
                  Else
                    emptyDimensionCallback.Invoke(currentDimensionName)
                  End If
                ElseIf (jr.TokenType = JsonToken.Null) Then
                  currentDimensionName = propName
                  emptyDimensionCallback.Invoke(currentDimensionName)
                ElseIf (jr.TokenType = JsonToken.Integer) Then
                  currentDimensionName = propName
                  perExpressionCallback.Invoke(currentDimensionName, jr.Value.ToString(), {}, {})
                ElseIf (jr.TokenType = JsonToken.StartArray) Then
                  currentDimensionName = propName
                  level = JsonReadingLevel.ExpressionsArray
                  expressionCounterForCurrentDimension = 0
                ElseIf (jr.TokenType = JsonToken.EndObject) Then
                  level = JsonReadingLevel.Root 'leaving "componentDiscovery"
                  expressionCounterForCurrentDimension = 0
                  currentDimensionName = Nothing
                ElseIf (jr.TokenType = JsonToken.StartObject) Then
                  currentDimensionName = propName
                  emptyDimensionCallback.Invoke(currentDimensionName)
                  jr.Skip() 'sub-objects are not allowed here
                End If

              Case JsonReadingLevel.ExpressionsArray
                If (jr.TokenType = JsonToken.String) Then
                  If (Not String.IsNullOrWhiteSpace(jr.Value.ToString())) Then
                    perExpressionCallback.Invoke(currentDimensionName, jr.Value.ToString(), {}, {})
                    expressionCounterForCurrentDimension += 1
                  End If
                ElseIf (jr.TokenType = JsonToken.Null) Then
                ElseIf (jr.TokenType = JsonToken.Integer) Then
                  perExpressionCallback.Invoke(currentDimensionName, jr.Value.ToString(), {}, {})
                  expressionCounterForCurrentDimension += 1
                ElseIf (jr.TokenType = JsonToken.StartObject) Then
                  level = JsonReadingLevel.ExpressionDetailObject
                  expressionCounterForCurrentDimension += 1
                  currentDetailExpression = String.Empty
                  currentDetailNsWhitelist.Clear()
                  currentDetailNsBlacklist.Clear()
                ElseIf (jr.TokenType = JsonToken.EndArray) Then
                  If (expressionCounterForCurrentDimension = 0) Then
                    emptyDimensionCallback.Invoke(currentDimensionName)
                  End If
                  level = JsonReadingLevel.DimensionNames
                End If


              Case JsonReadingLevel.ExpressionDetailObject
                If (jr.TokenType = JsonToken.StartObject) Then
                  jr.Skip()

                ElseIf (jr.TokenType = JsonToken.String) Then
                  If (propName.Equals("expression", StringComparison.InvariantCultureIgnoreCase)) Then
                    currentDetailExpression = jr.Value.ToString()
                  ElseIf (propName.Equals("namespace", StringComparison.InvariantCultureIgnoreCase)) Then
                    currentDetailNsWhitelist.Add(jr.Value.ToString())
                  ElseIf (propName.Equals("exclude", StringComparison.InvariantCultureIgnoreCase)) Then
                    currentDetailNsBlacklist.Add(jr.Value.ToString())
                  End If

                ElseIf (jr.TokenType = JsonToken.Null) Then
                  If (propName.Equals("expression", StringComparison.InvariantCultureIgnoreCase)) Then
                    currentDetailExpression = String.Empty
                  End If

                ElseIf (jr.TokenType = JsonToken.Integer) Then
                  If (propName.Equals("expression", StringComparison.InvariantCultureIgnoreCase)) Then
                    currentDetailExpression = jr.Value.ToString()
                  End If

                ElseIf (jr.TokenType = JsonToken.StartArray) Then
                  If (propName.Equals("namespace", StringComparison.InvariantCultureIgnoreCase)) Then
                    level = JsonReadingLevel.NsWhitelistArray
                  ElseIf (propName.Equals("exclude", StringComparison.InvariantCultureIgnoreCase)) Then
                    level = JsonReadingLevel.NsBlacklistArray
                  Else
                    jr.Skip()
                  End If
                ElseIf (jr.TokenType = JsonToken.EndObject) Then
                  perExpressionCallback.Invoke(
                    currentDimensionName, currentDetailExpression,
                   currentDetailNsWhitelist.ToArray(),
                   currentDetailNsBlacklist.ToArray()
                  )
                  currentDetailNsWhitelist.Clear()
                  currentDetailNsBlacklist.Clear()
                  level = JsonReadingLevel.ExpressionsArray
                End If

              Case JsonReadingLevel.NsWhitelistArray
                If (jr.TokenType = JsonToken.String) Then
                  currentDetailNsWhitelist.Add(jr.Value.ToString())
                ElseIf (jr.TokenType = JsonToken.EndArray) Then
                  level = JsonReadingLevel.ExpressionDetailObject
                Else
                  jr.Skip()
                End If

              Case JsonReadingLevel.NsBlacklistArray
                If (jr.TokenType = JsonToken.String) Then
                  currentDetailNsBlacklist.Add(jr.Value.ToString())
                ElseIf (jr.TokenType = JsonToken.EndArray) Then
                  level = JsonReadingLevel.ExpressionDetailObject
                Else
                  jr.Skip()
                End If

            End Select

          End While


        End Using
      End Using

    End Sub

    Private Enum JsonReadingLevel
      Root = 0
      DimensionNames = 1
      ExpressionsArray = 2
      ExpressionDetailObject = 3
      NsWhitelistArray = 4
      NsBlacklistArray = 5
    End Enum

  End Class

End Namespace

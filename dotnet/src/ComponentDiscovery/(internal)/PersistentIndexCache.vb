'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Text

Namespace ComponentDiscovery

  Friend Class PersistentIndexCache

#Region " Singleton & Constructor "

    Private Shared _Instance As PersistentIndexCache = Nothing

    Public Shared Function GetInstance() As PersistentIndexCache

      If (_Instance Is Nothing) Then
        Dim cacheDirectory As String = Nothing

#If NET461 Then
        cacheDirectory = Global.My.Settings.AssemblyIndexCacheDirectory
#Else
        'in .NET Core there are no MySettings!
#End If

        If (String.IsNullOrWhiteSpace(cacheDirectory)) Then
          cacheDirectory = Path.Combine("%PUBLIC%", "AssemblyIndex")
        End If

        _Instance = New PersistentIndexCache(cacheDirectory)

      End If

      Return _Instance
    End Function

    Private _CacheDirectory As String

    Friend Sub New(cacheDirectory As String)
      _CacheDirectory = Environment.ExpandEnvironmentVariables(cacheDirectory)
    End Sub

#End Region

    Private _InvalidCacheFileNames As New List(Of String)

#Region " internal Helpers "

    Friend Function BuildCacheFileFullName(assemblyFullFilename As String) As String
      Dim assemblyLocationDirectory As String = Path.GetDirectoryName(assemblyFullFilename)
      Dim folderNameHash As String = assemblyLocationDirectory.ToLower().MD5()
      Dim fileName As String = Path.GetFileNameWithoutExtension(assemblyFullFilename) + ".cache"

      Dim fullDir = Path.Combine(_CacheDirectory, folderNameHash)
      If (Not Directory.Exists(fullDir)) Then
        Directory.CreateDirectory(fullDir)
        File.WriteAllText(Path.Combine(fullDir, "_Info.txt"), "This Cache is related to assemblies located in: " + assemblyLocationDirectory)
      End If

      Return Path.Combine(_CacheDirectory, folderNameHash, fileName)
    End Function

    Private Sub AnalyzeAssemblyFingerprintData(assemblyFileFullName As String, ByRef fileSize As Long, ByRef modifiedDate As DateTime)
      Dim fi As New FileInfo(assemblyFileFullName)
      fileSize = fi.Length
      modifiedDate = fi.LastWriteTime
    End Sub

    Private Function BuildTimestampBlock(assemblyFileFullName As String) As String
      Dim currentAssemblyFileSize As Long
      Dim currentAssemblyModifiedDate As DateTime

      Me.AnalyzeAssemblyFingerprintData(assemblyFileFullName, currentAssemblyFileSize, currentAssemblyModifiedDate)

      'Note: the second token was the assembly version in past, but isnt used anymore because of performance-issues!
      Return $"{currentAssemblyFileSize}|*.*.*.*|{currentAssemblyModifiedDate}"
    End Function

#End Region

#Region " Read "

    Public Function TryGetClassificationExpressionsFromCache(
      assemblyFullFilename As String,
      dimensionName As String,
      ByRef returningClassificationExpressions As String()
    ) As Boolean

      Dim prefix = "<AssCl>|" + dimensionName + "|"
      Dim matchingLines As String() = Nothing

      If (Me.TryReadCacheFile(assemblyFullFilename, prefix, True, matchingLines)) Then
        Dim classificationExpressions As New List(Of String)

        If (matchingLines.Length = 0) Then
          Return False
        End If

        For Each expr In matchingLines(0).Substring(prefix.Length).Split(","c)
          If (Not String.IsNullOrWhiteSpace(expr)) Then
            classificationExpressions.Add(expr)
          End If
        Next

        returningClassificationExpressions = classificationExpressions.ToArray()
        Return True
      End If

      Return False
    End Function

    Public Function TryGetTypesFromCache(assemblyFullFilename As String, targeTypeFullName As String, ByRef foundTypeFullNames As String()) As Boolean
      Dim prefix = "<Type>|" + targeTypeFullName + "|"

      Dim matchingLines As String() = Nothing
      If (Me.TryReadCacheFile(assemblyFullFilename, prefix, True, matchingLines)) Then
        Dim typeFullNames As New List(Of String)

        If (matchingLines.Length = 0) Then
          Return False
        End If

        For Each expr In matchingLines(0).Substring(prefix.Length).Split(","c)
          If (Not String.IsNullOrWhiteSpace(expr)) Then
            typeFullNames.Add(expr)
          End If
        Next

        foundTypeFullNames = typeFullNames.ToArray()
        Return True
      End If

      Return False
    End Function

    Private Function TryParseFirstLineOfCache(
      firstLineOfCache As String,
      ByRef cachedAssemblyFileSize As Long,
      ByRef cachedAssemblyModifiedDate As DateTime
    ) As Boolean

      If (String.IsNullOrWhiteSpace(firstLineOfCache)) Then
        Return False
      End If

      'StartsWith("<") is an indicator for a malformed file
      If (firstLineOfCache.StartsWith("<")) Then
        Return False
      End If

      Dim fieldsOfFirstLine = firstLineOfCache.Split("|"c)
      If (Not Long.TryParse(fieldsOfFirstLine(0), cachedAssemblyFileSize)) Then
        Return False
      End If

      'NOTE: fields(1) was the assembly version in past,
      'but isnt used anymore because of performance-issues!

      If (fieldsOfFirstLine.Length < 3) Then
        Return False
      End If

      If (Not DateTime.TryParse(fieldsOfFirstLine(2), cachedAssemblyModifiedDate)) Then
        Return False
      End If

      Return True
    End Function

    Private Function TryReadCacheFile(
      assemblyFullFilename As String,
      searchPrefixForLinesToPeek As String,
      singleMatch As Boolean,
      ByRef returningContentLines As String()
    ) As Boolean

      Dim cacheFileFullName As String = BuildCacheFileFullName(assemblyFullFilename)

      If (Not File.Exists(cacheFileFullName)) Then
        Diag.Info($"Assembly-Index-Cachefile '{cacheFileFullName}' does not exsist!")
        Return False
      End If

      Dim cachedAssemblyFileSize As Long = 0
      Dim cachedAssemblyModifiedDate As DateTime = DateTime.MinValue
      Dim classificationExpressionsFromCache As String() = {}

      Dim currentAssemblyFileSize As Long
      Dim currentAssemblyModifiedDate As DateTime
      Me.AnalyzeAssemblyFingerprintData(assemblyFullFilename, currentAssemblyFileSize, currentAssemblyModifiedDate)

      Dim matchingLines As New List(Of String)
      Dim cacheIsValid As Boolean = False

      Try
        Using fs As New FileStream(cacheFileFullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
          Using sr As New StreamReader(fs, Encoding.Default)
            Dim content = sr.ReadLine()

            If (Me.TryParseFirstLineOfCache(content, cachedAssemblyFileSize, cachedAssemblyModifiedDate)) Then

              'HACK: die Equals-Methode liefert hier false - immer ein paar ticks unterschied - explorer bug????
              Dim modificationDateIsMatching As Boolean = (currentAssemblyModifiedDate.ToString() = cachedAssemblyModifiedDate.ToString())
              Dim filesizeIsMatching As Boolean = (currentAssemblyFileSize = cachedAssemblyFileSize)

              If (filesizeIsMatching AndAlso modificationDateIsMatching) Then
                cacheIsValid = True
                SyncLock _InvalidCacheFileNames
                  If (_InvalidCacheFileNames.Contains(cacheFileFullName)) Then
                    _InvalidCacheFileNames.Remove(cacheFileFullName)
                  End If
                End SyncLock

                Do While Not sr.EndOfStream
                  content = sr.ReadLine()

                  If (content.StartsWith(searchPrefixForLinesToPeek)) Then
                    matchingLines.Add(content)
                    If (singleMatch) Then
                      Exit Do
                    End If
                  End If

                Loop

              End If
            End If

          End Using
          fs.Close()
        End Using
      Catch ex As Exception
        Diag.Error(
          $"Cannot read Assembly-Index-Cachefile '{cacheFileFullName}' " +
          $"(a single occurrenceof this exception can be an uncritical filesystem-access-collision...): {ex}"
        )
        cacheIsValid = False
      End Try

      If (cacheIsValid) Then
        returningContentLines = matchingLines.ToArray()
        Return True
      Else
        SyncLock _InvalidCacheFileNames
          If (Not _InvalidCacheFileNames.Contains(cacheFileFullName)) Then
            _InvalidCacheFileNames.Add(cacheFileFullName)
          End If
        End SyncLock
        Return False
      End If
    End Function

#End Region

#Region " Write "

    Public Sub AppendTypesToCache(assemblyFileFullName As String, targeTypeFullName As String, foundTypeFullNames As String())
      Dim prefix = "<Type>|" + targeTypeFullName + "|"
      Dim content As String = prefix + String.Join(","c, foundTypeFullNames)
      Me.AppendToCacheFile(assemblyFileFullName, {content})
    End Sub

    Public Sub AppendClassificationExpressionToCache(assemblyFileFullName As String, dimensionName As String, classificationExpressions As String())
      Dim prefix = "<AssCl>|" + dimensionName + "|"
      Dim content As String = prefix + String.Join(","c, classificationExpressions)
      Me.AppendToCacheFile(assemblyFileFullName, {content})
    End Sub

    Private Sub AppendToCacheFile(assemblyFileFullName As String, contentLinesToAppend As IEnumerable(Of String))
      Dim cacheFileFullName As String = ""
      Try

        cacheFileFullName = Me.BuildCacheFileFullName(assemblyFileFullName)

        Dim fileMode As FileMode = FileMode.Create
        Dim fileShare As FileShare = FileShare.None
        SyncLock _InvalidCacheFileNames
          If (File.Exists(cacheFileFullName) AndAlso Not _InvalidCacheFileNames.Contains(cacheFileFullName)) Then
            fileMode = FileMode.Append
            fileShare = FileShare.ReadWrite
          End If
        End SyncLock

        Using fs As New FileStream(cacheFileFullName, fileMode, FileAccess.Write, fileShare)
          Using sw As New StreamWriter(fs, Encoding.Default)

            If (Not fileMode = FileMode.Append) Then
              'write the one header line containing the timestamp
              sw.WriteLine(Me.BuildTimestampBlock(assemblyFileFullName))
            End If

            For Each contentLineToAppend In contentLinesToAppend
              sw.WriteLine(contentLineToAppend)
            Next

          End Using

          fs.Close()
        End Using

        SyncLock _InvalidCacheFileNames
          If (_InvalidCacheFileNames.Contains(cacheFileFullName)) Then
            _InvalidCacheFileNames.Remove(cacheFileFullName)
          End If
        End SyncLock

      Catch ex As Exception
        'this could be caused by a collision during file access from multiple processes
        'but it is uncritical because the cache can also be rebuilded during the next call
        Diag.Error(
          $"Cannot write Assembly-Index-Cachefile '{cacheFileFullName}' " +
          "(a single occurrenceof this exception can be an uncritical filesystem-access-collision...): " + ex.Message
        )
      End Try

    End Sub

#End Region

  End Class

End Namespace

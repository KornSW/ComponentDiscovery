Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Text

Public Class AssemblyClassificationCache

#Region " Singleton & Constructor "

  Private Shared _Instance As AssemblyClassificationCache = Nothing

  Public Shared Function GetInstance() As AssemblyClassificationCache

    If (_Instance Is Nothing) Then

      Dim cacheDirectory = My.Settings.AssemblyIndexCacheDirectory

      If (String.IsNullOrWhiteSpace(cacheDirectory)) Then
        cacheDirectory = Path.Combine("%PUBLIC%", "AssemblyIndex")
      End If

      _Instance = New AssemblyClassificationCache(cacheDirectory)

    End If

    Return _Instance
  End Function

  Private _CacheDirectory As String

  Private Sub New(cacheDirectory As String)
    _CacheDirectory = Environment.ExpandEnvironmentVariables(cacheDirectory)
  End Sub

#End Region

  Private Function BuildCacheFileFullName(assemblyFullFilename As String, dimensionName As String) As String
    Dim assemblyLocationDirectory As String = Path.GetDirectoryName(assemblyFullFilename)
    Dim folderNameHash As String = assemblyLocationDirectory.ToLower().MD5()
    Dim fileName As String = Path.GetFileNameWithoutExtension(assemblyFullFilename) + "." + dimensionName

    Dim fullDir = Path.Combine(_CacheDirectory, folderNameHash)
    If (Not Directory.Exists(fullDir)) Then
      Directory.CreateDirectory(fullDir)
      File.WriteAllText(Path.Combine(fullDir, "_Info.txt"), "This Cache is related to assemblies located in: " + assemblyLocationDirectory)
    End If
    Return Path.Combine(_CacheDirectory, folderNameHash, fileName)
  End Function

  Private Sub AnalyzeAssemblyFingerprintData(
    assemblyFileFullName As String,
    ByRef fileSize As Long, ByRef fileVersion As Version, ByRef modifiedDate As DateTime
  )
    Dim fi As New FileInfo(assemblyFileFullName)
    fileSize = fi.Length
    modifiedDate = fi.LastWriteTime
    Try
      fileVersion = Version.Parse(FileVersionInfo.GetVersionInfo(assemblyFileFullName).FileVersion)
    Catch
      fileVersion = New Version()
    End Try
  End Sub

  Public Function TryGetClassificationExpressionsFromCache(
    assemblyFullFilename As String, dimensionName As String,
    ByRef returningClassificationExpressions As String()
  ) As Boolean

    Dim cacheFileFullName As String = BuildCacheFileFullName(assemblyFullFilename, dimensionName)

    If (Not File.Exists(cacheFileFullName)) Then
      System.Diagnostics.Trace.WriteLine(String.Format("Assembly-Index-Cachefile '{0}' does not exsist!", cacheFileFullName))
      Return False
    End If

    Dim cachedAssemblyFileSize As Long
    Dim cachedAssemblyFileVersion As Version
    Dim cachedAssemblyModifiedDate As DateTime
    Dim classificationExpressionsFromCache As String() = {}

    Try
      Using fs As New FileStream(cacheFileFullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        Using sr As New StreamReader(fs, Encoding.Default)
          Dim content = sr.ReadLine()
          If (String.IsNullOrWhiteSpace(content)) Then
            Return False
          End If
          Dim fileds = content.Split("|"c)
          cachedAssemblyFileSize = Long.Parse(fileds(0))
          cachedAssemblyFileVersion = Version.Parse(fileds(1))
          cachedAssemblyModifiedDate = DateTime.Parse(fileds(2))
          If (Not String.IsNullOrWhiteSpace(fileds(3))) Then
            classificationExpressionsFromCache = fileds(3).Split(","c)
          End If

        End Using
        fs.Close()
      End Using
    Catch ex As Exception
      System.Diagnostics.Trace.WriteLine(String.Format("Cannot read Assembly-Index-Cachefile '{0}' (a single occurrenceof this exception can be an uncritical filesystem-access-collision...): {1}", cacheFileFullName, ex))
      Return False
    End Try

    Dim currentAssemblyFileSize As Long
    Dim currentAssemblyFileVersion As Version = Nothing
    Dim currentAssemblyModifiedDate As DateTime
    AnalyzeAssemblyFingerprintData(assemblyFullFilename, currentAssemblyFileSize, currentAssemblyFileVersion, currentAssemblyModifiedDate)

    If (Not currentAssemblyFileSize = cachedAssemblyFileSize) Then
      Return False
    End If

    If (Not currentAssemblyFileVersion = cachedAssemblyFileVersion) Then
      Return False
    End If

    'HACK: die Equals-Methode liefert hier false - immer ein paar ticks unterschied - explorer bug????
    If (Not currentAssemblyModifiedDate.ToString() = cachedAssemblyModifiedDate.ToString()) Then
      Return False
    End If

    returningClassificationExpressions = classificationExpressionsFromCache
    Return True
  End Function

  Public Sub WriteScopeValuesToCache(assemblyFileFullName As String, dimensionName As String, classificationExpressions As String())

    Dim cacheFileFullName As String = ""

    Try

      Dim currentAssemblyFileSize As Long
      Dim currentAssemblyFileVersion As Version = Nothing
      Dim currentAssemblyModifiedDate As DateTime
      AnalyzeAssemblyFingerprintData(assemblyFileFullName, currentAssemblyFileSize, currentAssemblyFileVersion, currentAssemblyModifiedDate)

      cacheFileFullName = BuildCacheFileFullName(assemblyFileFullName, dimensionName)

      Using fs As New FileStream(cacheFileFullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)
        Using sw As New StreamWriter(fs, Encoding.Default)

          Dim content As String = String.Format(
          "{0}|{1}|{2}|{3}",
          currentAssemblyFileSize,
          currentAssemblyFileVersion,
          currentAssemblyModifiedDate,
          String.Join(","c, classificationExpressions)
        )

          sw.WriteLine(content)
          sw.Flush()
        End Using
        fs.Close()
      End Using

    Catch ex As Exception
      'this could be caused by a collision during file access from multiple processes
      'but it is uncritical because the cache can also be rebuilded during the next call
      System.Diagnostics.Trace.WriteLine(String.Format("Cannot write Assembly-Index-Cachefile '{0}' (a single occurrenceof this exception can be an uncritical filesystem-access-collision...): {1}", cacheFileFullName, ex))
    End Try

  End Sub

End Class

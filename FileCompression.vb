Imports System.IO
Imports System.IO.Compression

Public Class FileCompression
    Public Shared Sub CompressFile(ByVal sourceFile As String, ByVal destFile As String)

        Dim destStream As New FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.Read)
        Dim srcStream As New FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read)
        Dim gz As New GZipStream(destStream, CompressionMode.Compress)

        Dim bytesRead As Integer
        Dim buffer As Byte() = New Byte(1000000) {}

        bytesRead = srcStream.Read(buffer, 0, buffer.Length)

        While bytesRead <> 0
            gz.Write(buffer, 0, bytesRead)

            bytesRead = srcStream.Read(buffer, 0, buffer.Length)
        End While

        gz.Close()
        destStream.Close()
        srcStream.Close()
    End Sub

    Public Shared Sub DecompressFile(ByVal sourceFile As String, ByVal destFile As String)

        Dim destStream As New FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.Read)
        Dim srcStream As New FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read)
        Dim gz As New GZipStream(srcStream, CompressionMode.Decompress)

        Dim bytesRead As Integer
        Dim buffer As Byte() = New Byte(10000) {}

        bytesRead = gz.Read(buffer, 0, buffer.Length)

        While bytesRead <> 0
            destStream.Write(buffer, 0, bytesRead)

            bytesRead = gz.Read(buffer, 0, buffer.Length)
        End While

        gz.Close()
        destStream.Close()
        srcStream.Close()


    End Sub
End Class
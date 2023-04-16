Public Class IpItem
    Public strText As String
    Public strValue As String
    Public Overrides Function ToString() As String
        Return Me.strText
    End Function

End Class

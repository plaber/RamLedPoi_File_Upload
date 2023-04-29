Imports System.IO
Imports System.Net
Imports System.Text.RegularExpressions
Imports System.Net.Sockets
Imports System.Threading

Public Class Form1

#Region "Base functions"

    Shared client As UdpClient
    Shared receivePoint As IPEndPoint
    Dim readThread As Thread
    Dim fwpath As String
    Dim fwpath1 As String = "\Arduino\esp_led3\esp_led3.ino.generic.bin"
    Dim fwpath2 As String = "\Arduino\esp32_ledhd\esp32_ledhd.ino.esp32.bin"

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Width = GroupBox2.Location.X + GroupBox2.Width + 30
        client = New UdpClient(60201) 'Port
        receivePoint = New IPEndPoint(New IPAddress(0), 0)
        readThread = New Thread(New ThreadStart(AddressOf WaitForPackets))
        readThread.Start()
        If Environment.MachineName = "PKDIMON" Then
            GroupBox1.Visible = True
            Me.Width = GroupBox1.Location.X + GroupBox1.Width + 30
            ButtonDebug.Visible = False
        End If
        Dim fwpathA = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        OpenFileDialog1.InitialDirectory = fwpathA
        If Directory.Exists(fwpathA & "\Arduino") Then
            OpenFileDialog1.InitialDirectory = fwpathA & "\Arduino"
        End If
        Dim strHostName As String = System.Net.Dns.GetHostName()
        Dim strIPAddress As String = System.Net.Dns.GetHostByName(strHostName).AddressList(0).ToString()
        ToolStripStatusLabel1.Text = "Host Name: " & strHostName & "; IP Address: " & strIPAddress
        fwpath = FileIO.SpecialDirectories.MyDocuments & fwpath2
    End Sub

    Private Sub Form1_Closing(sender As Object, e As EventArgs) Handles MyBase.FormClosing
        Application.Exit()
        End
    End Sub

    Private Sub ButtonDebug_Click(sender As Object, e As EventArgs) Handles ButtonDebug.Click
        GroupBox1.Visible = True
        Me.Width = GroupBox1.Location.X + GroupBox1.Width + 30
        ButtonDebug.Visible = False
    End Sub

    Dim lastAns As String = ""
    Public Sub WaitForPackets()
        While True
            Dim data As Byte() = client.Receive(receivePoint)
            Dim ip As Byte() = receivePoint.Address.GetAddressBytes()
            Dim ans As String = System.Text.Encoding.UTF8.GetString(data)
            Dim strip = String.Format("{0}.{1}.{2}.{3:000}: ", ip(0), ip(1), ip(2), ip(3))
            If ans.StartsWith("name=") Then
                strip = receivePoint.Address.ToString() & ": "
            End If
            lastAns = strip & ans
            Console.WriteLine(lastAns)
            TextBoxAnsw.Invoke(New ThreadStart(AddressOf toLog))
        End While
    End Sub

    Sub toLog()
        Dim ip() As String = lastAns.Split(":")
        Dim ipb() As String = ip(1).Split(".")
        Dim address As IPAddress = Nothing
        If IPAddress.TryParse(ip(1).Substring(1), address) And ipb.Length = 4 Then
            Debug.WriteLine("ip detected")
            Dim obj = New IpItem()
            obj.strText = ip(1).Substring(1)
            obj.strValue = ip(1).Substring(1)
            ListBoxIp.Items.Add(obj)
        ElseIf ip(1).Substring(1).StartsWith("name=") Then
            For Each item As IpItem In ListBoxIp.Items
                If item.strText = ip(0) Then
                    item.strText += " " & ip(1).Substring(6)
                End If
            Next
            ListBoxIp.SelectionMode = SelectionMode.None
            ListBoxIp.SelectionMode = SelectionMode.One
        Else
            TextBoxAnsw.AppendText(lastAns & vbCrLf)
        End If
    End Sub

    Sub apLog(text As String)
        If TextBoxAnsw.InvokeRequired Then
            TextBoxAnsw.Invoke(Sub()
                                   TextBoxAnsw.AppendText(text)
                               End Sub)
        Else
            TextBoxAnsw.AppendText(text)
        End If
    End Sub

    Private Sub SendGet(adr As String)
        If CheckBoxClear.Checked Then
            TextBoxAnsw.ResetText()
        End If
        Dim address As IPAddress = Nothing
        If Not IPAddress.TryParse(TextBoxIp.Text, address) Then
            MsgBox("Формат IP адреса некорректный или строка пуста")
            Return
        End If
        If My.Computer.Network.Ping(TextBoxIp.Text) = False Then
            MsgBox("IP адрес не отвечает")
            Return
        End If
        Dim result As String
        Using client As New WebClient
            Try
                result = client.DownloadString("http://" & TextBoxIp.Text & adr)
                If IsNumeric(result) Then
                    TextBoxAnsw.AppendText(Format(CInt(result), "### ### ##0") & vbCrLf)
                Else
                    TextBoxAnsw.AppendText(result & vbCrLf)
                End If

            Catch ex As Exception
                TextBoxAnsw.AppendText(ex.Message & vbCrLf)
                Debug.WriteLine(ex.StackTrace)
            End Try
        End Using
    End Sub

    Private Function SendGet2(ip As String, adr As String)
        Dim address As IPAddress = Nothing
        If Not IPAddress.TryParse(ip, address) Then
            MsgBox("Формат IP адреса некорректный")
            Return ""
        End If
        If My.Computer.Network.Ping(ip) = False Then
            MsgBox("IP адрес не отвечает")
            Return ""
        End If
        Dim result As String
        Using client As New WebClient
            Try
                client.Encoding = System.Text.Encoding.UTF8
                result = client.DownloadString("http://" & ip & adr)
                Return result
            Catch ex As Exception
                TextBoxAnsw.AppendText(ex.Message & vbCrLf)
            End Try
        End Using
        Return ""
    End Function

    Private Sub SendUdp(msg As String, Optional port As Integer = 8888, Optional addr As String = "192.168.0.255")
        If CheckBoxClear.Checked Then
            TextBoxAnsw.ResetText()
        End If
        Dim uc As New UdpClient(addr, port)
        Dim cm As Byte() = System.Text.Encoding.UTF8.GetBytes(msg)
        uc.Send(cm, cm.Length)
    End Sub
#End Region

#Region "Files upload"

    Private Sub scan_dir(path As String)
        If System.IO.Directory.Exists(path) = False Then
            MsgBox("Неправильный путь", MsgBoxStyle.Critical)
            Return
        End If
        Dim strFileSize As String = ""
        Dim di As New IO.DirectoryInfo(path)
        Dim aryFi As IO.FileInfo() = di.GetFiles()
        Dim fi As IO.FileInfo
        ListBox1.Items.Clear()
        Dim fsize As Integer = 0
        Dim fqty As Integer = 0
        For Each fi In aryFi
            If fi.Name.ToLower.EndsWith(".bmp") Or fi.Name.ToLower.EndsWith(".gif") Or fi.Name.ToLower.EndsWith(".jpg") Or fi.Name.ToLower.EndsWith(".txt") Then
                ListBox1.Items.Add(fi.Name)
                fsize += fi.Length
                fqty += 1
            End If
        Next
        LabelFoldSize.Text = (fqty.ToString & " шт " & (Format(fsize, "### ### ##0")) & " байт")
        If aryFi.Length = 0 Then
            MsgBox("В этой папке картинки не найдены")
            TextBoxImgPath.Text = ""
            ListBox1.Items.Clear()
        End If
    End Sub

    Private Sub ButtonSelectFolder_Click(sender As Object, e As EventArgs) Handles ButtonSelectFolder.Click
        If (FolderBrowserDialog1.ShowDialog() = DialogResult.OK) Then
            TextBoxImgPath.Text = FolderBrowserDialog1.SelectedPath
            scan_dir(FolderBrowserDialog1.SelectedPath)
        End If
    End Sub

    Private Sub TextBoxImgPath_KeyUp(sender As Object, e As KeyEventArgs) Handles TextBoxImgPath.KeyUp
        If e.KeyCode = Keys.Enter Then
            scan_dir(TextBoxImgPath.Text)
        End If
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged
        PictureBox1.ImageLocation = TextBoxImgPath.Text & "\" & ListBox1.SelectedItem
    End Sub

    Private Sub ButtonViewImg_Click(sender As Object, e As EventArgs) Handles ButtonViewImg.Click
        System.Diagnostics.Process.Start("http://" & TextBoxIp.Text & "/files")
    End Sub

    Private Sub ButtonFtpOpen_Click(sender As Object, e As EventArgs) Handles ButtonFtpOpen.Click
        System.Diagnostics.Process.Start("explorer.exe", "ftp://" & TextBoxIp.Text & "/")
    End Sub

    Dim WithEvents wclient As New WebClient
    Dim waitcli As Boolean = False

    Private Sub ButtonUploadImg_Click(sender As Object, e As EventArgs) Handles ButtonUploadImg.Click
        If ButtonUploadImg.Text = "Стоп" Then
            BackgroundWorker1.CancelAsync()
            wclient.CancelAsync()
            ProgressBar1.Value = 0
            ButtonUploadImg.Text = "Загрузить"
            waitcli = False
            Return
        End If
        If Directory.Exists(TextBoxImgPath.Text) = False Then
            MsgBox("Выберите папку")
            Return
        End If
        If My.Computer.Network.Ping(TextBoxIp.Text) = False Then
            MsgBox("IP адрес не отвечает")
            Return
        End If
        BackgroundWorker1.RunWorkerAsync()
        ButtonUploadImg.Text = "Стоп"
    End Sub

    Private Sub BackgroundWorker1_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        Dim di As New IO.DirectoryInfo(TextBoxImgPath.Text)
        Dim aryFi As IO.FileInfo() = di.GetFiles()
        Dim fi As IO.FileInfo
        Dim p As Double = 0.0
        For Each fi In aryFi
            If fi.Name.ToLower.EndsWith(".bmp") Or fi.Name.ToLower.EndsWith(".gif") Or fi.Name.ToLower.EndsWith(".jpg") Or fi.Name.ToLower.EndsWith(".txt") Then
                p += 1
                BackgroundWorker1.ReportProgress(p / aryFi.Length * 100, vbCrLf & p.ToString & " " & fi.Name & " ")
                waitcli = True
                wclient.UploadFileAsync(New Uri("http://" & TextBoxIp.Text & "/load"), fi.FullName)
                While waitcli
                    Thread.Sleep(50)
                End While
            Else
                BackgroundWorker1.ReportProgress(p / aryFi.Length * 100, " skipped " & fi.Name)
            End If
            If (BackgroundWorker1.CancellationPending = True) Then
                e.Cancel = True
                Exit For
            End If
            Thread.Sleep(500)
        Next
    End Sub

    Private Sub UploadProgressChanged(sender As Object, e As UploadProgressChangedEventArgs) Handles wclient.UploadProgressChanged
        Console.WriteLine("Upload {0}% complete. ", e.ProgressPercentage)
        apLog(e.ProgressPercentage.ToString & "% ")
    End Sub

    Private Sub UploadFileCompleted(sender As Object, e As UploadFileCompletedEventArgs) Handles wclient.UploadFileCompleted
        Console.WriteLine("Upload complete.")
        waitcli = False
    End Sub

    Private Sub BackgroundWorker1_DoWork2(sender As Object, e As System.ComponentModel.ProgressChangedEventArgs) Handles BackgroundWorker1.ProgressChanged
        ProgressBar1.Value = e.ProgressPercentage
        TextBoxAnsw.AppendText(e.UserState)
    End Sub

    Private Sub BackgroundWorker1_DoWork1(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        If e.Cancelled Then
            TextBoxAnsw.AppendText("прерван" & vbCrLf)
        Else
            TextBoxAnsw.AppendText("готово" & vbCrLf)
        End If

        ButtonUploadImg.Text = "Загрузить"
    End Sub
#End Region

#Region "Search IP"
    Private Sub ButtonSearchIp_Click(sender As Object, e As EventArgs) Handles ButtonSearchIp.Click
        ListBoxIp.Items.Clear()
        SendUdp("ip=1")
    End Sub

    Private Sub ListBoxIp_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBoxIp.SelectedIndexChanged
        Try
            Dim obj As IpItem = ListBoxIp.SelectedItem
            TextBoxIp.Text = obj.strValue
            If TextBoxIp.Text.Length > 0 Then
                'SendGet("/req?free=1")
                SendUdp("free=1", 8888, obj.strValue)
            End If
        Catch ex As Exception
            Debug.WriteLine(ex.StackTrace)
        End Try
    End Sub
#End Region

#Region "Controls"
    Private Sub ButtonGo_Click(sender As Object, e As EventArgs) Handles ButtonGo.Click
        SendUdp("go=1")
    End Sub

    Private Sub ButtonVer_Click(sender As Object, e As EventArgs) Handles ButtonVer.Click
        SendUdp("ver=1")
    End Sub

    Private Sub ButtonBtt_Click(sender As Object, e As EventArgs) Handles ButtonBtt.Click
        SendUdp("vcc=1")
    End Sub

    Private Sub ButtonDelSub_Click(sender As Object, e As EventArgs) Handles ButtonDelSub.Click
        SendUdp("spd=m")
    End Sub

    Private Sub ButtonDelAdd_Click(sender As Object, e As EventArgs) Handles ButtonDelAdd.Click
        SendUdp("spd=p")
    End Sub

    Private Sub ButtonBrgSub_Click(sender As Object, e As EventArgs) Handles ButtonBrgSub.Click
        SendUdp("brg=m")
    End Sub

    Private Sub ButtonBrgAdd_Click(sender As Object, e As EventArgs) Handles ButtonBrgAdd.Click
        SendUdp("brg=p")
    End Sub

    Private Sub ButtonMode1_Click(sender As Object, e As EventArgs) Handles ButtonMode1.Click
        SendUdp("btmode=loop")
    End Sub

    Private Sub ButtonMode2_Click(sender As Object, e As EventArgs) Handles ButtonMode2.Click
        SendUdp("btmode=one")
    End Sub

    Private Sub ButtonSw1_Click(sender As Object, e As EventArgs) Handles ButtonSw1.Click
        NumericUpDownPic.DownButton()
    End Sub

    Private Sub ButtonSw2_Click(sender As Object, e As EventArgs) Handles ButtonSw2.Click
        NumericUpDownPic.UpButton()
    End Sub

    Dim NumericUpDownPic_hold = False

    Private Sub NumericUpDownPic_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDownPic.ValueChanged
        If NumericUpDownPic_hold Then
            SendUdp("modeone=" & NumericUpDownPic.Value)
        End If
        NumericUpDownPic_hold = True
    End Sub

    Private Sub ButtonModeSpiffs_Click(sender As Object, e As EventArgs) Handles ButtonModeSpiffs.Click
        SendUdp("mode=3")
    End Sub

    Private Sub ButtonModeProg_Click(sender As Object, e As EventArgs) Handles ButtonModeProg.Click
        SendUdp("mode=4")
    End Sub

    Private Sub ButtonProgSub_Click(sender As Object, e As EventArgs) Handles ButtonProgSub.Click
        SendUdp("prg=m")
    End Sub

    Private Sub ButtonProgAdd_Click(sender As Object, e As EventArgs) Handles ButtonProgAdd.Click
        SendUdp("prg=p")
    End Sub

    Dim NumericUpDownProg_hold = False

    Private Sub NumericUpDownProg_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDownProg.ValueChanged
        If NumericUpDownProg_hold Then
            SendUdp("prg=" & NumericUpDownProg.Value)
        End If
        NumericUpDownProg_hold = True
    End Sub

    Private Sub ButtonConfigSave_Click(sender As Object, e As EventArgs) Handles ButtonConfigSave.Click
        SendUdp("cmt=1")
    End Sub

    Private Sub ButtonConfigReset_Click(sender As Object, e As EventArgs) Handles ButtonConfigReset.Click
        Dim answer = MessageBox.Show("Вы уверены что хотите сбросить настройки?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If answer = vbYes Then
            SendUdp("rst=1")
        End If
    End Sub

    Private Sub ButtonFormat_Click(sender As Object, e As EventArgs) Handles ButtonFormat.Click
        Dim answer = MessageBox.Show("Вы уверены что хотите стереть все файлы?" & vbCrLf & "(процесс займет примерно 30 секунд)", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If answer = vbYes Then
            SendGet("/req?format=1")
        End If
    End Sub
#End Region

#Region "Debug"
    Private Sub ButtonDebugVibrate_Click(sender As Object, e As EventArgs) Handles ButtonDebugVibrate.Click
        SendUdp("vibrate", 60202, TextBoxVibr.Text)
    End Sub

    Private Sub NumericUpDownDelay_KeyPress(sender As Object, e As KeyEventArgs) Handles NumericUpDownDelay.KeyUp
        If e.KeyCode = 13 Then
            ButtonDebugDelay.PerformClick()
        End If
    End Sub

    Private Sub ButtonDebugDelay_Click(sender As Object, e As MouseEventArgs) Handles ButtonDebugDelay.Click
        If (e.Button = MouseButtons.Left And (ModifierKeys & Keys.Control) = Keys.Control) Then
            SendGet("/config?uptime=1")
        Else
            SendGet("/reqf?fwait=" & NumericUpDownDelay.Value.ToString)
        End If
    End Sub

    Private Sub ButtonReboot_Click(sender As Object, e As EventArgs) Handles ButtonReboot.Click
        SendUdp("restart=1")
    End Sub

    Private Sub ButtonBrg4_Click(sender As Object, e As EventArgs) Handles ButtonBrg4.Click
        SendUdp("brg=" & NumericUpDownBrgn.Value)
    End Sub

    Private Sub ButtonDrip_Click(sender As Object, e As EventArgs) Handles ButtonDrip.Click
        SendUdp("drip=1")
    End Sub

    Private Sub ButtonBlnk_Click(sender As Object, e As EventArgs) Handles ButtonBlnk.Click
        SendUdp("blink=1")
    End Sub

    Private Sub ButtonGetPics_Click(sender As Object, e As EventArgs) Handles ButtonGetPics.Click
        If Not IPAddress.TryParse(TextBoxIp.Text, Nothing) Then
            Return
        End If
        Dim picsFold As String = "pics" & TextBoxIp.Text.Split(".")(3)
        Dim picsPage As String = "/pics"
        If Control.ModifierKeys And Keys.LControlKey = Keys.LControlKey Then
            Dim pt = InputBox("enter path to save", "Custom path", picsFold)
            If pt.Length > 0 Then
                picsFold = pt
                picsPage = "/pics?all=1"
            End If
        End If
        Dim ans As String = SendGet2(TextBoxIp.Text, picsPage)
        If ans.Length > 0 Then
            Dim Splitted() As String = ans.Split(New String() {vbLf}, StringSplitOptions.None)
            TextBoxAnsw.AppendText("founded pics " & Splitted.Length & vbCrLf)
            Dim p As Integer = 0
            Dim YourPath As String = Application.StartupPath() & "\" & picsFold
            If (Not System.IO.Directory.Exists(YourPath)) Then
                System.IO.Directory.CreateDirectory(YourPath)
            End If
            Using client As New WebClient()
                For Each f In Splitted
                    Dim remoteUri As String = "http://" & TextBoxIp.Text & If(f.StartsWith("/"), "", "/") & f
                    TextBoxAnsw.AppendText(remoteUri & vbCrLf)
                    Dim fileName As String = YourPath & If(f.StartsWith("/"), "", "/") & f
                    client.Credentials = New NetworkCredential()
                    client.DownloadFile(remoteUri, fileName)
                    p += 1
                    ProgressBar1.Value = (p / Splitted.Length) * 100
                Next
            End Using
        End If
    End Sub

    Dim countDown As Integer

    Private Sub ButtonRun_Click(sender As Object, e As EventArgs) Handles ButtonRun.Click
        countDown = NumericUpDownTimeout.Value
        Timer1.Enabled = True
        'SendUdp("god=" & NumericUpDownTimeout.Value)
    End Sub

    Private Sub ButtonStop_Click(sender As Object, e As EventArgs) Handles ButtonStop.Click
        SendUdp("stp=1")
        Timer1.Enabled = False
    End Sub
    Private Sub ButtonStop2_Click(sender As Object, e As EventArgs) Handles ButtonStop2.Click
        SendUdp("stp=1")
        Timer1.Enabled = False
    End Sub

    Dim audName As String = Application.StartupPath() & "\around.mp3"

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        TextBoxAnsw.AppendText(countDown & vbCrLf)
        If countDown = 0 Then
            SendUdp("go=1")
            Timer1.Enabled = False
            WebBrowser1.Url = New Uri(audName)
        End If
        countDown -= 1
    End Sub

    Private Sub ButtonBpm_Click(sender As Object, e As EventArgs) Handles ButtonBpm.Click
        SendUdp("bpm=" & NumericUpDownBpm.Value)
    End Sub

    Private Sub ButtonSelAud_Click(sender As Object, e As EventArgs) Handles ButtonSelAud.Click
        OpenFileDialog1.Filter = "audio|*.mp3"
        OpenFileDialog1.InitialDirectory = Application.StartupPath()
        If OpenFileDialog1.ShowDialog() = Windows.Forms.DialogResult.OK Then
            audName = OpenFileDialog1.FileName
        End If
    End Sub

    Private Sub ButtonEnow_Click(sender As Object, e As EventArgs) Handles ButtonEnow.Click
        SendUdp("enow=2")
    End Sub

    Private Sub ButtonTestUDP_Click(sender As Object, e As EventArgs) Handles ButtonTestUDP.Click
        SendUdp("hello", 60201)
    End Sub

    Private Sub ButtonMacpair_Click(sender As Object, e As EventArgs) Handles ButtonMacpair.Click
        SendUdp("macun=1")
    End Sub
    Private Sub ButtonMacOrder_Click(sender As Object, e As EventArgs) Handles ButtonMacOrder.Click
        SendUdp("macor=1")
    End Sub

    Private Sub ButtonMacAp_Click(sender As Object, e As EventArgs) Handles ButtonMacAp.Click
        SendUdp("maca=1")
    End Sub

    Private Sub ButtonMacDel_Click(sender As Object, e As EventArgs) Handles ButtonMacDel.Click
        SendUdp("macun=unsn")
    End Sub

    Private Sub ButtonDelConf_Click(sender As Object, e As EventArgs) Handles ButtonDelConf.Click
        Dim answer = MessageBox.Show("Вы уверены что хотите стереть все файлы конфигурации?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If answer = vbYes Then
            SendUdp("delconf=1")
        End If
    End Sub
#End Region

    Private Sub ButtonGetConf_Click(sender As Object, e As EventArgs) Handles ButtonGetConf.Click
        SendUdp("conf=1")
    End Sub

    Private Sub CheckBoxClear_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxClear.CheckedChanged
        If CheckBoxClear.Checked = True Then
            TextBoxAnsw.Clear()
        End If
    End Sub

    Private Sub ButtonSelFw_Click(sender As Object, e As EventArgs) Handles ButtonSelFw.Click
        If Control.ModifierKeys = Keys.Control Then
            Debug.WriteLine("Ctrl+Click")
            fwpath = FileIO.SpecialDirectories.MyDocuments & fwpath1
            TextBoxAnsw.AppendText("выбран " & fwpath & vbCrLf)
            Return
        End If
        If Control.ModifierKeys = Keys.Shift Then
            Debug.WriteLine("Shift+Click")
            fwpath = FileIO.SpecialDirectories.MyDocuments & fwpath2
            TextBoxAnsw.AppendText("выбран " & fwpath & vbCrLf)
            Return
        End If
        OpenFileDialog1.Filter = "binary|*.bin|txt files|*.txt"
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        If OpenFileDialog1.ShowDialog() = Windows.Forms.DialogResult.OK Then
            fwpath = OpenFileDialog1.FileName
            TextBoxAnsw.AppendText("выбран " & fwpath & vbCrLf)
            'FileCompression.CompressFile(fwpath, fwpath & ".gz")
        End If
    End Sub

    Private Sub sendfw(ByVal ip As String)
        Try
            Dim fi As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(fwpath)
            Dim client As New WebClient
            AddHandler client.UploadProgressChanged, AddressOf webClient_UploadFileProgressChanged
            AddHandler client.UploadFileCompleted, AddressOf webClient_UploadFileCompleted
            client.UploadFileAsync(New Uri("http://" & ip & "/update"), fi.FullName)
        Catch ex As System.Net.Sockets.SocketException
            Debug.WriteLine(ex.Message)
            TextBoxAnsw.AppendText(ip & " not response" & vbCrLf)
        End Try
    End Sub

    Private Sub ButtonSendFw_Click(sender As Object, e As EventArgs) Handles ButtonSendFw.Click
        If File.Exists(fwpath) = False Then
            MsgBox("Укажите файл")
            Debug.WriteLine("no file " & fwpath)
            Return
        End If
        If My.Computer.Keyboard.CtrlKeyDown Then
            Dim ips = ListBoxIp.Items.Count, ipn = 0
            Dim clients(ips) As WebClient
            For Each item As IpItem In ListBoxIp.Items
                sendfw(item.strValue)
            Next
        Else
            If TextBoxIp.Text.Length = 0 Or My.Computer.Network.Ping(TextBoxIp.Text) = False Then
                MsgBox("IP адрес не отвечает")
                Return
            End If
            sendfw(TextBoxIp.Text)
        End If
    End Sub
    Private Sub webClient_UploadFileCompleted(ByVal sender As Object, ByVal e As UploadFileCompletedEventArgs)
        TextBoxAnsw.AppendText("готово " & vbCrLf)
        Dim V As New System.Text.UTF8Encoding()
        'MsgBox(V.GetString(e.Result))
        apLog(V.GetString(e.Result) & vbCrLf)
    End Sub
    Private Sub webClient_UploadFileProgressChanged(ByVal sender As Object, ByVal e As UploadProgressChangedEventArgs)
        ProgressBar1.Value = e.ProgressPercentage
        apLog(e.BytesSent & "/" & e.TotalBytesToSend & vbCrLf)
    End Sub

    Private Sub ButtonName_Click(sender As Object, e As EventArgs) Handles ButtonName.Click
        SendUdp("wpref=0")
    End Sub

    Private Sub ButtonInfo_Click(sender As Object, e As EventArgs) Handles ButtonInfo.Click
        SendUdp("info=0")
    End Sub

End Class
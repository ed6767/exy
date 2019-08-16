Public Class exyHandler_core
    Shared Function Functions() As Dictionary(Of String, Byte)
        ' This returns all the functions to exyconst
        Console.WriteLine("exyHandler Core commands requested")
        Return New Dictionary(Of String, Byte) From {
             {"msg", 1}, ' System message box
             {"log", 1}, ' console output
             {"check", 1} ' check if statement true/false
        }
    End Function
    Shared Sub Init()
        ' new is excecuted on init of code
        Console.WriteLine("exyHandler Core loaded")
    End Sub

    Shared Sub FunctionCall(ByVal FuncName As String, ByVal params As List(Of String))
        If FuncName = "msg" Then
            MsgBox(params(1))
        ElseIf FuncName = "log" Then
            Console.WriteLine(params(1))
        End If
    End Sub

End Class

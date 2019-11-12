Public Class exyHandler_logic
    Shared exyInst As Object
    Shared Function Functions() As Dictionary(Of String, Byte)
        ' This returns all the functions to exyconst
        exyInst.loggingMessage("exyHandler Logic commands requested")
        Return New Dictionary(Of String, Byte) From {
             {"msg", 1}, ' System message box
             {"log", 1}, ' console output
             {"check", 1}, ' check if statement true/false
             {"chk", 3}, ' Expanded and recommended alternative to check - this is because check also handles varible inputs as a comparitor which is not good
             {"not", 0}, ' Reverse the temp bool, i.e true = false, false = true     
             {"store", 1}, {"->", 1}, {"||", 1}, ' Save temp into a var
             {"using", 1}, {">||<", 1}, {"<-", 1} ' Set temp
        }
    End Function
    Shared Sub Init(ByVal exyInstIn As Object)
        ' excecuted on init of code
        exyInst = exyInstIn
        exyInst.loggingMessage("Logic loaded")
    End Sub

    Shared Sub FunctionCall(ByVal FuncName As String, ByVal params As List(Of String))
        If FuncName = "msg" Then
            'Msgbox
            MsgBox(params(1))
        ElseIf FuncName = "log" Then
            'Console log
            Console.WriteLine(params(1))
        ElseIf FuncName = "check" Then
            ' Check things then return the bool. REMEMBER: >= <= MUST come before < >
            ' !!! THIS COMMAND HAS A DESIGN ISSUE !!!, it should not be used by script writer to handle user data as varibles will also count as a comparitor here. Alternative command: chk
            Dim toCheck = params(1)
            If toCheck.Contains(">=") Then
                Dim chk = toCheck.Split(">=")
                exyInst.runtimeTemp = CInt(chk(0)) >= CInt(chk(1).Replace("=", ""))
            ElseIf toCheck.Contains("<=") Then
                Dim chk = toCheck.Split("<=")
                exyInst.runtimeTemp = CInt(chk(0)) <= CInt(chk(1).Replace("=", ""))
            ElseIf toCheck.Contains("<") Then
                Dim chk = toCheck.Split("<")
                exyInst.runtimeTemp = CInt(chk(0)) < CInt(chk(1).Replace("=", ""))
            ElseIf toCheck.Contains(">") Then
                Dim chk = toCheck.Split(">")
                exyInst.runtimeTemp = CInt(chk(0)) > CInt(chk(1).Replace("=", ""))
            ElseIf toCheck.Contains("!=") Then
                Dim chk = toCheck.Split("!=")
                exyInst.runtimeTemp = chk(0) IsNot chk(1).Replace("=", "")
            ElseIf toCheck.Contains("==") Then
                ' Used to compare two integers, ONLY ON CHECK function, not chk!!
                Dim chk = toCheck.Split("==")
                exyInst.runtimeTemp = CInt(chk(0)) = CInt(chk(1).Replace("=", ""))
            ElseIf toCheck.Contains("=") Then
                ' Should only be used to compare strings
                Dim chk = toCheck.Split("=")
                exyInst.runtimeTemp = chk(0) = chk(1)
            Else
                Throw New Exception("check: Unknown comparision: " & params(1))
            End If

        ElseIf FuncName = "chk" Then
            ' Basically the same as check but with 3 params (a, comparitor, b) instead of 1 to ease varible handling issues
            Dim oprt = params(2)
            Dim chk As List(Of String) = New List(Of String) From {params(1), params(3)}
            If oprt = (">=") Then
                exyInst.runtimeTemp = CInt(chk(0)) >= CInt(chk(1))
            ElseIf oprt = ("<=") Then
                exyInst.runtimeTemp = CInt(chk(0)) <= CInt(chk(1))
            ElseIf oprt = ("<") Then
                exyInst.runtimeTemp = CInt(chk(0)) < CInt(chk(1))
            ElseIf oprt = (">") Then
                exyInst.runtimeTemp = CInt(chk(0)) > CInt(chk(1))
            ElseIf oprt = ("!=") Then
                exyInst.runtimeTemp = chk(0) IsNot chk(1)
            ElseIf oprt = ("=") Then
                exyInst.runtimeTemp = chk(0) = chk(1)
            Else
                Throw New Exception("chk: Unknown comparision: " & params(2))
            End If
        ElseIf FuncName = "not" Then
            Try
                exyInst.runtimeTemp = IIf(exyInst.runtimeTemp, False, True)
            Catch ex As Exception
                Throw New Exception("not: Cannot invert")
            End Try
        ElseIf FuncName = "store" OrElse FuncName = "->" OrElse FuncName = "||" Then
            'Store temp
            exyInst.varMod(params(1), exyInst.runtimeTemp)
        ElseIf FuncName = "using" OrElse FuncName = "<-" OrElse FuncName = ">||<" Then
            ' Set temp
            exyInst.runtimeTemp = exyInst.varibles(params(1))
        End If
    End Sub

End Class

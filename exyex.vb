Public Class exyex
    ' EXY (c) 2019 Ed.E
    Public Shared exyVersion As Short = 1

    ' Instruct, dictionary of instructions to run
    ' Instructionindex = List of string(0)Instruction, (1)(2)(3...) arguments, varibles can be replaced on the fly in mode
    Public Shared instruct As Dictionary(Of Integer, List(Of String)) = New Dictionary(Of Integer, List(Of String))
    Shared instructIndex As Integer = 0
    ' Goto label, closest instruction index
    Public Shared gotoLocations As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)
    Public Shared runtimeTemp As Object = "Not defined"
    Public Shared runningCode As Boolean = False
    Public Shared runningErrorHandling As Dictionary(Of String, Object) = New Dictionary(Of String, Object) From
     {
        {"enabled", False},
        {"evenFatal", False}, ' Hand fatal errors to the script
        {"gotoMarker", ""},
        {"warnings", True}
     }

    Public Shared varibles As Dictionary(Of String, Object) = New Dictionary(Of String, Object) From
        {
        {"_VERSION", exyVersion}
        }

    'Load code handlers
    Shared Sub loadHandlers()
        loggingMessage("Load handlers") 'DEBUG
        exyconst.handlerList.Clear()
        exyconst.handlerClasses.Clear()

        'Recreate primary functions that EXY wouldn't function without
        exyconst.functionList = New Dictionary(Of String, Byte) From
    {
    {"", 0}, {" ", 0}, 'nothing
    {"on error", 1}, {"die", 1}, {"warnings", 1}, 'set error action
    {"if", 2}, 'If temp bool is true goto arg 1 else goto arg 2 (arg 2 can be continue which does nothing)
    {"goto", 1}, 'jump to marker
    {"var", 1}, ' define var
    {"set", 2} ' Set var
    }
        loggingMessage("values reset, finding classes") 'DEBUG
        ' Find all classes with names that contain "exyHandler"
        Dim handlerClasses = AppDomain.CurrentDomain.GetAssemblies().SelectMany(Function(t) t.GetTypes()).Where(Function(t) t.IsClass AndAlso t.Name.Contains("exyHandler"))
        'If none found throw an error
        If handlerClasses.Count < 1 Then errorHandler(New Exception("No handlers found. Please check your config files."), "Error loading handlers", "EXYEX", True)

        ' Now we need to get each class and ask it for the functions it'd like to add
        For Each handler In handlerClasses
            loggingMessage("Found " & handler.FullName) 'DEBUG
            exyconst.handlerClasses.Add(Activator.CreateInstance(handler)) ' Create an instance of it
            Dim hndI = exyconst.handlerClasses.Count - 1 ' Get index of the handler object, as it as just been added, it is last
            loggingMessage("requesting functions")
            Dim funcs As Dictionary(Of String, Byte) = exyconst.handlerClasses(hndI).Functions() ' Request functions
            For i = 0 To funcs.Count - 1
                loggingMessage("adding function" & funcs.ElementAt(i).Value) 'DEBUG
                exyconst.functionList.Add(funcs.ElementAt(i).Key, funcs.ElementAt(i).Value) ' Add to function list
                exyconst.handlerList.Add(funcs.ElementAt(i).Key, hndI) ' Add to handler list
            Next

        Next
        loggingMessage("done load ok :)")
    End Sub

    ' Turn file/string into intructions
    Shared Sub parseStringToInstructions(ByVal inp As String, Optional ByVal reloadHandlers As Boolean = False)
        Try
            If reloadHandlers Then loadHandlers() ' Load functions
            instruct.Clear() ' Clear past instructions
            gotoLocations.Clear()
            Dim inst = inp.Split(New String() {Environment.NewLine}, StringSplitOptions.None) ' Split at newline
            Dim i = 0
            instructIndex = 0
            While i <= inst.Length - 1
                Dim curr = inst(i) 'load the current instruction
                Dim paramCount As Byte
                Try
                    If curr.EndsWith(";") Then ' Goto syntax, mark it
                        Try
                            loggingMessage("Goto noticed") ' DEBUG
                            Dim marker = curr.Split(";")(0)
                            i += 1 ' Move to next instruction
                            ' Check for an unexpected end
                            If i > inst.Length - 1 Then errorHandler(New Exception("Unexpected end. You cannot define a marker as the last operation of an instruction set."), "Error defining marker '" & curr & "'", "line " & i + 1, True)
                            curr = inst(i) ' DO not need to change instruct index as the next instruction will be applied that index
                            ' Add to locations
                            If gotoLocations.ContainsKey(marker) Then
                                loggingMessage("contains key already, warning") 'DEBUG
                                ' This already exists, so ignore it, and give warning
                                warningHandler(New Exception("The marker '" & marker & "' was already defined at instruction " & gotoLocations(marker) & ". The new marker at instruction position " & instructIndex & " will be ignorned."), "Marker Not Overwriten", "line " & i, False)
                                Exit Try
                            End If
                            gotoLocations.Add(marker, instructIndex)
                            loggingMessage("Goto noted in gotoLocations") 'DEBUG

                        Catch ex As Exception
                            errorHandler(ex, "Error defining marker '" & curr & "'", "line " & i + 1, False)
                        End Try
                    End If

                    ' We can't handle two gotos in a row, lets test for that
                    If curr.EndsWith(";") Then errorHandler(New Exception("Cannot create marker directly after previous marker '" & inst(i - 1) & "'"), "Error defining marker '" & curr & "'", "line " & i + 1, False)

                    ' Match with list and add the 
                    loggingMessage("verifying function exists") 'DEBUG
                    paramCount = exyconst.functionList(curr)
                Catch ex As Exception
                    loggingMessage("error! probably unknown. " & ex.ToString) 'DEBUG
                    errorHandler(New Exception("Unknown function '" & curr & "'"), "Parse error", "line " & i + 1, False)
                End Try

                Dim params As List(Of String) = New List(Of String)
                params.Add(curr) ' Add instruction so it is index 0
                If paramCount > 0 Then
                    For i2 = 1 To paramCount
                        ' Get the params and add to param list
                        params.Add(inst(i + i2))
                        loggingMessage("added param") 'DEBUG
                    Next
                Else
                    params.Add("None Given") ' All commands will recieve one param, prevents null errors, also tells dev of bug
                End If
                'Push to instruct (used at runtime)
                instruct.Add(instructIndex, params)
                'Increment by 1 (+ paramCount, if more than 0)
                i += 1 + IIf(paramCount > 0, paramCount, 0)
                'Increment Instruct index

                instructIndex += 1
            End While

            'Reset
            instructIndex = 0
        Catch ex As Exception
            errorHandler(ex, "Unknown Syntax / Parse error", "Unknown Location", False)
        End Try
    End Sub

    ' Excecute code
    Shared Sub beginExcecution()
        runningCode = True
        loggingMessage("excecution starting") ' DEBUG
        'todo check if instructions empty
        instructIndex = 0
        While instructIndex <= instruct.Count - 1
            Dim params = instruct(instructIndex) ' params remember START at 1
            Dim func = params(0) ' inst
            loggingMessage(func & " index " & instructIndex)
            ' Now we replace with varibles if params ask for them (surrounded with [[varibleName]] - It is important that with var/set you do not use this)
            ' pottentially v bad and slow code, may want to change if possible
            For i = 0 To params.Count - 2
                ' For each real param replace
                For Each va In varibles
                    loggingMessage("current var dump: " & "[[" & va.Key & "]]")
                    If params(i + 1).Contains("[[" & va.Key & "]]") Then
                        params(i + 1) = params(i + 1).Replace("[[" & va.Key & "]]", va.Value)
                    End If

                Next
            Next
            If func = "" Then
                ' Do nothing
                'elifs here for built in fucntions
            ElseIf func = "goto" Then ' !! ALWAYS FIRST TO HAVE FAST GOTO !!
                ' syntax goto + marker
                doGoto(params(1))
            ElseIf func = "if" Then : doGoto(params(IIf(runtimeTemp = True, 1, 2))) ' syntax if + true marker + false marker
            ElseIf func = "on error" OrElse func = "die" Then ' error handling in scripts
                'syntax die + marker, also define varible with error message
                If params(1) = "Default" Then : runningErrorHandling("enabled") = False ' If Default disable script handling
                ElseIf params(1) = "Fatal On" OrElse params(1) = "Fatal Off" Then ' Modify fatal error handling
                    warningHandler(New Exception("You have specified that you want your script to handle internal/fatal errors during excecution. EXY is not designed to run in this way, expect unexpected behaviour." & vbNewLine & vbNewLine _
                        & "To disable these messages, use:" & vbNewLine & "warnings" & vbNewLine & "off"), "die/on error warning", "Instruction " & instructIndex, False)
                    runningErrorHandling("evenFatal") = IIf(params(1) = "Fatal On", True, False)  ' Let scripts handle fatal errors by using this flag
                    'Testing (see more, scroll right ->)
                ElseIf params(1) = "TEST Normal" OrElse params(1) = "TEST Fatal" Then : errorHandler(New Exception("Test Exception"), "test", "test", IIf(params(1) = "TEST Fatal", True, False)) ' Testing error throwing, DO NOT REMOVE, useful for script writer
                Else
                    ' Set error handling flags
                    runningErrorHandling("enabled") = True
                    runningErrorHandling("gotoMarker") = params(1)
                End If
            ElseIf func = "warnings" Then : runningErrorHandling("warnings") = IIf(params(1) = "off", False, True) ' syntax warnings + on/off, disable warnings
            ElseIf func = "var" OrElse func = "set" Then
                ' Create/modify varible - syntax var + var name
                If func = "var" Then : varMod(params(1), "Not set") : Else ' Set varible, else. ALSO WORKS TO DEFINE!
                    ' Modify varible
                    varMod(params(1), params(2))
                End If
            ElseIf exyconst.handlerList.ContainsKey(func) Then ' !! ALWAYS LAST !!
                    'Another class can do this, pass it to them
                    Try
                        exyconst.handlerClasses(exyconst.handlerList(func)).FunctionCall(func, params)
                    Catch ex As Exception
                        If ex.GetType() = GetType(MissingMemberException) Then
                            ' module maker messed up
                            errorHandler(ex, "Runtime error", "Handler " & exyconst.handlerList(func) & vbCrLf & "!! Please contact the module developer regarding module errors !!", True)
                        End If
                        Throw ex
                    End Try

                Else
                    'Not found
                    errorHandler(New Exception("No handler for function '" & func & "'"), "Runtime error", "instruction position " & instructIndex, False)
            End If
            instructIndex += 1

        End While
        runningCode = False
    End Sub

    Shared Sub doGoto(ByVal marker As String, Optional ByVal increment As Boolean = False)
        ' Perform a goto
        If marker = "Continue" Then Exit Sub ' Continue, don't do goto
        If Not gotoLocations.ContainsKey(marker) Then
            errorHandler(New Exception("Marker '" & marker & "' not defined"), "Runtime goto error", "instruction position " & instructIndex, False)
            Exit Sub
        End If
        instructIndex = gotoLocations(marker) - IIf(increment, 0, 1) ' do goto, but take one as instructIndex will be incremented later
        loggingMessage("goto " & instructIndex + IIf(increment, 0, 1))
    End Sub

    ' For making and changing varibles
    Shared Sub varMod(ByVal Nm As String, ByVal val As Object)
        If varibles.ContainsKey(Nm) Then
            loggingMessage("var key " & Nm & " already exists, overwrite")
            varibles(Nm) = val
        Else
            varibles.Add(Nm, val)
        End If
    End Sub


    'Handle errors (non syntax)
    Shared Sub errorHandler(ByVal err As Exception, ByVal ref As String, ByVal codeLine As Object, ByVal fatal As Boolean)
        ' Will be done when running code
        loggingMessage("ERROR HANDLER - err fatal: " & fatal.ToString() & vbNewLine &
                       "script handling: " & runningErrorHandling("enabled").ToString)
        If runningCode AndAlso IIf(runningErrorHandling("evenFatal"), True, Not fatal) AndAlso runningErrorHandling("enabled") Then
            loggingMessage("Script will handle error " & err.ToString())
            varMod("_ERROR", err.ToString)
            varMod("_ERRORLOC", ref & " - " & codeLine)
            doGoto(runningErrorHandling("gotoMarker"))
            Exit Sub
        End If
        ' Fatal error
        Console.BackgroundColor = ConsoleColor.Red
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("ERROR")
        Console.WriteLine(ref & " - " & codeLine)
        Console.WriteLine(err.Message)
        Console.ReadLine()
        If My.Computer.Keyboard.ShiftKeyDown Then Throw err
        End
    End Sub

    Shared Sub warningHandler(ByVal err As Exception, ByVal ref As String, ByVal codeLine As Object, ByVal dumpVars As Boolean)
        If runningCode AndAlso Not runningErrorHandling("warnings") Then Exit Sub ' Warning disables
        Console.BackgroundColor = ConsoleColor.DarkYellow
        Console.ForegroundColor = ConsoleColor.Black
        Console.WriteLine("WARNING")
        Console.WriteLine(ref & " - " & codeLine)
        Console.WriteLine(err.Message)
        Console.ResetColor()
    End Sub

    Shared Sub loggingMessage(ByVal mes As String)
        Console.BackgroundColor = ConsoleColor.Blue
        Console.ForegroundColor = ConsoleColor.Black
        Console.WriteLine("LOG: " & mes)
        Console.ResetColor()
    End Sub
End Class

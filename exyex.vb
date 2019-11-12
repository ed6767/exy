Public Class exyex
    ' EXY (c) 2019 Ed.E
    ' WARNING! DO NOT DIRECTLY REFERENCE THIS CLASS. ALWAYS USE exyconst.currentEXYex INSTEAD
    Public Shared exyVersion As Short = 1

    ' Instruct, dictionary of instructions to run
    ' Instructionindex = List of string(0)Instruction, (1)(2)(3...) arguments, varibles can be replaced on the fly in mode
    Public Shared instruct As Dictionary(Of Integer, List(Of String)) = New Dictionary(Of Integer, List(Of String))
    Shared instructIndex As Integer = 0
    ' Goto label, closest instruction index
    Public Shared gotoLocations As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)
    Public Shared runtimeTemp As Object = "Not defined"
    Public Shared runningCode As Boolean = False
    Public Shared runningErrorHandling As Dictionary(Of String, Object)
    Public Shared loadedModules As List(Of String) = New List(Of String)
    Public Shared varibles As Dictionary(Of String, Object)
    Public Shared dllModulePaths As List(Of String) = New List(Of String)
    Public Shared errorsInARow As Byte = 0
    Public Shared wasGoodFunc As Boolean = True
    Public Shared debugMode As Boolean = False
    Public Shared loggingOn As Boolean = False
    Public Shared operationsCompleted As Integer = 0

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
    {"require", 1}, ' Check module is loaded 
    {"if", 2}, 'If temp bool is true goto arg 1 else goto arg 2 (arg 2 can be continue which does nothing)
    {"goto", 1}, 'jump to marker
    {"var", 1}, ' define var
    {"set", 2} ' Set var
    }
        loggingMessage("values reset, finding classes") 'DEBUG
        loadedModules.Clear()
        ' Find all classes with names that contain "exyHandler" within the compiled excecutable
        loadTypestoModules(AppDomain.CurrentDomain.GetAssemblies().SelectMany(Function(t) t.GetTypes()).Where(Function(t) t.IsClass AndAlso t.Name.Contains("exyHandler")))
        'Now we load the dlls and perform those checks in there
        For Each dllPath In dllModulePaths
            If IO.File.Exists(dllPath) Then
                ' We do the same thing to the dll, load then get exyHandler things
                loadTypestoModules(From t In Reflection.Assembly.LoadFrom(dllPath).GetTypes()
                                   Where (t.IsClass AndAlso t.Name.Contains("exyHandler")))
            End If
        Next
        ' Now we need to get each class and ask it for the functions it'd like to add

        loggingMessage("done load ok :)")
    End Sub

    'Load types into actual modules
    Public Shared Sub loadTypestoModules(ByVal handlerClasses As IEnumerable(Of Type))
        If handlerClasses Is Nothing Then
            Throw New ArgumentNullException(NameOf(handlerClasses))
        End If

        For Each handler In handlerClasses
            Dim modName = handler.FullName.Replace("exyHandler_", "")
            loggingMessage("Found " & modName) 'DEBUG

            exyconst.handlerClasses.Add(Activator.CreateInstance(handler)) ' Create an instance of it
            Dim hndI = exyconst.handlerClasses.Count - 1 ' Get index of the handler object, as it as just been added, it is last
            loggingMessage("requesting functions")
            exyconst.handlerClasses(hndI).Init(exyconst.currentEXYex) ' Initialise so the modules can find us if needed
            Dim funcs As Dictionary(Of String, Byte) = exyconst.handlerClasses(hndI).Functions() ' Request functions
            For i = 0 To funcs.Count - 1
                loggingMessage("adding function" & funcs.ElementAt(i).Value) 'DEBUG
                Try
                    exyconst.functionList.Add(funcs.ElementAt(i).Key, funcs.ElementAt(i).Value) ' Add to function list
                    exyconst.handlerList.Add(funcs.ElementAt(i).Key, hndI) ' Add to handler list
                Catch ex As Exception
                    loggingMessage("Add failed! It is usually because the function is already loaded, which is fine, but there may have been a naming conflict. " & ex.ToString) ' Don't make these fatal in case module writer messed up
                End Try

            Next
            loadedModules.Add(modName) ' Add to module list
        Next
    End Sub

    ' Turn file/string into intructions
    ' SYNTAXES
    ' [[varName]] - Replace with string
    ' " then closed with " - mutliline / unparsed string
    ' COMMENTS ARE NOT SUPPORTED BY DEFAULT
    Shared Sub parseStringToInstructions(ByVal inp As String, Optional ByVal reloadHandlers As Boolean = False)
        Try
            If reloadHandlers Then loadHandlers() ' Load functions
            instruct.Clear() ' Clear past instructions
            gotoLocations.Clear()
            Dim inst As List(Of String) = New List(Of String)(inp.Split(New String() {Environment.NewLine}, StringSplitOptions.None)) ' Split at newline
            Dim i = 0
            instructIndex = 0
            While i <= inst.Count - 1
                Dim curr = inst(i) 'load the current instruction
                Dim paramCount As Byte
                Try
                    If curr.EndsWith(";") Then ' Goto syntax, mark it
                        Try
                            loggingMessage("Goto noticed") ' DEBUG
                            Dim marker = curr.Split(";")(0)
                            i += 1 ' Move to next instruction
                            ' Check for an unexpected end
                            If i > inst.Count - 1 Then errorHandler(New Exception("Unexpected end. You cannot define a marker as the last operation of an instruction set."), "Error defining marker '" & curr & "'", "line " & i + 1, True)
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

                    ' Match with list and make sure it exists
                    loggingMessage("verifying function exists") 'DEBUG
                    paramCount = exyconst.functionList(curr)
                Catch ex As Exception
                    loggingMessage("error! probably unknown. " & ex.ToString) 'DEBUG
                    errorHandler(New Exception("Unknown function '" & curr & "'"), "Parse error", "line " & i + 1, False)
                End Try

                ' Process Parameters
                Dim params As List(Of String) = New List(Of String)
                params.Add(curr) ' Add instruction so it is index 0
                If paramCount > 0 Then
                    For i2 = 1 To paramCount
                        ' Get the params and add to param list
                        Dim prm = inst(i + i2)
                        If prm = ControlChars.Quote Then ' Handle multiline
                            loggingMessage("Handling multiline")
                            Dim i3 = i + i2 + 1 ' Jump to the line after we're on
                            Dim mulLine As New List(Of String)
                            Do

                                If i3 + 1 = inst.Count Then errorHandler(New Exception("Expected ending multiline quote, found EOF."), "Parameter Parse Error", "Line " & i + 1, True)
                                ' What! No closing quote! Smh.
                                If inst(i3) = ControlChars.Quote Then
                                    Exit Do ' We done
                                End If

                                mulLine.Add(inst(i3)) '' Add to the string

                                inst.RemoveAt(i3) ' Remove from the instruction set

                                ' DO NOT CHANGE i3. THERE IS NO NEED AS WE REMOVE IT AND IT DECREMENTS ANYWAY
                            Loop
                            ' Now we have a list of string split at newlines
                            prm = ""
                            For i4 = 0 To mulLine.Count - 1
                                prm &= mulLine(i4) & IIf(i4 < prm.Length - 1, vbNewLine, "") ' For each in there add it but not for last line
                            Next
                            ' That's it.
                            i += 1
                        End If
                        params.Add(prm) ' Add to param list
                        loggingMessage("added param") 'DEBUG
                    Next
                Else
                    params.Add("NonReq") ' All commands will recieve one param, prevents null errors, also tells dev of bug
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
            If loggingOn Then loggingMessage(ex.ToString)
            errorHandler(ex, "Unknown Syntax / Parse error", "Unknown Location", False)
        End Try
    End Sub

    ' Excecute code
    Shared Sub beginExcecution()
        ' Reset things
        varibles = New Dictionary(Of String, Object) From
        {
        {"_VERSION", exyVersion}
        }
        runningErrorHandling = New Dictionary(Of String, Object) From
     {
        {"enabled", False},
        {"evenFatal", False}, ' Hand fatal errors to the script
        {"gotoMarker", ""},
        {"warnings", True}
     }
        runningCode = True

        loggingMessage("excecution starting") ' DEBUG
        'todo check if instructions empty
        instructIndex = 0
        operationsCompleted = 0

        While instructIndex <= instruct.Count - 1
            wasGoodFunc = True
            Dim params As List(Of String) = New List(Of String)
            For Each param In instruct(instructIndex)
                params.Add(param)
            Next ' params remember START at 1
            Dim func = params(0) ' inst
            loggingMessage(func & " index " & instructIndex)
            ' Now we replace with varibles if params ask for them (surrounded with [[varibleName]] - It is important that with var/set you do not use this)
            ' pottentially v bad and slow code, may want to change if possible
            For i = 0 To params.Count - 2 'TODO: CHECK IF NO PARAMS
                ' For each real param replace
                For Each va In varibles
                    loggingMessage("current var dump: " & "[[" & va.Key & "]] " & va.Value.ToString)
                    If params(i + 1).Contains("[[" & va.Key & "]]") Then
                        loggingMessage("replacing " & "[[" & va.Key & "]]")
                        params(i + 1) = params(i + 1).Replace("[[" & va.Key & "]]", va.Value)
                    Else
                        loggingMessage("No need to replace " & params(i + 1) & " for " & va.Key)
                    End If

                Next
            Next
            loggingMessage("CURRENT TEMP: " & runtimeTemp.ToString)
            If func = "" Then
                ' Do nothing
                'elifs here for built in fucntions
            ElseIf func = "goto" Then ' !! ALWAYS FIRST TO HAVE FAST GOTO !!
                ' syntax goto + marker
                doGoto(params(1))
            ElseIf func = "require" Then
                If Not loadedModules.Contains(params(1)) Then
                    errorHandler(New Exception("The required module " & params(1) & " was not loaded."), "Runtime Error", "Manually suspended (require)", True)
                End If
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
                    errorHandler(ex, "Runtime error", "Instruction " & instructIndex, False)
                End Try

            Else
                'Not found
                errorHandler(New Exception("No handler for function '" & func & "'"), "Runtime error", "instruction position " & instructIndex, False)
            End If
            debuggerOutput(params)
            instructIndex += 1
            errorsInARow = IIf(wasGoodFunc, 0, errorsInARow)
            'Clean up then move on
            params.Clear()
            GC.Collect()
            operationsCompleted += 1

        End While
        loggingMessage("Completed " & operationsCompleted & " operations")
        runningCode = False
    End Sub

    Shared Sub doGoto(ByVal marker As String, Optional ByVal increment As Boolean = False)
        ' Perform a goto
        If marker = "continue" Then Exit Sub ' Continue, don't do goto
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
        wasGoodFunc = False
        errorsInARow += 1
        'Prevent handling infinite loops, check errors in a row.
        If errorsInARow > 253 Then
            Console.BackgroundColor = ConsoleColor.Black
            Console.ForegroundColor = ConsoleColor.Red
            Console.WriteLine("!!! Your script has been terminated due to a serious problem being detected. !!!")
            Console.WriteLine("
Too many errors occured in a row. As the error count is stored as a byte, it cannot exceed 255 before an overflow.
Please check your code and fix these errors.

Please ensure that you have no errors in your script, such as an infinite loop or missing error handlers.
You can check this by running EXY with logging ON.
")
            Console.WriteLine("Press any key to continue...")
            Console.ReadKey()
            End
        End If

        Try
            loggingMessage("ERROR HANDLER - err fatal: " & fatal.ToString() & vbNewLine &
                       "script handling: " & runningErrorHandling("enabled").ToString)
            If runningCode AndAlso IIf(runningErrorHandling("evenFatal"), True, Not fatal) AndAlso runningErrorHandling("enabled") Then
                loggingMessage("Script will handle error " & err.ToString())
                varMod("_ERROR", err.ToString)
                varMod("_ERRORLOC", ref & " - " & codeLine)
                doGoto(runningErrorHandling("gotoMarker"))
                Exit Sub
            End If
        Catch ex As Exception
            ' It may be a preruntime error so handle as fatal.
        End Try

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
        If Not loggingOn Then Exit Sub
        Console.BackgroundColor = ConsoleColor.Blue
        Console.ForegroundColor = ConsoleColor.Black
        Console.WriteLine("LOG: " & mes)
        Console.ResetColor()
    End Sub

    Shared Sub debuggerOutput(ByVal params As List(Of String))
        If debugMode AndAlso params(0) IsNot "" Then
            If IO.File.Exists("continue.file") Then Kill("continue.file")
            My.Computer.FileSystem.WriteAllText("functiondebug.json", Newtonsoft.Json.JsonConvert.SerializeObject(params), False)
            My.Computer.FileSystem.WriteAllText("vars.json", Newtonsoft.Json.JsonConvert.SerializeObject(varibles) & vbNewLine & vbNewLine & "Instruct: " & vbNewLine & Newtonsoft.Json.JsonConvert.SerializeObject(instruct), False)
            My.Computer.FileSystem.WriteAllText("tempruntime.txt", runtimeTemp.ToString, False)
            My.Computer.FileSystem.WriteAllText("continue.file", "", False)
            While IO.File.Exists("continue.file")
                Threading.Thread.Sleep(250) ' Check every 1/4 second
            End While
        End If
    End Sub
End Class


Public Class exytest
    Shared Sub parseStringTest()
        Console.WriteLine("Performing parse string test...")
        exyex.parseStringToInstructions("msg" & vbNewLine & "test1" & vbNewLine & "msg" & vbNewLine & "test2" & vbNewLine, True)
        Console.WriteLine("Test 1: " & IIf(exyex.instruct(0)(1) & exyex.instruct(1)(1) = "test1test2", "PASS", "FAIL :( " & exyex.instruct(0)(1) & exyex.instruct(1)(1)))
        Console.WriteLine()

        exyex.parseStringToInstructions("msg" & vbNewLine & "test12" & vbNewLine & "msg" & vbNewLine & "test22" & vbNewLine)
        Console.WriteLine("Test 2: " & IIf(exyex.instruct(0)(1) & exyex.instruct(1)(1) = "test12test22", "PASS", "FAIL :("))
        Console.WriteLine("Complete. Press any key to continue.")
        Console.ReadKey()
    End Sub
    Shared Sub gotoTest()
        Console.WriteLine("Performing goto test...")
        exyex.parseStringToInstructions("begin;" & vbNewLine & "msg" & vbNewLine & "test1" & vbNewLine & "middle;" & vbNewLine & "msg" & vbNewLine & "test2" & vbNewLine & "end;" & vbNewLine, True)
        Console.WriteLine("Marker test: " & IIf(exyex.gotoLocations("begin") = 0 AndAlso exyex.gotoLocations("middle") = 1 AndAlso exyex.gotoLocations("end") = 2, "PASS", "FAIL :("))
        Console.WriteLine()
        exyex.parseStringToInstructions("begin;" & vbNewLine & "msg" & vbNewLine & "test1" & vbNewLine & "begin;" & vbNewLine & "msg" & vbNewLine & "test1" & vbNewLine & "begin;" & vbNewLine & "msg" & vbNewLine & "test1" & vbNewLine & "begin;" & vbNewLine & "msg" & vbNewLine & "test1" & vbNewLine & "begin;" & vbNewLine & "msg" & vbNewLine & "test1" & vbNewLine & "begin;" & vbNewLine & "msg" & vbNewLine & "test1" & vbNewLine & "begin;" & vbNewLine & "msg" & vbNewLine & "test1" & vbNewLine & "begin;" & vbNewLine)
        Console.WriteLine("Overwrite test: " & IIf(exyex.gotoLocations("begin") = 0, "PASS", "FAIL :("))

        Console.ReadKey()
    End Sub

    Shared Sub executeTest()
        Console.WriteLine("Performing running test...")
        exyex.parseStringToInstructions("begin;" & vbNewLine & "log" & vbNewLine & "Hello world!" & vbNewLine & "goto" & vbNewLine & "begin", True)
        exyex.beginExcecution()
        Console.WriteLine("Done")
        Console.ReadKey()
    End Sub
End Class

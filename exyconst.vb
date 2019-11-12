
Public Class exyconst
    ' exyconst.vb - Contains all constants needed to run exy

    'Not static so addons can add their own commands.
    'Name of method, number of arguments (BYTE, max 255)
    Public Shared functionList As Dictionary(Of String, Byte) = New Dictionary(Of String, Byte)
    ' THESE are the PRIMARY functions that EXY wouldn't function without. Other included are in exyHandler_core

    ' Used to store handler classes
    Public Shared handlerClasses As List(Of Object) = New List(Of Object)

    ' This is used to match commands to their class - integer is the index in class list above
    Public Shared handlerList As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)

    Public Shared currentEXYex As Object = Nothing ' This is used in excecution to select and open a new instance of exyex

    Public Shared Function newEXYexInstance() As Object
        ' Why is this here? Because DLLs can't access the excecutable classes as they are compiled, so a new instance will
        ' have to be loaded into memory and passed to each module during excecution
        For Each exyexType In AppDomain.CurrentDomain.GetAssemblies().SelectMany(Function(t) t.GetTypes()).Where(Function(t) t.IsClass AndAlso t.Name.Contains("exyex"))
            Console.WriteLine("New EXY instance created")
            Return Activator.CreateInstance(exyexType)
            Exit Function
        Next
        Return Nothing
    End Function
    Public Shared Sub unhandledProtection()
        Dim currentDomain As AppDomain = AppDomain.CurrentDomain

        ' Define a handler for unhandled exceptions.
        AddHandler currentDomain.UnhandledException, AddressOf SeriousUnhandledError
    End Sub
    Public Shared Sub SeriousUnhandledError(ByVal sender As Object,
       ByVal e As UnhandledExceptionEventArgs)
        Dim EX As Exception
        EX = e.ExceptionObject
        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.Red
        Console.WriteLine("!!! Your script has been terminated due to a serious error. !!!")
        Console.WriteLine("Please ensure that you have no errors in your script, such as an infinite loop, before sending an error report.
You can check this by running EXY with logging ON.")
        Console.WriteLine(EX.StackTrace)
        Console.WriteLine(EX.ToString)
        End
    End Sub
End Class

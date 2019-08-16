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

End Class

@Echo off

SET OUTPUT=..\..\..\..\..\..\output
COPY /Y %OUTPUT%\Akka.Interfaced-SlimClient.Net35\bin\Debug\*.DLL
COPY /Y %OUTPUT%\Akka.Interfaced-SlimClient.Net35\bin\Debug\*.PDB
COPY /Y %OUTPUT%\Akka.Interfaced-SlimSocketClient.Net35\bin\Debug\*.DLL
COPY /Y %OUTPUT%\Akka.Interfaced-SlimSocketClient.Net35\bin\Debug\*.PDB

pause.

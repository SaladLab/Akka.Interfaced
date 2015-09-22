@Echo off

SET CORE=..\..\..\..\..\..\core
COPY /Y %CORE%\Akka.Interfaced-SlimClient\bin\Akka.Interfaced-SlimClient.Net35\Debug\*.DLL
COPY /Y %CORE%\Akka.Interfaced-SlimClient\bin\Akka.Interfaced-SlimClient.Net35\Debug\*.PDB
COPY /Y %CORE%\Akka.Interfaced-SlimSocketClient\bin\Akka.Interfaced-SlimSocketClient.Net35\Debug\*.DLL
COPY /Y %CORE%\Akka.Interfaced-SlimSocketClient\bin\Akka.Interfaced-SlimSocketClient.Net35\Debug\*.PDB

pause.

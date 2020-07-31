# PLC Communications

The Mirage project captures and collates data from numerous MiR autonomous robots.

It does so using a standard REST API and the HttpClient class, as part of the .Net Core C# Framework. The data from the robots is stored in a local MariaDB database and is replicated to a Cloud solution using TLS encryption. This is then displayed using a Grafana server.

## Important Notes

:warning: This project is in active development and so parameter names and behaviour will change without notice.

:warning: As a Worker Service, the program does not require user input apart from specyfying initial parameters.

## Installation

Mirage runs as a Windows Service however it can also be installed on Linux-based systems, in which case it runs as a Linux daemon using the systemd framework.

The installation requires elevated privilages. On Windows, this means Administrator account and on Linux root access. 



# Windows

Download the application from: link here.

Double click on Mirage.msi

Currently:

Open Command Prompt (not Powershell) as Admin. Go To:

C:\Users\PaliszewskiM\source\repos\Mirage\bin\Release\netcoreapp3.1

To install, type:

Mirage.exe install

To uninstall:

Mirage.exe uninstall

You can see diagnostics in the Microsoft Events Logs,

See build.md for detailed build instructions.

## System Requirements

Running the program requires approximately X of RAM and X amount of CPU power?

Breakdown of RAM per robot??

Connectivity analysis.

## Software Design Documents


## Documentation



## Dependencies, Copyright and Licences

The source code for MiRage is licensed under the GPLv3, see
[LICENSE.md](LICENSE.md).

It is Copyright Iconsys (Independent Control Systems) Ltd. The lead developers is Mikolaj Paliszewski

Licensing details for material from other projects may be found in
[NOTICE.md](NOTICE.md). In summary:

MiRage includes code modified from
[JSNO.NET](https://www.newtonsoft.com/json/help/html/Introduction.htm) which
is licensed under the MIT Licencse. The text of the license can be found at: <https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md>

# PLC Communications

The Mirage project captures and collates data from numerous MiR autonomous robots.

It does so using a standard REST API and the HttpClient class, as part of the .Net Core C# Framework. The data from the robots is stored in a local MariaDB database and is replicated to a Cloud solution using TLS encryption. This is then displayed using a Grafana server.

## PLC Support

Supports S7-300, S7-400, S7-1200 and S7-1500. The PLC configuration differs across different generations but the application supports all of them. The program was tested using S7-1500.

## Important Notes

:warning: This project is in active development and so parameter names and behaviour will change without notice.

:warning: As a Worker Service, the program does not require user input apart from specyfying initial parameters.

## Design

Bllah Blah blah


# Implementation

Say a couple of words about libnodave.

## PLC Configuration

## PLC Example Program

## Mirage Configuration

# Alerts


# PLC Communication

As one of its main features, AMR Connect is capable of communicating with Siemens S7 PLCs. This allows for easy integration with a lower level machine which might be a natural master of the overall system. For example, a conveyor systen controlled by a Siemens PLC could issue missions to collect and deliver parts from one side of the factory to another. In other words, it's a bridge between a low and high level systems. It can also act as a substitute SCADA system.

## PLC Support

Support is provided for the following PLC types: S7-300, S7-400, S7-1200 and S7-1500. The PLC configuration differs across different generations. AMR Connect can be configured via a web interface.

Beyond the configuration, the devices need to be on the same network, with TCP/IP port 120 open.

# Implementation

PLC communication is implemented via libnodave C# library which uses GET/PUT S7 Communication. For more information about GET/PUT, see Siemens documentation [here](https://cache.industry.siemens.com/dl/files/115/82212115/att_108330/v2/82212115_s7_communication_s7-1500_en.pdf). This is a well established method of data transfer often used between separate Siemens PLCs. 

The following instructions are available in S7 Communication:
• PUT for sending data
• GET for receiving data

In conjunction with a standard PLC library developed by iconsys, these are used in the following ways:
- To update a watchdog between the PLC and AMR Connect for health.
- To pass the current MiR data to the PLC for the purposes of safety. Currently, this includes position, speed, robot and mission status.
- To issue missions to individual robots (go to position A, start mission B).
- To issue missions to fleet manager.
- To keep the PLC updated with regards to connectivity or other failures.
- To read alarms from the PLC (on falling or rising edge) in case they affect the overall system (for example, a conveyor goes down so missions for it are cancelled). 

The data is written and read using PDU (protocol data unit) blocks which allow for a maximum read of 222 bytes and write of 218 bytes in a single transaction. The benefit of this method is that the reads/writes occur simultanously, meaning there's no room for poor timing as all bytes are read at the same time. If more data needs to be passed, this is done by using several transactions.

## Flow Overview

The PLC will have two data blocks: the Task Control block, used for issuing tasks to the robots; and the Data Storage block, used for storing data from the robots. The typical event flow will be:

1. PLC writes a task to Task Control

2. Picked up and executed by Mirage

3. Return data from fleet/robots is stored in Data Storage

4. Task Control is set to a corresponding Status Code (20 if successful, for example)

Task Control is scanned every loop, roughly once every 10ms.

See the Visio document for more details. Particularly sections “PLC Data Blocks” and “PLC w/comments”. Additional tasks, storage areas and codes can be added as required with minimum difficulty.

## PLC Configuration (In Detail)

Before starting work, the PLC needs to be configured so the driver can access the data. It’s fairly straightforward; there are three requirements:

1. Only global DBs can be accessed.

2. The optimized block access must be turned off.

3. The access level must be “full” and the “connection mechanism” must allow GET/PUT.

**1) This is self-explanatory**

**2) Changing the optimized block access**

Select the DB in the left pane under “Program blocks” and press Alt-Enter (or in the contextual menu select “Properties…”).
Go to the “Attributes” tab.
Uncheck Optimized block access - it’s checked by default.

![alt text](https://github.com/mp-iconsys/Mirage/blob/master/docs/Optimized%20Block%20Access.png "Optimized Block Access")

**3) Changing the access level**

Select the CPU project in the left pane and press Alt-Enter (or in the contextual menu select “Properties…”)
In the “Protection” tab, select “Full access” and Check “Permit access with PUT/GET ….” as in the picture.

![alt text](https://github.com/mp-iconsys/Mirage/blob/master/docs/Full%20Accesspng.png "Full Access")

## AMR Connect Configuration

The following details are necessary to set up communications on AMR Connect:

· IP of the PLC

· Port (102 is standard)

· Rack No (only required for older PLCs, for newer it’s set to 0)

· Slot (only required for older PLCs, for newer it’s set to 1)

In addition, the number of the data blocks as they appear in the PLC needs to be configured. This is only for the blocks that AMR Connect will read so Task Control and Data Storage blocks. If Task Control was DB5 in the PLC, 5 would be its number in AMR Connect.

All the data is read on start-up and cannot be configured during runtime. It’s accessed through a configuration file in the /config directory or from the database if it's been configured via the web browser. An example is given below:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <configSections>
    <section name="plc" type="siemens.plc" />
  </configSections>
  <plc>
    <connectionString>
      <ip>192.168.0.1</ip>
      <port>102</port>
      <rack>0</rack>
      <slot>1</slot>
    </connectionString>
    <data>
      <taskControlDB>5</taskControlDB>
      <dataStorageDB>6</dataStorageDB>
    </data>
  </plc>
</configuration>
```

In addition, if a block is used that’s not strictly dedicated to communicating with Mirage a data offset will need to be provided (set to 0 if a block is only used for comms).Required Details

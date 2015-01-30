### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

## Microsoft patterns & practices
# CQRS Journey Reference Implementation

http://cqrsjourney.github.com

## Appendix 1. Building and running the sample code (RI)

The most up-to-date version of the Release Notes is available [online](http://go.microsoft.com/fwlink/p/?LinkID=258574).

This appendix describes how to obtain, build, and run the RI.

These instructions describe five different scenarios for running the RI using the **Conference** Visual Studio solution:

1. Running the application on a local web server and using a local
   message bus and event store.
2. Running the application on a local web server and using the Windows
   Azure Service Bus and an event store that uses Windows Azure table
   storage.
3. Deploying the application to the local Windows Azure compute emulator
   and using a local message bus and event store.
4. Deploying the application to the local Windows Azure compute emulator
   and using the Windows Azure Service Bus and an event store that uses
   Windows Azure table storage.
5. Deploying the application to Windows Azure and using the Windows
   Azure Service Bus and an event store that uses Windows Azure table
   storage.

> **Note 1:** The local message bus and event store use SQL Express and
> are intended to help you run the application locally for demonstration
> purposes. They are not intended to illustrate a production-ready
> scenario.

> **Note 2:** Scenarios 1, 2, 3, and 4 use SQL Express for other data
> storage requirements. Scenario 5 requires you to use Windows Azure SQL Database instead
> of SQL Express.

> **Note 3:** The source code download for the V3 and later releases also includes a **Conference.NoAzureSDK**
> solution that enables you to build and run the sample application
> without installing the Windows Azure SDK. This solution supports
> scenarios 1 and 2 only.

# Prerequisites

Before you begin, you should install the following pre-requisites:

* Microsoft Visual Studio 2010 or later
* SQL Server 2008 Express or later
* ASP.NET MVC 3 and MVC 4 for the V1 and V2 releases
* ASP.NET MVC 4 Installer (Visual Studio 2010) for the V3 and later releases
* Windows Azure SDK for .NET - November 2011 for the V1 and V2 releases
* Windows Azure SDK for .NET - June 2012 or later for the V3 and later releases

> **Note:** The V1 and V2 releases of the sample application used
> ASP.NET MVC 3 in addition to ASP.NET MVC 4. As of the V3 release all
> of the web applications in the project use ASP.NET MVC 4.

> **Note:** The Windows Azure SDK is **not** a prerequisite if you plan to
> use the **Conference.NoAzureSDK** solution.

You can download and install all of these except for Visual Studio by
using the [Microsoft Web Platform Installer 4.0](http://www.microsoft.com/web/downloads/platform.aspx). 

You can install the remaining dependencies from NuGet by running the
script **install-packages.ps1** included with the downloadable source.

If you plan to deploy any part of the RI to Windows Azure (scenarios 2, 4, 5), you must have a Windows 
Azure subscription. You will need to configure a Windows Azure storage 
account (for blob storage), a Windows Azure Service Bus namespace, and a SQL Database
instance (they do not necessarily need to be in the same Windows Azure 
subscription). You should be aware, that depending on your Windows Azure 
subscription type, you may incur usage charges when you use the Windows 
Azure Service Bus, Windows Azure table storage, and when you deploy and 
run the RI in Windows Azure. 

At the time of writing, you can sign-up for a [Windows Azure free trial 
](http://www.windowsazure.com/en-us/pricing/free-trial/) that enables you to run the RI in Windows Azure. 

> **Note:** Scenario 1 enables you to run the RI locally without using
> the Windows Azure compute and storage emulators. 


# Obtaining the code

*	You can download the source code from the Microsoft Download center as a [self-extractable zip](http://go.microsoft.com/fwlink/p/?LinkID=258571).
*	Alternatively, you can get the [source code with the full git history](http://go.microsoft.com/fwlink/p/?LinkID=258576).


# Creating the databases

## SQL Express Database

For scenarios 1, 2, 3, and 4 you can create a local SQL Express database 
called **Conference** by running the script **Install-Database.ps1** in 
the scripts folder. 

The projects in the solution use this database to store application 
data. The SQL-based message bus and event store also use this database. 

## Windows Azure SQL Database instance

For scenario 5, you must create a SQL Database instance called
**Conference** by running the script **Install-Database.ps1** in 
the scripts folder.

The follow command will populate a SQL Database instance called 
**Conference** with the tables and views required to support the RI
(this script assumes that you have already created the **Conference**
database in SQL Database): 

```
.\Install-Database.ps1 -ServerName [your-sql-azure-server].database.windows.net -DoNotCreateDatabase -DoNotAddNetworkServiceUser -UseSqlServerAuthentication -UserName [your-sql-azure-username]
```

You must then modify the **ServiceConfiguration.Cloud.cscfg** file in the Conference.Azure project to use the following connection strings.

**SQL Database Connection String**

```
Server=tcp:[your-sql-azure-server].database.windows.net;Database=myDataBase;User ID=[your-sql-azure-username]@[your-sql-azure-server];Password=[your-sql-azure-password];Trusted_Connection=False;Encrypt=True; MultipleActiveResultSets=True;
```

**Windows Azure Connection String**

```
DefaultEndpointsProtocol=https;AccountName=[your-windows-azure-storage-account-name];AccountKey=[your-windows-azure-storage-account-key]
```

**Conference.Azure\ServiceConfiguration.Cloud.cscfg**

```
<?xml version="1.0" encoding="utf-8"?>
<ServiceConfiguration serviceName="Conference.Azure" xmlns="http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration" osFamily="1" osVersion="*">
  <Role name="Conference.Web.Admin">
    <Instances count="1" />
    <ConfigurationSettings>
      <Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="[your-windows-azure-connection-string]" />
      <Setting name="Diagnostics.ScheduledTransferPeriod" value="00:02:00" />
      <Setting name="Diagnostics.LogLevelFilter" value="Warning" />
      <Setting name="Diagnostics.PerformanceCounterSampleRate" value="00:00:30" />
      <Setting name="DbContext.ConferenceManagement" value="[your-sql-azure-connection-string]" />
      <Setting name="DbContext.SqlBus" value="[your-sql-azure-connection-string] />
    </ConfigurationSettings>
  </Role>
  <Role name="Conference.Web.Public">
    <Instances count="1" />
    <ConfigurationSettings>
      <Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="[your-windows-azure-connection-string]" />
      <Setting name="Diagnostics.ScheduledTransferPeriod" value="00:02:00" />
      <Setting name="Diagnostics.LogLevelFilter" value="Warning" />
      <Setting name="Diagnostics.PerformanceCounterSampleRate" value="00:00:30" />
      <Setting name="DbContext.Payments" value="[your-sql-azure-connection-string]" />
      <Setting name="DbContext.ConferenceRegistration" value="[your-sql-azure-connection-string]" />
      <Setting name="DbContext.SqlBus" value="[your-sql-azure-connection-string]" />
      <Setting name="DbContext.BlobStorage" value="[your-sql-azure-connection-string]" />
    </ConfigurationSettings>
  </Role>
  <Role name="WorkerRoleCommandProcessor">
    <Instances count="1" />
    <ConfigurationSettings>
      <Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="[your-windows-azure-connection-string]" />
      <Setting name="Diagnostics.ScheduledTransferPeriod" value="00:02:00" />
      <Setting name="Diagnostics.LogLevelFilter" value="Information" />
      <Setting name="Diagnostics.PerformanceCounterSampleRate" value="00:00:30" />
      <Setting name="DbContext.Payments" value="[your-sql-azure-connection-string]" />
      <Setting name="DbContext.EventStore" value="[your-sql-azure-connection-string]" />
      <Setting name="DbContext.ConferenceRegistrationProcesses" value="[your-sql-azure-connection-string]" />
      <Setting name="DbContext.ConferenceRegistration" value="[your-sql-azure-connection-string]" />
      <Setting name="DbContext.SqlBus" value="[your-sql-azure-connection-string]" />
      <Setting name="DbContext.BlobStorage" value="[your-sql-azure-connection-string]" />
      <Setting name="DbContext.ConferenceManagement" value="your-sql-azure-connection-string]" />
    </ConfigurationSettings>
  </Role>
</ServiceConfiguration>
```

> **Note:** The **LogLevelFilter** values for these roles is set to
> either **Warning** or **Information**. If you want to capture logs
> from the application into the **WADLogsTable**, you should change
> these values to **Verbose**.

# Creating the Settings.xml file

Before you can build the solution, you must create a **Settings.xml** 
file in the **Infrastructure Projects\Azure** solution folder. You can 
copy the **Settings.Template.xml** in this solution folder to create a 
**Settings.xml** file. 

> **Note:** You only need to create the **Settings.xml** file if you
> plan to use either the **Debug** or **Release** build configurations.

If you plan to use the Windows Azure Service Bus and the Windows Azure 
table storage based event store then you must edit the **Settings.xml** 
file in the **Infrastructure Projects\Azure** solution folder to include 
details of your Windows Azure storage account and a Windows Azure 
Service Bus namespace. 

> **Note:** See the contents of the **Settings.Template.xml** for details
> of the configuration information that is required.

> **Note:** You cannot currently use the Windows Azure storage emulator
> for the event store. You must use a real Windows Azure storage
> account.


# Building the RI

Open the **Conference** Visual Studio solution file in the code 
repository that you downloaded and un-zipped. 

You can use NuGet to download and install all of the dependencies by
running the script **install-packages.ps1** before building the
solution.

## Build configurations

The solution includes a number of build configurations. These are 
described in the following sections: 

### Release

Use the **Release** build configuration if you plan to deploy your 
application to Windows Azure. 

This solution uses the Windows Azure Service Bus to provide the 
messaging infrastructure. 

Use this build configuration if you plan to deploy the RI to Windows 
Azure (scenario 5). 

### Debug

Use the **Debug** build configuration if you plan either to deploy your 
application locally to the Windows Azure  compute emulator or to run as a standalone application locally and without using the Windows Azure compute emulator.

This solution uses the Windows Azure Service Bus to provide the 
messaging infrastructure and the event store based on Windows Azure 
table storage (scenarios 2 and 4). 

### DebugLocal

Use the **DebugLocal** build configuration if you plan to either deploy 
your application locally to the Windows Azure compute emulator or run 
the application on a local web server without using the Windows 
Azure compute emulator. 

This solution uses a local messaging infrastructure and event store 
built using SQL Server (scenarios 1 and 3). 

# Running the RI

When you run the RI, you should first create a conference, add at least
one seat type, and then publish the conference using the 
**Conference.Web.Admin** site.

After you have published the conference, you will then be able to use 
the site to order seats and use the simulated  payment process using 
the **Conference.Web.Public** site. 

The following sections describe how to run the RI using in the different 
scenarios. 

## Scenario 1. Local Web Server, SQL Event Bus, SQL Event Store

To run this scenario you should build the application using the 
**DebugLocal** configuration. 

Run the **WorkerRoleCommandProcessor** project as a console application. 

Run the **Conference.Web.Public** and **Conference.Web.Admin** (located 
in the **ConferenceManagement** folder) as web applications. 

> **Note:** The easiest way is to set multiple startup projects in the Visual Studio solution properties.

## Scenario 2. Local Web Server, Windows Azure Service Bus, Table Storage Event Store

To run this scenario you should build the application using the 
**Debug** configuration. 

Run the **WorkerRoleCommandProcessor** project as a console application. 

Run the **Conference.Web.Public** and **Conference.Web.Admin** (located 
in the **ConferenceManagement** folder) as web applications. 

## Scenario 3. Compute Emulator, SQL Event Bus, SQL Event Store

To run this scenario you should build the application using the 
**DebugLocal** configuration. 

Run the **Conference.Azure** Windows Azure project. 

> **Note:** To use the Windows Azure compute emulator you must launch
> Visual Studio as an administrator.

## Scenario 4. Compute Emulator, Windows Azure Service Bus, Table Storage Event Store

To run this scenario you should build the application using the 
**Debug** configuration. 

Run the **Conference.Azure** Windows Azure project. 

> **Note:** To use the Windows Azure compute emulator you must launch
> Visual Studio as an administrator.

## Scenario 5. Windows Azure, Windows Azure Service Bus, Table Storage Event Store 

Deploy the **Conference.Azure** Windows Azure project to your Windows 
Azure account. 

> **Note:** You must also ensure that you have created **Conference**
> database in SQL Database using the **Install-Database.ps1** in the
> scripts folder as described above. You must also ensure that you have 
> modified the connection strings in the
> configuration files in the solution to point to your SQL Database
> **Conference** database instead of your local SQL Express
> **Conference** database as described above.

# Running the tests

The following sections describe how to run the unit, integration, and 
acceptance tests. 

## Running the unit and integration Tests

The unit and integration tests in the **Conference** solution are 
created using **xUnit.net**. 

For more information about how you can run these tests, please visit the 
[xUnit.net][xunit] site on Codeplex. 

## Running the acceptance tests

The acceptance tests are located in the Visual Studio solution in the 
**Conference.AcceptanceTests** folder. 

You can use NuGet to download and install all of the dependencies by
running the script **install-packages.ps1** before building this
solution.

The acceptance tests are created using SpecFlow. For more information 
about SpecFlow, please visit [SpecFlow][specflow]. 

The SpecFlow tests are implemented using **xUnit.net**.

The **Conference.AcceptanceTests** solution uses the same build 
configurations as the **Conference** solution to control whether you run 
the acceptance tests against either the local SQL-based messaging 
infrastructure and event store or the Windows Azure Service Bus 
messaging infrastructure and Windows Azure table storage based event 
store.

You can use the xUnit console runner or a third-party tool with Visual Studio integration and xUnit support (for example TDD.net) to run the tests. The xUnit GUI tool is not supported.


[source]:          https://github.com/mspnp/cqrs-journey-code
[xunit]:           http://xunit.codeplex.com/
[specflow]:        http://www.specflow.org/
[connectionerror]: http://blogs.msdn.com/b/narahari/archive/2011/12/21/azure-a-connection-attempt-failed-because-the-connected-party-did-not-properly-respond-after-a-period-of-time-or-established-connection-failed-because-connected-host-has-failed-to-respond-x-x-x-x-x-quot.aspx
[v2outstanding]:   https://github.com/mspnp/cqrs-journey-code/issues/search?utf8=%E2%9C%93&q=v2
[v3outstanding]:   https://github.com/mspnp/cqrs-journey-code/issues?page=1&state=open
[azurerdp]:        http://msdn.microsoft.com/en-us/library/windowsazure/gg443832.aspx

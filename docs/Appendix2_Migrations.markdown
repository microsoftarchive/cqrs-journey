### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

## Microsoft patterns & practices
# CQRS Journey - Reference Implementation Migrations

07/25/2012   
[http://aka.ms/cqrs](http://aka.ms/cqrs)

The most up-to-date version of the Release Notes is available [online](http://go.microsoft.com/fwlink/p/?LinkID=258574).


# Migrating from the V1 to the V2 release

If you have been running the V1 release and have data that you would 
like to preserve as you migrate to the V2 release, the following steps 
describe how you can perform this migration if you are hosting the V1
release in Windows Azure. 

> **Note:** You should create a backup of the Conference database before you begin the migration.

1. Make sure that the V1 release is running in your Windows Azure production environment.
2. Deploy the V2 release to your Windows Azure staging environment. The
   V2 release has a global **MaintenanceMode** property that is
   initially set to **true**. In this mode, the application displays a
   message to the user that the site is currently undergoing maintenance.
3. When you are ready, swap the V2 release (still in maintenance mode)
   into your Windows Azure production environment.
4. Leave the V1 release (now running in the staging environment) to run
   for a few minutes to ensure that all in-flight messages complete
   their processing.
5. Run the migration program to migrate the data (see below).
6. After the data migration completes successfully, change the
   **MaintenanceMode** property to **false**.
7. The V2 release is now live in Windows Azure.

> **Note:** You can change the value of the **MaintenanceMode** property
> in the Windows Azure Management Portal.

## Running the migration program to migrate the data

_Before beginning the data migration process, ensure that you have a 
backup of the data from your SQL Database database._ 

The **MigrationToV2** utility uses the same **Settings.xml** file as the 
other projects in the **Conference** solution in addition to its own 
**App.config** file to specify the Windows Azure storage account 
and SQL connection strings.

The **Settings.xml** file contains the names of the new Windows Azure 
tables that the V2 release uses. If you are migrating data from V1 to V2 
ensure that the name of the **EventSourcing** table is different from the 
name of the table used by the V1 release. The name of the table used by 
the V1 release is hardcoded in the **Program.cs** file in the MigrationToV2 
project: 

```
var originalEventStoreName = "ConferenceEventStore";
```

The name of the new table for V2 is in the **Settings.xml** file:

```
<EventSourcing>
	<ConnectionString>...</ConnectionString>
	<TableName>ConferenceEventStoreApplicationDemoV2</TableName>
</EventSourcing>
```

> **Note:** The migration utility assumes that the V2 event sourcing
> table is in the same Windows Azure storage account as the V1 event
> sourcing table. If this is not the case, you will need to modify the
> MigrationToV2 application code.

The **App.config** file contains the **DbContext.ConferenceManagement** 
connection string. The migration utility uses this connection string to 
connect to the SQL Database instance that contains the SQL tables used by 
the application. Ensure that this connection string points to the Windows Azure SQL 
Database that contains your production data. You can verify which 
SQL Database instance your production environment uses by looking in the 
active **ServiceConfiguration.csfg** file. 

> **Note:** If you are running the application locally using the
> **Debug** configuration, the **DbContext.ConferenceManagement**
> connection string will point to local SQL Express database.

> **Note:** To avoid data transfer charges, you should run the migration
> utility inside a Windows Azure worker role instead of on-premise. The
> solution includes an empty, configured Windows Azure worker role in the
> **MigrationToV2.Azure** with diagnostics that you
> can use for this purpose. For information about how to run an
> application inside a Windows Azure role instance, see [Using Remote
> Desktop with Windows Azure Roles](http://msdn.microsoft.com/en-us/library/windowsazure/gg443832.aspx). 

> **Note:** Migration from V1 to V2 is not supported if you are using
> the **DebugLocal** configuration.

### If the data migration fails

If the data migration process fails for any reason, then before you retry the migration you should:

1. Restore the SQL Database back to its state before you ran the
   migration utility.
2. Delete the two new Windows Azure tables defined in **Settings.xml**
   in the **EventSourcing** and **MessageLog** sections.

# Migrating from the V2 to the V3 release

If you have been running the V2 release and have data that you would 
like to preserve as you migrate to the V3 release, the following steps 
describe how you can perform this migration if you are hosting the V2
release in Windows Azure. 

> **Note:** You should create a backup of the Conference database before you begin the migration.

1.	Make sure that the V2 release is running in your Windows Azure production environment.

2. Deploy the V3 release to your Windows Azure staging environment. In the
   V3 release, the command processor worker role has a **MaintenanceMode** property that is
   initially set to **true**.
3. Start the ad hoc MigrationToV3.InHouseProcessor utility to rebuild the read models for the V3 deployment.
4. Change the **MaintenanceMode** property of the command processor worker role in the V2 release (running in the production slot) to **true**.  
At this point, the application is still running, but the registrations cannot progress. You should wait until the status of the worker role instance shows as **Ready** in the Windows Azure portal (this may take some time).
5. Change the **MaintenanceMode** property of the command processor worker role in the V3 release (running in the staging slot) to **false** and allow the MigrationToV3.InHouseProcessor utility to start handling the V2 events. The migration utility prompts you to start handling these V2 events when you are ready. This change is faster than changing the value of the **MaintenanceMode** property in the V2 release. When this change is complete, the V2 release web roles are using the data processed by the V3 version of the  worker role. This configuration change also triggers the database migration.
6. In the Windows Azure portal, perform a VIP swap to make the V3 web roles visible externally.
7. Shutdown the V2 deployment that is now running in the staging slot.
8. The V3 release is now live in Windows Azure.

> **Note:** You can change the value of the **MaintenanceMode** property
> in the Windows Azure Management Portal.
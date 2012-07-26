## [Microsoft patterns & practices](http://msdn.microsoft.com/practices)
# CQRS Journey - Known Issues

07/25/2012   
[http://aka.ms/cqrs](http://aka.ms/cqrs)

The most up-to-date version of the Release Notes is available [online](http://go.microsoft.com/fwlink/p/?LinkID=258574)



### 1. Error messages by the command processor 
When running the RI using the steps described in scenario 1 and 2, the worker role command processor console may display the errors below:

```
Loaded "Microsoft.WindowsAzure.ServiceRuntime, Version=1.7.0.0, Culture=neutral
PublicKeyToken=31bf3856ad364e35"
Getting "DbContext.SqlBus" from ServiceRuntime: FAIL.
Getting "DbContext.SqlBus" from ConfigurationManager: FAIL.
```

These errors do not affect the functionality of the application, are handled internally, and can be ignored. 

### 2. No error handling in web front ends

You will see an unhandled error in your browser for some error conditions. For example, you will see an unhandled error if the database you are using is offline.

### 3. It is possible to access an unpublished conference

If you create a conference, such as "myconf," publish it, and then subsequently unpublish it, you can access the unpublished conference at the URL it was available at when it was in the published state.

In this scenario, the user should be redirected to an error page, but instead is able to register on the unpublished conference.

### 4. Running the acceptance tests using the xUnit console

If you want to run the acceptance tests in the **Conference.AcceptanceTests** solution, you must add the following to the xUnit console configuration file:

```Xml
<startup useLegacyV2RuntimeActivationPolicy="true">
  <supportedRuntime version="v4.0" />
</startup>
```

### 5. Acceptance tests browser support

Internet Explorer 9 is the only supported browser for the SpecFlow acceptance tests.

### 6. Localizability is not in scope

The RI is not designed with localizability in mind. For example, it 
currently contains hardcoded strings, fixed number formats, and so on. 

### 7. Runtime Activation Error in Debug Mode

When you run the application in debug mode you will see an error in the
**Conference.Web.Public** web application:

```
Activation error occured while trying to get instance of type IControllerFactory
```

Click in the **Continue** button and the application will run as
expected. You may see the error multiple times.

### 8.	Server error in '/' application

When you run the application locally and you are using a proxy server you see:

```
Server Error in '/' Application.
A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond ...

Description: An unhandled exception occurred during the execution of the current web request. Please review the stack trace for more information about the error and where it originated in the code.

Exception Details: System.Net.Sockets.SocketException: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond ...
```

For help resolving this issue see [Azure: A connection attempt failed...][connectionerror]

### 9. Switching between Debug and DebugLocal builds

If you run the application after building using the Debug configuration, 
create some data, and then re-build using the DebugLocal configuration 
you will see errors when you run the application. This scenario is not 
supported. 

The problem arises because the two build configurations only share some 
data sources, so after the switch there are inconsistencies in the data. 
You should re-create all the data sources if you switch from one build 
configuration to another.

### 10. Other Known Issues

* 	No security features have been implemented.
*	Only basic UI validation is performed.
*	You can see the list of all outstanding issues [here](https://github.com/mspnp/cqrs-journey-code/issues?page=1&state=open).

[connectionerror]: http://blogs.msdn.com/b/narahari/archive/2011/12/21/azure-a-connection-attempt-failed-because-the-connected-party-did-not-properly-respond-after-a-period-of-time-or-established-connection-failed-because-connected-host-has-failed-to-respond-x-x-x-x-x-quot.aspx





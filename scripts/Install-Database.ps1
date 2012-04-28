# ==============================================================================================================
# Microsoft patterns & practices
# CQRS Journey project
# ==============================================================================================================
# ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
# http://cqrsjourney.github.com/contributors/members
# Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
# with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software distributed under the License is 
# distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
# See the License for the specific language governing permissions and limitations under the License.
# ==============================================================================================================

<#
.SYNOPSIS 
 Creates the dabase objects for the Conference Management sample application
.EXAMPLE
 .\Install-Database.ps1
 Creates the database objects using the default server and database names, and using Windows Authentication.
.EXAMPLE
 .\Install-Database.ps1 -UserName myUser -ServerName myServerName -DatabaseName cqrs -CreateDatabase
 Creates the database objects on a new database named 'cqrs' on server 'myServerName', using Sql Server Authentication with user name 'myUser'.
#>

[CmdletBinding(DefaultParameterSetName="WindowsAuthentication")]
param (
# The database server name. The default value is '.\SQLEXPRESS'.
    [string] $ServerName = ".\SQLEXPRESS",
# The database name. The default value is 'conference'.
    [string] $DatabaseName = "conference",
# Creates the database in addition to the database objects
    [switch] $CreateDatabase,
# Adds a server login and a database user with read and write access for 'NETWORK SERVICE'
    [switch] $AddNetworkServiceUser,
# Uses Windows Authentication to connect to the database server. This is the default authentication mode.
    [Parameter(ParameterSetName='WindowsAuthentication')]
    [switch] $UseWindowsAuthentication = [switch]::present,
# Uses Sql Server Authentication to connect to the database server
    [Parameter(ParameterSetName='SqlServerAuthentication')]
    [switch] $UseSqlServerAuthentication = [switch]::present,
# Sql Server Authentication user name
    [Parameter(ParameterSetName='SqlServerAuthentication', Mandatory=$true)]
    [string] $UserName
)

switch($PsCmdlet.ParameterSetName)
{
  "WindowsAuthentication" { $authenticationParamters = "-E" }
  "SqlServerAuthentication" { $authenticationParamters = "-U $UserName" }
}

$scriptPath = Split-Path (Get-Variable MyInvocation -Scope 0).Value.MyCommand.Path

$baseOsqlCommandLine = "osql -S $ServerName $authenticationParamters "

#Write-Host $baseOsqlCommandLine

if($CreateDatabase.IsPresent)
{
    Write-Host "Creating the database"
    
    $createDatabaseCommandLine = $baseOsqlCommandLine + "-Q 'CREATE DATABASE $DatabaseName'"
    Write-Host $createDatabaseCommandLine
    Invoke-Expression $createDatabaseCommandLine

    Write-Host "Database created"
    Write-Host
}

if($AddNetworkServiceUser.IsPresent)
{
    Write-Host "Creating the NETWORK SERVICE user"

    $addNetworkServiceUserScript = Join-Path $scriptPath "AddNetworkServiceUser.sql"
    $addNetworkServiceUserCommandLine = $baseOsqlCommandLine + "-d $DatabaseName -i '$addNetworkServiceUserScript'"
    Write-Host $addNetworkServiceUserCommandLine
    Invoke-Expression $addNetworkServiceUserCommandLine

    Write-Host
    Write-Host "User created"
    Write-Host
}

Write-Host "Creating the database objects"

$createDatabaseObjectsScript = Join-Path $scriptPath "CreateDatabaseObjects.sql"
$createDatabaseObjectsCommandLine = $baseOsqlCommandLine + "-d $DatabaseName -i '$createDatabaseObjectsScript'"
Write-Host $createDatabaseObjectsCommandLine
Invoke-Expression $createDatabaseObjectsCommandLine

Write-Host
Write-Host "Database objects created"
Write-Host
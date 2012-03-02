# ==============================================================================================================
# Microsoft patterns & practices
# CQRS Journey project
# ==============================================================================================================
# Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
# Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
# with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software distributed under the License is 
# distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
# See the License for the specific language governing permissions and limitations under the License.
# ==============================================================================================================
#
# This assumes that the nuget command line is on the path. You can get it from:
# http://nuget.codeplex.com/releases/58939/download/222685
# instead of running this file you can 
#	- open the solution
#	- right-click on the solution in the solution explorer
#	- select Enable Package Restore

if (Test-Path .\nuget.exe)
{
	$nuget = '.\nuget.exe'
}
else
{
	$nuget = 'nuget.exe'
}


# TODO: List all dependencies and prompt to continue
Get-Item **\packages.config | ForEach-Object { & $nuget install $_.FullName -o packages }

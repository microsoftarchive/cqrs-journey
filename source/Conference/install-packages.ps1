# this assumes that the nuget command line is on the path. You can get it from: http://nuget.codeplex.com/releases/58939/download/222685
# instead of running this file you can 
#	- open the solution
#	- right-click on the solution in the solution explorer
#	- select Enable Package Restore

# TODO: List all dependencies and prompt to continue
Get-Item **\packages.config | ForEach-Object { & .\nuget.exe install $_.FullName -o packages }

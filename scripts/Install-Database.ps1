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
 Creates the dabase objects for the Conference Management sample application, optionally creating the database and a login for the NETWORK SERVICE user.
.EXAMPLE
 .\Install-Database.ps1
 Creates the database, database objects and login for NETWORK SERVICE using the default server and database names, and using Windows Authentication.
.EXAMPLE
 .\Install-Database.ps1 -UserName myUser -ServerName myServerName -DatabaseName cqrs -DoNotCreateDatabase -DoNotAddNetworkServiceUser
 Creates the database objects on a new database named 'cqrs' on server 'myServerName', using Sql Server Authentication with user name 'myUser'.
#>

[CmdletBinding(DefaultParameterSetName="WindowsAuthentication")]
param (
# The database server name. The default value is '.\SQLEXPRESS'.
    [string] $ServerName = ".\SQLEXPRESS",
# The database name. The default value is 'conference'.
    [string] $DatabaseName = "conference",
# Does not create the database in addition to the database objects
    [switch] $DoNotCreateDatabase,
# Does not add a server login and a database user with read and write access for 'NETWORK SERVICE'
    [switch] $DoNotAddNetworkServiceUser,
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

if(-not $DoNotCreateDatabase.IsPresent)
{
    Write-Host "Creating the database"
    
    $createDatabaseCommandLine = $baseOsqlCommandLine + "-Q 'CREATE DATABASE $DatabaseName'"
    Write-Host $createDatabaseCommandLine
    Invoke-Expression $createDatabaseCommandLine

    Write-Host "Database created"
    Write-Host
}

if(-not $DoNotAddNetworkServiceUser.IsPresent)
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
# SIG # Begin signature block
# MIIamAYJKoZIhvcNAQcCoIIaiTCCGoUCAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUq7lgSQ1tXQUZbYuggVkh1+D6
# evSgghUtMIIEoDCCA4igAwIBAgIKYRnMkwABAAAAZjANBgkqhkiG9w0BAQUFADB5
# MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVk
# bW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSMwIQYDVQQDExpN
# aWNyb3NvZnQgQ29kZSBTaWduaW5nIFBDQTAeFw0xMTEwMTAyMDMyMjVaFw0xMzAx
# MTAyMDMyMjVaMIGDMQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQ
# MA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9u
# MQ0wCwYDVQQLEwRNT1BSMR4wHAYDVQQDExVNaWNyb3NvZnQgQ29ycG9yYXRpb24w
# ggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDuW759ESTjhgbgZv9ItRe9
# AuS0DDLwcj59LofXTqGxp0Mv92WeMeEyMUWu18EkhCHXLrWEfvo101Mc17ZRHk/O
# ZrnrtwwC/SlcraiH9soitNW/CHX1inCPY9fvih7pj0MkZFrTh32QbTusds1XNn3o
# vBBWrJjwiV0uZMavJgleHmMV8T2/Fo+ZiALDMLfBC2AfD3LM1reoNRKGm6ELCuaT
# W476VJzB8xlfQo0Snx0/kLcnE4MZMoId89mH1CGyPKK2B0/XJKrujfWz2fr5OU+n
# 6fKvWVL03EGbLxFwY93q3qrxbSEEEFMzu7JPxeFTskFlR2439rzpmxZBkWsuWzDD
# AgMBAAGjggEdMIIBGTATBgNVHSUEDDAKBggrBgEFBQcDAzAdBgNVHQ4EFgQUG1IO
# 8xEqt8CJwxGBPdSWWLmjU24wDgYDVR0PAQH/BAQDAgeAMB8GA1UdIwQYMBaAFMsR
# 6MrStBZYAck3LjMWFrlMmgofMFYGA1UdHwRPME0wS6BJoEeGRWh0dHA6Ly9jcmwu
# bWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY0NvZFNpZ1BDQV8wOC0z
# MS0yMDEwLmNybDBaBggrBgEFBQcBAQROMEwwSgYIKwYBBQUHMAKGPmh0dHA6Ly93
# d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWljQ29kU2lnUENBXzA4LTMxLTIw
# MTAuY3J0MA0GCSqGSIb3DQEBBQUAA4IBAQClWzZsrU6baRLjb4oCm2l3w2xkciiI
# 2T1FbSwYe9QoLxPiWWobwgs0t4r96rmU7Acx5mr0dQTTp9peOgaeEP2pDb2cUUNv
# /2eUnOHPfPAksDXMg13u2sBvNknAWgpX9nPhnvPjCEw7Pi/M0s3uTyJw9wQfAqZL
# m7iPXIgONpRsMwe4qa1RoNDC3I4iEr3D34LXVqH33fClIFcQEJ3urIZ0bHGbwfDy
# wnBep9ttTTdYmU15QNA0XVolrmfrG05GBrCMKR+jEI+lM58j1fi1Rn3g7mOYkEs+
# BagvsBizWaSvQVOOCAUQLSrJOgZMHC6pMVFWZKyazKyXmCmKl5CH6p22MIIEujCC
# A6KgAwIBAgIKYQUTNgAAAAAAGjANBgkqhkiG9w0BAQUFADB3MQswCQYDVQQGEwJV
# UzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UE
# ChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSEwHwYDVQQDExhNaWNyb3NvZnQgVGlt
# ZS1TdGFtcCBQQ0EwHhcNMTEwNzI1MjA0MjE3WhcNMTIxMDI1MjA0MjE3WjCBszEL
# MAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1v
# bmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjENMAsGA1UECxMETU9Q
# UjEnMCUGA1UECxMebkNpcGhlciBEU0UgRVNOOjE1OUMtQTNGNy0yNTcwMSUwIwYD
# VQQDExxNaWNyb3NvZnQgVGltZS1TdGFtcCBTZXJ2aWNlMIIBIjANBgkqhkiG9w0B
# AQEFAAOCAQ8AMIIBCgKCAQEAnDSYGckJKWOZAhZ1qIhXfaG7qUES/GSRpdYFeL93
# 3OzmrrhQTsDjGr3tt/34IIpxOapyknKfignlE++RQe1hJWtRre6oQ7VhQiyd8h2x
# 0vy39Xujc3YTsyuj25RhgFWhD23d2OwW/4V/lp6IfwAujnokumidj8bK9JB5euGb
# 7wZdfvguw2oVnDwUL+fVlMgiG1HLqVWGIbda80ESOZ/wValOqiUrY/uRcjwPfMCW
# ctzBo8EIyt7FybXACl+lnAuqcgpdCkB9LpjQq7KIj4aA6H3RvlVr4FgsyDY/+eYR
# w/BDBYV4AxflLKcpfNPilRcAbNvcrTwZOgLgfWLUzvYdPQIDAQABo4IBCTCCAQUw
# HQYDVR0OBBYEFPaDiyCHEe6Dy9vehaLSaIY3YXSQMB8GA1UdIwQYMBaAFCM0+NlS
# RnAK7UD7dvuzK7DDNbMPMFQGA1UdHwRNMEswSaBHoEWGQ2h0dHA6Ly9jcmwubWlj
# cm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY3Jvc29mdFRpbWVTdGFtcFBD
# QS5jcmwwWAYIKwYBBQUHAQEETDBKMEgGCCsGAQUFBzAChjxodHRwOi8vd3d3Lm1p
# Y3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY3Jvc29mdFRpbWVTdGFtcFBDQS5jcnQw
# EwYDVR0lBAwwCgYIKwYBBQUHAwgwDQYJKoZIhvcNAQEFBQADggEBAGL0BQ1P5xtr
# gudSDN95jKhVgTOX06TKyf6vSNt72m96KE/H0LeJ2NGmmcyRVgA7OOi3Mi/u+c9r
# 2Zje1gL1QlhSa47aQNwWoLPUvyYVy0hCzNP9tPrkRIlmD0IOXvcEnyNIW7SJQcTa
# bPg29D/CHhXfmEwAxLLs3l8BAUOcuELWIsiTmp7JpRhn/EeEHpFdm/J297GOch2A
# djw2EUbKfjpI86/jSfYXM427AGOCnFejVqfDbpCjPpW3/GTRXRjCCwFQY6f889GA
# noTjMjTdV5VAo21+2usuWgi0EAZeMskJ6TKCcRan+savZpiJ+dmetV8QI6N3gPJN
# 1igAclCFvOUwggW8MIIDpKADAgECAgphMyYaAAAAAAAxMA0GCSqGSIb3DQEBBQUA
# MF8xEzARBgoJkiaJk/IsZAEZFgNjb20xGTAXBgoJkiaJk/IsZAEZFgltaWNyb3Nv
# ZnQxLTArBgNVBAMTJE1pY3Jvc29mdCBSb290IENlcnRpZmljYXRlIEF1dGhvcml0
# eTAeFw0xMDA4MzEyMjE5MzJaFw0yMDA4MzEyMjI5MzJaMHkxCzAJBgNVBAYTAlVT
# MRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQK
# ExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xIzAhBgNVBAMTGk1pY3Jvc29mdCBDb2Rl
# IFNpZ25pbmcgUENBMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAsnJZ
# XBkwZL8dmmAgIEKZdlNsPhvWb8zL8epr/pcWEODfOnSDGrcvoDLs/97CQk4j1XIA
# 2zVXConKriBJ9PBorE1LjaW9eUtxm0cH2v0l3511iM+qc0R/14Hb873yNqTJXEXc
# r6094CholxqnpXJzVvEXlOT9NZRyoNZ2Xx53RYOFOBbQc1sFumdSjaWyaS/aGQv+
# knQp4nYvVN0UMFn40o1i/cvJX0YxULknE+RAMM9yKRAoIsc3Tj2gMj2QzaE4BoVc
# TlaCKCoFMrdL109j59ItYvFFPeesCAD2RqGe0VuMJlPoeqpK8kbPNzw4nrR3XKUX
# no3LEY9WPMGsCV8D0wIDAQABo4IBXjCCAVowDwYDVR0TAQH/BAUwAwEB/zAdBgNV
# HQ4EFgQUyxHoytK0FlgByTcuMxYWuUyaCh8wCwYDVR0PBAQDAgGGMBIGCSsGAQQB
# gjcVAQQFAgMBAAEwIwYJKwYBBAGCNxUCBBYEFP3RMU7TJoqV4ZhgO6gxb6Y8vNgt
# MBkGCSsGAQQBgjcUAgQMHgoAUwB1AGIAQwBBMB8GA1UdIwQYMBaAFA6sgmBAVieX
# 5SUT/CrhClOVWeSkMFAGA1UdHwRJMEcwRaBDoEGGP2h0dHA6Ly9jcmwubWljcm9z
# b2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL21pY3Jvc29mdHJvb3RjZXJ0LmNybDBU
# BggrBgEFBQcBAQRIMEYwRAYIKwYBBQUHMAKGOGh0dHA6Ly93d3cubWljcm9zb2Z0
# LmNvbS9wa2kvY2VydHMvTWljcm9zb2Z0Um9vdENlcnQuY3J0MA0GCSqGSIb3DQEB
# BQUAA4ICAQBZOT5/Jkav629AsTK1ausOL26oSffrX3XtTDst10OtC/7L6S0xoyPM
# fFCYgCFdrD0vTLqiqFac43C7uLT4ebVJcvc+6kF/yuEMF2nLpZwgLfoLUMRWzS3j
# StK8cOeoDaIDpVbguIpLV/KVQpzx8+/u44YfNDy4VprwUyOFKqSCHJPilAcd8uJO
# +IyhyugTpZFOyBvSj3KVKnFtmxr4HPBT1mfMIv9cHc2ijL0nsnljVkSiUc356aNY
# Vt2bAkVEL1/02q7UgjJu/KSVE+Traeepoiy+yCsQDmWOmdv1ovoSJgllOJTxeh9K
# u9HhVujQeJYYXMk1Fl/dkx1Jji2+rTREHO4QFRoAXd01WyHOmMcJ7oUOjE9tDhNO
# PXwpSJxy0fNsysHscKNXkld9lI2gG0gDWvfPo2cKdKU27S0vF8jmcjcS9G+xPGeC
# +VKyjTMWZR4Oit0Q3mT0b85G1NMX6XnEBLTT+yzfH4qerAr7EydAreT54al/RrsH
# YEdlYEBOsELsTu2zdnnYCjQJbRyAMR/iDlTd5aH75UcQrWSY/1AWLny/BSF64pVB
# J2nDk4+VyY3YmyGuDVyc8KKuhmiDDGotu3ZrAB2WrfIWe/YWgyS5iM9qqEcxL5rc
# 43E91wB+YkfRzojJuBj6DnKNwaM9rwJAav9pm5biEKgQtDdQCNbDPTCCBgcwggPv
# oAMCAQICCmEWaDQAAAAAABwwDQYJKoZIhvcNAQEFBQAwXzETMBEGCgmSJomT8ixk
# ARkWA2NvbTEZMBcGCgmSJomT8ixkARkWCW1pY3Jvc29mdDEtMCsGA1UEAxMkTWlj
# cm9zb2Z0IFJvb3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5MB4XDTA3MDQwMzEyNTMw
# OVoXDTIxMDQwMzEzMDMwOVowdzELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjEhMB8GA1UEAxMYTWljcm9zb2Z0IFRpbWUtU3RhbXAgUENBMIIBIjAN
# BgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAn6Fssd/bSJIqfGsuGeG94uPFmVEj
# UK3O3RhOJA/u0afRTK10MCAR6wfVVJUVSZQbQpKumFwwJtoAa+h7veyJBw/3DgSY
# 8InMH8szJIed8vRnHCz8e+eIHernTqOhwSNTyo36Rc8J0F6v0LBCBKL5pmyTZ9co
# 3EZTsIbQ5ShGLieshk9VUgzkAyz7apCQMG6H81kwnfp+1pez6CGXfvjSE/MIt1Nt
# UrRFkJ9IAEpHZhEnKWaol+TTBoFKovmEpxFHFAmCn4TtVXj+AZodUAiFABAwRu23
# 3iNGu8QtVJ+vHnhBMXfMm987g5OhYQK1HQ2x/PebsgHOIktU//kFw8IgCwIDAQAB
# o4IBqzCCAacwDwYDVR0TAQH/BAUwAwEB/zAdBgNVHQ4EFgQUIzT42VJGcArtQPt2
# +7MrsMM1sw8wCwYDVR0PBAQDAgGGMBAGCSsGAQQBgjcVAQQDAgEAMIGYBgNVHSME
# gZAwgY2AFA6sgmBAVieX5SUT/CrhClOVWeSkoWOkYTBfMRMwEQYKCZImiZPyLGQB
# GRYDY29tMRkwFwYKCZImiZPyLGQBGRYJbWljcm9zb2Z0MS0wKwYDVQQDEyRNaWNy
# b3NvZnQgUm9vdCBDZXJ0aWZpY2F0ZSBBdXRob3JpdHmCEHmtFqFKoKWtTHNY9AcT
# LmUwUAYDVR0fBEkwRzBFoEOgQYY/aHR0cDovL2NybC5taWNyb3NvZnQuY29tL3Br
# aS9jcmwvcHJvZHVjdHMvbWljcm9zb2Z0cm9vdGNlcnQuY3JsMFQGCCsGAQUFBwEB
# BEgwRjBEBggrBgEFBQcwAoY4aHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraS9j
# ZXJ0cy9NaWNyb3NvZnRSb290Q2VydC5jcnQwEwYDVR0lBAwwCgYIKwYBBQUHAwgw
# DQYJKoZIhvcNAQEFBQADggIBABCXisNcA0Q23em0rXfbznlRTQGxLnRxW20ME6vO
# vnuPuC7UEqKMbWK4VwLLTiATUJndekDiV7uvWJoc4R0Bhqy7ePKL0Ow7Ae7ivo8K
# BciNSOLwUxXdT6uS5OeNatWAweaU8gYvhQPpkSokInD79vzkeJkuDfcH4nC8GE6d
# jmsKcpW4oTmcZy3FUQ7qYlw/FpiLID/iBxoy+cwxSnYxPStyC8jqcD3/hQoT38IK
# YY7w17gX606Lf8U1K16jv+u8fQtCe9RTciHuMMq7eGVcWwEXChQO0toUmPU8uWZY
# sy0v5/mFhsxRVuidcJRsrDlM1PZ5v6oYemIp76KbKTQGdxpiyT0ebR+C8AvHLLvP
# Q7Pl+ex9teOkqHQ1uE7FcSMSJnYLPFKMcVpGQxS8s7OwTWfIn0L/gHkhgJ4VMGbo
# QhJeGsieIiHQQ+kr6bv0SMws1NgygEwmKkgkX1rqVu+m3pmdyjpvvYEndAYR7nYh
# v5uCwSdUtrFqPYmhdmG0bqETpr+qR/ASb/2KMmyy/t9RyIwjyWa9nR2HEmQCPS2v
# WY+45CHltbDKY7R4VAXUQS5QrJSwpXirs6CWdRrZkocTdSIvMqgIbqBbjCW/oO+E
# yiHW6x5PyZruSeD3AWVviQt9yGnI5m7qp5fOMSn/DsVbXNhNG6HY+i+ePy5VFmvJ
# E6P9MYIE1TCCBNECAQEwgYcweTELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjEjMCEGA1UEAxMaTWljcm9zb2Z0IENvZGUgU2lnbmluZyBQQ0ECCmEZ
# zJMAAQAAAGYwCQYFKw4DAhoFAKCCAQEwGQYJKoZIhvcNAQkDMQwGCisGAQQBgjcC
# AQQwHAYKKwYBBAGCNwIBCzEOMAwGCisGAQQBgjcCARUwIwYJKoZIhvcNAQkEMRYE
# FOXzv+iZzbtiL1rUqXLZoeHz9t/bMIGgBgorBgEEAYI3AgEMMYGRMIGOoHaAdABD
# AFEAUgBTACAASgBvAHUAcgBuAGUAeQAgAFIAZQBmAGUAcgBlAG4AYwBlACAASQBt
# AHAAbABlAG0AZQBuAHQAYQB0AGkAbwBuACAAaQBuAHMAdABhAGwAbABhAHQAaQBv
# AG4AIABzAGMAcgBpAHAAdABzoRSAEmh0dHA6Ly9ha2EubXMvY3FyczANBgkqhkiG
# 9w0BAQEFAASCAQAWDxEADLK8WAZ1laGtP9GUlnQrQfPnqN1rc/UkUTsycrAczSJW
# uUe/mC2AwTBtXlosn1o93dysKblDeKtwbLgHAR4Katd0MrHhsrdZ++6jFGu/Rk29
# 5ivWwlgfaPoVyxCAGp3DbxuYxsDqCW0m1tLqyGNUrH8CSj98N69CZNS7LF2X1ixS
# Jfgb2g1ldfuiVvyOWUkeuR/kF7XD33sz3hbU1NtbTM8uAk1X/PwqxbIBE6Kly0bp
# 4OGkJdy09gIo7YjxtYIz0m2ZI6IeJQKJA/E0qgOgf/1O6S26mhH3NANqiMXo+VSq
# bRpzf7IqXsHO3ixlScx57n/VK+AvSPBPDLgaoYICHTCCAhkGCSqGSIb3DQEJBjGC
# AgowggIGAgEBMIGFMHcxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9u
# MRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp
# b24xITAfBgNVBAMTGE1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQQIKYQUTNgAAAAAA
# GjAHBgUrDgMCGqBdMBgGCSqGSIb3DQEJAzELBgkqhkiG9w0BBwEwHAYJKoZIhvcN
# AQkFMQ8XDTEyMDcyNTE2NDcwNlowIwYJKoZIhvcNAQkEMRYEFDw9/nFZtfZV7BHR
# GYJH/a/uIqwSMA0GCSqGSIb3DQEBBQUABIIBAH5VPbcmh1LenSTrMbgur2+gtVIb
# 8YwFyEGlOfg8bQe8z883dcHhi/D6afBm22ge0bOz+tVkN7BJfbxX93J+s+6GBCaO
# 3j2CAYqmtNjlTC077PRGYnVwh0XxCE23jZW3VhR41afIekrpAHSI43JPq6Kd+15R
# cbVLPVCtydXAK4Dh+qrHEVaOPlas40TXNgTsOuGbz98Rv1vkY4D7j2uYq6K22AZP
# s3gUANV3j0suwlGH60/YYqOfnGDZi+0i8+QI6njUx0Z1nQwBvWjQiF1LIVRRLNc9
# 4i2at7zi5bCszhuYgBBBoBAUtXmICLbCebc8sCKLHZNbv7CW3kPO4N5LBE0=
# SIG # End signature block

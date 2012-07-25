# ==============================================================================================================
# Microsoft patterns & practices
# CQRS Journey project
# ==============================================================================================================
# ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
# http://go.microsoft.com/fwlink/p/?LinkID=258575
# Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
# with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software distributed under the License is 
# distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
# See the License for the specific language governing permissions and limitations under the License.
# ==============================================================================================================

param (
    [switch] $autoAcceptTerms
)

# list all the solution folders where the "packages" folder will be placed.
$solutionRelativePaths = @('source', 'source\Conference.AcceptanceTests')


$scriptPath = Split-Path (Get-Variable MyInvocation -Scope 0).Value.MyCommand.Path 

$solutionFolders = New-Object object[] $solutionRelativePaths.Length
$allPackagesFiles = New-Object object[] $solutionRelativePaths.Length
for($i=0; $i -lt $solutionRelativePaths.Length; $i++)
{
    $solutionFolder = Join-Path $scriptPath $solutionRelativePaths[$i]
    $solutionFolders[$i] = $solutionFolder
    $allPackagesFiles[$i] = Get-ChildItem $solutionFolder -Include "packages.config" -Recurse
}


# get all the packages to install
$packages = @()
foreach ($packageFilesForSolution in $allPackagesFiles)
{
    $packageFilesForSolution | ForEach-Object { 
        $xml = New-Object "System.Xml.XmlDocument"
        $xml.Load($_.FullName)
        $xml | Select-Xml -XPath '//packages/package' | 
            Foreach { $packages += " - "+ $_.Node.id + " v" + $_.Node.version }
    }
}

$packages = $packages | Select -uniq | Sort-Object
$packages = [system.string]::Join("`r`n", $packages)

# prompt to continue
$caption = "DOWNLOADING NUGET PACKAGE DEPENDENCIES";
$packageInformation = "You are about to automatically download the following NuGet package dependencies required to build the sample application:
" + $packages + "
 
Microsoft grants you no rights for third party software.  You are responsible for and must locate and read the license terms for each of the above packages. The owners of the above packages are solely responsible for their content and behavior. Microsoft gives no express warranties, guarantees or conditions.
";

if ($autoAcceptTerms)
{
    Write-Host $caption
    Write-Host $packageInformation
}
else
{
    $message = $packageInformation + "Do you want to proceed?";

    $yes = New-Object System.Management.Automation.Host.ChoiceDescription "&Yes","I agree to download the NuGet packages dependencies.";
    $no = New-Object System.Management.Automation.Host.ChoiceDescription "&No","I do not agree to download the NuGet packages dependencies.";
    $choices = [System.Management.Automation.Host.ChoiceDescription[]]($yes,$no);
    $answer = $host.ui.PromptForChoice($caption,$message,$choices,1) 

    switch ($answer){
        0 { break }
        1 { exit; break }
    } 
}

# copy NuGet.exe bootstrapper to a temp folder if it's not there (this is to avoid distributing the full version of NuGet, and avoiding source control issues with updates).
$nuget = Join-Path $scriptPath 'build\temp\NuGet.exe'
$nugetExists = Test-Path $nuget

if ($nugetExists -eq 0)
{
	$tempFolder = Join-Path $scriptPath 'build\temp\'
	mkdir $tempFolder -Force > $null
	$nugetOriginal = Join-Path $scriptPath 'build\NuGet.exe'
	Copy-Item $nugetOriginal -Destination $nuget -Force
}

$env:EnableNuGetPackageRestore=$true

for($i=0; $i -lt $solutionFolders.Length; $i++)
{
    pushd $solutionFolders[$i]

    # install the packages
    $allPackagesFiles[$i] | ForEach-Object { & $nuget install $_.FullName -o packages }

    popd
}

# SIG # Begin signature block
# MIIamAYJKoZIhvcNAQcCoIIaiTCCGoUCAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUerNJvXS4etIUl4/bLZjjMye4
# QsygghUtMIIEoDCCA4igAwIBAgIKYRnMkwABAAAAZjANBgkqhkiG9w0BAQUFADB5
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
# A6KgAwIBAgIKYQUZlgAAAAAAGzANBgkqhkiG9w0BAQUFADB3MQswCQYDVQQGEwJV
# UzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UE
# ChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSEwHwYDVQQDExhNaWNyb3NvZnQgVGlt
# ZS1TdGFtcCBQQ0EwHhcNMTEwNzI1MjA0MjE5WhcNMTIxMDI1MjA0MjE5WjCBszEL
# MAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1v
# bmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjENMAsGA1UECxMETU9Q
# UjEnMCUGA1UECxMebkNpcGhlciBEU0UgRVNOOjlFNzgtODY0Qi0wMzlEMSUwIwYD
# VQQDExxNaWNyb3NvZnQgVGltZS1TdGFtcCBTZXJ2aWNlMIIBIjANBgkqhkiG9w0B
# AQEFAAOCAQ8AMIIBCgKCAQEA08s7U6KfRKN6q01WcVOKd6o3k34BPv2rAqNTqf/R
# sSLFAJDndW7uGOiBDhPF2GEAvh+gdjsEDQTFBKCo/ENTBqEEBLkLkpgCYjjv1DMS
# 9ys9e++tRVeFlSCf12M0nGJGjr6u4NmeOfapVf3P53fmNRPvXOi/SJNPGkMHWDiK
# f4UUbOrJ0Et6gm7L0xVgCBSJlKhbPzrJPyB9bS9YGn3Kiji8w8I5aNgtWBoj7SoQ
# CFogjIKl7dGXRZKFzMM3g98NmHzF07bgmVPYeAj15SMhB2KGWmppGf1w+VM0gfcl
# MRmGh4vAVZr9qkw1Ff1b6ZXJq1OYKV8speElD2TF8rAndQIDAQABo4IBCTCCAQUw
# HQYDVR0OBBYEFHkj56ENvlUsaBgpYoJn1vPhNjhaMB8GA1UdIwQYMBaAFCM0+NlS
# RnAK7UD7dvuzK7DDNbMPMFQGA1UdHwRNMEswSaBHoEWGQ2h0dHA6Ly9jcmwubWlj
# cm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY3Jvc29mdFRpbWVTdGFtcFBD
# QS5jcmwwWAYIKwYBBQUHAQEETDBKMEgGCCsGAQUFBzAChjxodHRwOi8vd3d3Lm1p
# Y3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY3Jvc29mdFRpbWVTdGFtcFBDQS5jcnQw
# EwYDVR0lBAwwCgYIKwYBBQUHAwgwDQYJKoZIhvcNAQEFBQADggEBAEfCdoFbMd1v
# 0zyZ8npsfpcTUCwFFxsQuEShtYz0Vs+9sCG0ZG1hHNju6Ov1ku5DohhEw/r67622
# XH+XbUu1Q/snYXgIVHyx+a+YCrR0xKroLVDEff59TqGZ1icot67Y37GPgyKOzvN5
# /GEUbb/rzISw36O7WwW36lT1Yh1sJ6ZjS/rjofq734WWZWlTsLZxmGQmZr3F8Vxi
# vJH0PZxLQgANzzgFFCZa3CoFS39qmTjY3XOZos6MUCSepOv1P4p4zFSZXSVmpEEG
# KK9JxLRSlOzeAoNk/k3U/0ui/CmA2+4/qzztM4jKvyJg0Fw7BLAKtJhtPKc6T5rR
# ARYRYopBdqAwggW8MIIDpKADAgECAgphMyYaAAAAAAAxMA0GCSqGSIb3DQEBBQUA
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
# FAm5DhTSv66xk+7u/NK0T3WWG36NMIGgBgorBgEEAYI3AgEMMYGRMIGOoHaAdABD
# AFEAUgBTACAASgBvAHUAcgBuAGUAeQAgAFIAZQBmAGUAcgBlAG4AYwBlACAASQBt
# AHAAbABlAG0AZQBuAHQAYQB0AGkAbwBuACAAaQBuAHMAdABhAGwAbABhAHQAaQBv
# AG4AIABzAGMAcgBpAHAAdABzoRSAEmh0dHA6Ly9ha2EubXMvY3FyczANBgkqhkiG
# 9w0BAQEFAASCAQCKyZm8HArQgLSkdtPFTlfuxOfwhY6vwJX3pcWAgRAx0T6KoEXa
# aQCeZSUkU0UvE8NlOfQTU85R8fWJMOjHFbDFlqV/dsi+gJYHgEP6454uqbhHPQpr
# 5T944T4+RTcQuaZ9J8esCJ+IrcFCyOxC26tBP1ro/rVqLdtaKToFeeVoVyB8pCF5
# Eas5tYe5uAnDl3/sU9K4125czUxWXwH6oLy+5nsVYz3kSKzob5tA78AJYOtpiTg6
# 8U/bH8DWdXE23aYLh7eaZgY5dK/wOCIJMyOmNIFzEQgtiLXN6ixQcIqDSCi2hWxh
# tMXk0mRMiwPOuVolL0XOQWB6SPR5W3elkFLJoYICHTCCAhkGCSqGSIb3DQEJBjGC
# AgowggIGAgEBMIGFMHcxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9u
# MRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp
# b24xITAfBgNVBAMTGE1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQQIKYQUZlgAAAAAA
# GzAHBgUrDgMCGqBdMBgGCSqGSIb3DQEJAzELBgkqhkiG9w0BBwEwHAYJKoZIhvcN
# AQkFMQ8XDTEyMDcyNTE2NDcwNlowIwYJKoZIhvcNAQkEMRYEFJUscDSCECfvFSUd
# K3N2iq7yyftyMA0GCSqGSIb3DQEBBQUABIIBAHGgFAZ9pTcowe4e8vrTSD1n5Eeg
# GMJ+LMJsTAwKX/AbQk6f02X1WraaJHgLc7LONkbdsK99lgLT+Zk9sz9nIuyAb2nz
# lGjKyMH0dnIQNf5YhIzi+P+hus8iguZE4dmMqgv6+JCG/TrEc9H2JTrJuH4Ii8t+
# U6PsyfGv6vWFz3iTTYzVo64ungBHj1xN2d4rNkGMhfYJ/TJpgG4pNAzjsMo1sqnr
# 6aolPdlrsvkwewITgKA51OQmKEQTauwus2xB+o/5ZFjAuslmsjf1CVw5snhVMyGt
# fiVLiVpP9hZrCdnieqvJRV+FWZ9gxci2zXDd8A8Ke3gdKkgfYjAN96CBdZg=
# SIG # End signature block

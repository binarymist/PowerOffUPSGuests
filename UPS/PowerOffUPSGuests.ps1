Set-StrictMode -Version 2.0

# add the assembly that does the work.
Add-Type -Path C:\Scripts\UPS\BinaryMist.PowerOffUPSGuests.dll

# instantiate an Initiator instance
$powerOffUPSGuestsInstance = New-Object -TypeName BinaryMist.PowerOffUPSGuests.Initiator

Write-Host $powerOffUPSGuestsInstance.InitShutdownOfServers() -ForegroundColor Green

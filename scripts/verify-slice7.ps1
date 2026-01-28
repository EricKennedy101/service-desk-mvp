$ErrorActionPreference = "Stop"

$baseUrl = "http://localhost:5249"
$analystEmail = "analyst@fra.local"
$analystPassword = "analyst123"
$leadEmail = "lead@fra.local"
$leadPassword = "lead123"

function Write-Pass($message) { Write-Host "PASS: $message" -ForegroundColor Green }
function Write-Fail($message) { Write-Host "FAIL: $message" -ForegroundColor Red }

function Invoke-Json {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter()][hashtable]$Headers,
        [Parameter()][object]$Body
    )

    $params = @{
        Method = $Method
        Uri = $Uri
        Headers = $Headers
        ContentType = "application/json"
    }

    if ($null -ne $Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 5)
    }

    return Invoke-RestMethod @params
}

try {
    Write-Host "=== Slice 7 Verification ==="

    $loginAnalyst = Invoke-Json -Method "Post" -Uri "$baseUrl/api/Auth/login" -Body @{
        email = $analystEmail
        password = $analystPassword
    }
    if (-not $loginAnalyst.token) { throw "Analyst login did not return token." }
    Write-Pass "Analyst login"

    $analystHeaders = @{ Authorization = "Bearer $($loginAnalyst.token)" }

    $newCase = Invoke-Json -Method "Post" -Uri "$baseUrl/api/Cases" -Headers $analystHeaders -Body @{
        title = "Slice7 Verify"
        description = "Soft delete retention test"
    }
    if (-not $newCase.id) { throw "Create case did not return id." }
    $caseId = [int]$newCase.id
    Write-Pass "Create case"

    $eventsInitial = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/Cases/$caseId/events" -Headers $analystHeaders
    if (-not $eventsInitial) { throw "Events list is empty after create." }
    Write-Pass "Fetch events after create"

    $loginLead = Invoke-Json -Method "Post" -Uri "$baseUrl/api/Auth/login" -Body @{
        email = $leadEmail
        password = $leadPassword
    }
    if (-not $loginLead.token) { throw "Lead login did not return token." }
    Write-Pass "Lead login"

    $leadHeaders = @{ Authorization = "Bearer $($loginLead.token)" }

    $deleteResponse = Invoke-WebRequest -Method Delete -Uri "$baseUrl/api/Cases/$caseId" -Headers $leadHeaders -UseBasicParsing
    if ($deleteResponse.StatusCode -ne 204) { throw "Delete expected 204, got $($deleteResponse.StatusCode)." }
    Write-Pass "Delete case (soft delete)"

    $casesDefault = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/Cases" -Headers $leadHeaders
    $defaultHasCase = $casesDefault | Where-Object { $_.id -eq $caseId }
    if ($defaultHasCase) { throw "Case should not appear in default list." }
    Write-Pass "Default list excludes deleted case"

    $casesIncludeDeleted = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/Cases?includeDeleted=true" -Headers $leadHeaders
    $includedCase = $casesIncludeDeleted | Where-Object { $_.id -eq $caseId }
    if (-not $includedCase) { throw "Case should appear in includeDeleted list." }
    if (-not $includedCase.isDeleted) { throw "Case isDeleted should be true in includeDeleted list." }
    Write-Pass "includeDeleted list includes soft-deleted case"

    $eventsAfterDelete = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/Cases/$caseId/events" -Headers $leadHeaders
    $deletedEvent = $eventsAfterDelete | Where-Object {
        ($_.eventType -eq "Deleted" -or $_.eventType -like "*Deleted*") -and $_.actorEmail -eq $leadEmail
    }
    if (-not $deletedEvent) { throw "Deleted event with actorEmail=$leadEmail not found." }
    Write-Pass "Deleted event present with actorEmail"

    Write-Host "=== All checks passed ==="
    exit 0
}
catch {
    Write-Fail $_.Exception.Message
    exit 1
}

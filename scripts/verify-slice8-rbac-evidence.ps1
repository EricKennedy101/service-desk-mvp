$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

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

function Invoke-UploadFile {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string]$BearerToken
    )

    function Get-ContentType([string]$Path) {
        switch ([System.IO.Path]::GetExtension($Path).ToLowerInvariant()) {
            ".txt" { return "text/plain" }
            ".log" { return "text/plain" }
            ".csv" { return "text/csv" }
            ".json" { return "application/json" }
            ".png" { return "image/png" }
            ".jpg" { return "image/jpeg" }
            ".jpeg" { return "image/jpeg" }
            ".pdf" { return "application/pdf" }
            ".docx" { return "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
            ".xlsx" { return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
            ".pptx" { return "application/vnd.openxmlformats-officedocument.presentationml.presentation" }
            ".zip" { return "application/zip" }
            default { return "application/octet-stream" }
        }
    }

    $client = New-Object System.Net.Http.HttpClient
    $client.DefaultRequestHeaders.Authorization =
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $BearerToken)

    $content = New-Object System.Net.Http.MultipartFormDataContent
    $fileStream = [System.IO.File]::OpenRead($FilePath)
    $fileContent = New-Object System.Net.Http.StreamContent($fileStream)
    $fileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue((Get-ContentType $FilePath))
    $fileName = [System.IO.Path]::GetFileName($FilePath)
    $content.Add($fileContent, "file", $fileName)

    $response = $client.PostAsync($Uri, $content).Result
    $body = $response.Content.ReadAsStringAsync().Result

    $fileStream.Dispose()
    $client.Dispose()

    return @{
        StatusCode = [int]$response.StatusCode
        Body = $body
    }
}

try {
    Write-Host "=== Slice 8 RBAC + Evidence Verification ==="

    $loginAnalyst = Invoke-Json -Method "Post" -Uri "$baseUrl/api/Auth/login" -Body @{
        email = $analystEmail
        password = $analystPassword
    }
    if (-not $loginAnalyst.token) { throw "Analyst login did not return token." }
    Write-Pass "A) Analyst login"

    $analystHeaders = @{ Authorization = "Bearer $($loginAnalyst.token)" }

    $case1 = Invoke-Json -Method "Post" -Uri "$baseUrl/api/Cases" -Headers $analystHeaders -Body @{
        title = "Slice8 RBAC Case"
        description = "RBAC delete check"
    }
    if (-not $case1.id) { throw "Create case did not return id." }
    $caseId = [int]$case1.id
    Write-Pass "B) Analyst created case"

    $analystDeleteStatus = $null
    try {
        $analystDelete = Invoke-WebRequest -Method Delete -Uri "$baseUrl/api/Cases/$caseId" -Headers $analystHeaders -UseBasicParsing
        $analystDeleteStatus = $analystDelete.StatusCode
    }
    catch {
        if ($_.Exception.Response) {
            $analystDeleteStatus = $_.Exception.Response.StatusCode.value__
        } else {
            throw
        }
    }
    if ($analystDeleteStatus -ne 401 -and $analystDeleteStatus -ne 403) {
        throw "Analyst delete expected 401/403, got $analystDeleteStatus."
    }
    Write-Pass "C) Analyst delete blocked (status $analystDeleteStatus)"

    $loginLead = Invoke-Json -Method "Post" -Uri "$baseUrl/api/Auth/login" -Body @{
        email = $leadEmail
        password = $leadPassword
    }
    if (-not $loginLead.token) { throw "Lead login did not return token." }
    Write-Pass "D) Lead login"

    $leadHeaders = @{ Authorization = "Bearer $($loginLead.token)" }

    $leadDelete = Invoke-WebRequest -Method Delete -Uri "$baseUrl/api/Cases/$caseId" -Headers $leadHeaders -UseBasicParsing
    if ($leadDelete.StatusCode -ne 204) { throw "Lead delete expected 204, got $($leadDelete.StatusCode)." }
    Write-Pass "E) Lead delete allowed"

    $case2 = Invoke-Json -Method "Post" -Uri "$baseUrl/api/Cases" -Headers $analystHeaders -Body @{
        title = "Slice8 Evidence Case"
        description = "Evidence validation"
    }
    if (-not $case2.id) { throw "Create case for evidence did not return id." }
    $evidenceCaseId = [int]$case2.id
    Write-Pass "F) Analyst created case for evidence"

    $txtPath = Join-Path $env:TEMP "evidence_test.txt"
    Set-Content -Path $txtPath -Value "evidence test"

    $uploadOk = Invoke-UploadFile -Uri "$baseUrl/api/Cases/$evidenceCaseId/evidence" -FilePath $txtPath -BearerToken $loginAnalyst.token
    if ($uploadOk.StatusCode -ne 200) { throw "Evidence upload expected 200, got $($uploadOk.StatusCode). Response: $($uploadOk.Body)" }
    $uploadOkJson = $uploadOk.Body | ConvertFrom-Json
    if (-not $uploadOkJson.id) { throw "Evidence upload did not return id. Response: $($uploadOk.Body)" }
    $evidenceId = [int]$uploadOkJson.id
    Write-Pass "F) Evidence upload allowed (.txt)"

    $exePath = Join-Path $env:TEMP "malware.exe"
    Set-Content -Path $exePath -Value "not really"
    $uploadBad = Invoke-UploadFile -Uri "$baseUrl/api/Cases/$evidenceCaseId/evidence" -FilePath $exePath -BearerToken $loginAnalyst.token
    if ($uploadBad.StatusCode -ne 400) { throw "Evidence upload (.exe) expected 400, got $($uploadBad.StatusCode). Response: $($uploadBad.Body)" }
    if ($uploadBad.Body -notmatch "Allowed extensions") { throw "Expected allowed extensions list in error response. Response: $($uploadBad.Body)" }
    Write-Pass "G) Evidence upload blocked (.exe)"

    $evidenceList = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/Cases/$evidenceCaseId/evidence" -Headers $analystHeaders
    $foundEvidence = $evidenceList | Where-Object { $_.id -eq $evidenceId }
    if (-not $foundEvidence) { throw "Evidence list does not include uploaded evidence. Response: $($evidenceList | ConvertTo-Json -Depth 5)" }
    Write-Pass "H) Evidence list includes uploaded file"

    $downloadPath = Join-Path $env:TEMP "evidence_download_$evidenceId.txt"
    $downloadStatus = $null
    $downloadBody = $null
    try {
        $download = Invoke-WebRequest -Method Get -Uri "$baseUrl/api/Cases/$evidenceCaseId/evidence/$evidenceId" -Headers $analystHeaders -UseBasicParsing -OutFile $downloadPath -PassThru
        $downloadStatus = $download.StatusCode
    }
    catch {
        if ($_.Exception.Response) {
            $downloadStatus = $_.Exception.Response.StatusCode.value__
            $stream = $_.Exception.Response.GetResponseStream()
            if ($stream) {
                $reader = New-Object System.IO.StreamReader($stream)
                $downloadBody = $reader.ReadToEnd()
                $reader.Dispose()
            }
        } else {
            throw
        }
    }
    if (-not $downloadStatus) {
        if (Test-Path $downloadPath) { $downloadStatus = 200 }
    }
    if ($downloadStatus -ne 200) { throw "Evidence download expected 200, got $downloadStatus. Response: $downloadBody" }
    if (-not (Test-Path $downloadPath)) { throw "Evidence download did not create file at $downloadPath." }
    if ((Get-Item $downloadPath).Length -le 0) { throw "Evidence download returned empty file." }
    Write-Pass "I) Evidence download returns content"

    $events = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/Cases/$evidenceCaseId/events" -Headers $analystHeaders
    $evidenceEvent = $events | Where-Object { $_.eventType -eq "EvidenceUploaded" -and $_.actorEmail -eq $analystEmail }
    if (-not $evidenceEvent) { throw "EvidenceUploaded event with actorEmail=$analystEmail not found. Response: $($events | ConvertTo-Json -Depth 5)" }
    Write-Pass "J) EvidenceUploaded event present"

    Write-Host "=== All checks passed ==="
    exit 0
}
catch {
    Write-Fail $_.Exception.Message
    exit 1
}
finally {
    if ($txtPath -and (Test-Path $txtPath)) { Remove-Item -Path $txtPath -ErrorAction SilentlyContinue }
    if ($exePath -and (Test-Path $exePath)) { Remove-Item -Path $exePath -ErrorAction SilentlyContinue }
    if ($downloadPath -and (Test-Path $downloadPath)) { Remove-Item -Path $downloadPath -ErrorAction SilentlyContinue }
}

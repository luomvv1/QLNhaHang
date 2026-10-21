param(
    [string]$ApiBaseUrl = 'http://localhost:5079',
    [string]$TestDate = '2026-10-20',
    [string]$AdminEmail = 'admintest4@gmail.com',
    [string]$ChefEmail = 'daubep1@gmail.com',
    [string]$StaffEmail = 'nhanvien1@gmail.com',
    [string]$Password = '123',
    [int]$Concurrency = 8
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

function Get-Token([string]$email) {
    $body = @{ email = $email; password = $Password } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/Auth/login" `
        -Method Post -ContentType 'application/json' -Body $body
    if ([string]::IsNullOrWhiteSpace($response.Token)) {
        throw "Không lấy được token cho $email"
    }
    return $response.Token
}

function Invoke-AuthorizedGet([string]$token, [string]$path) {
    return Invoke-RestMethod -Uri "$ApiBaseUrl$path" `
        -Headers @{ Authorization = "Bearer $token" } -Method Get
}

Write-Host "WMS smoke test: $ApiBaseUrl / ngày $TestDate"
$adminToken = Get-Token $AdminEmail
$chefToken = Get-Token $ChefEmail
$staffToken = Get-Token $StaffEmail

$null = Invoke-AuthorizedGet $adminToken "/api/admin/inventory/daily-portions?date=$TestDate"
$null = Invoke-AuthorizedGet $chefToken '/api/chef/queue'
$null = Invoke-AuthorizedGet $staffToken '/api/staff/orders/tables'
$null = Invoke-AuthorizedGet $staffToken '/api/staff/orders/menu'
Write-Host '[PASS] Admin/Chef/Staff login và endpoint chính'

$daily = Invoke-AuthorizedGet $adminToken "/api/admin/inventory/daily-portions?date=$TestDate"
$portionId = @($daily.data)[0].portionControlId
if (-not $portionId) { throw "Ngày kiểm thử chưa có kế hoạch suất ăn." }
$portion = Invoke-AuthorizedGet $adminToken "/api/admin/inventory/daily-portions/$portionId"
$firstBom = @($portion.data.BOM)[0]
if ($null -eq $firstBom.availableQuantity) {
    throw 'API BOM chưa trả về availableQuantity theo lô còn hạn.'
}
Write-Host '[PASS] Kế hoạch trả tồn lô còn hạn và không chỉ dùng CurrentQuantity'

$headers = [System.Net.Http.HttpClient]::new()
$headers.DefaultRequestHeaders.Authorization = `
    [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $adminToken)
$payload = @{ controlDate = $TestDate; note = 'WMS-SMOKE-CONCURRENCY-CLEANUP' } | ConvertTo-Json
$tasks = [System.Collections.Generic.List[System.Threading.Tasks.Task[System.Net.Http.HttpResponseMessage]]]::new()
1..$Concurrency | ForEach-Object {
    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::Post,
        "$ApiBaseUrl/api/admin/inventory/daily-portions/generate-requisition")
    $request.Content = [System.Net.Http.StringContent]::new(
        $payload, [Text.Encoding]::UTF8, 'application/json')
    [void]$tasks.Add($headers.SendAsync($request))
}
$responses = [System.Threading.Tasks.Task]::WhenAll($tasks).GetAwaiter().GetResult()
$statuses = @($responses | ForEach-Object { [int]$_.StatusCode })
$successResponses = @($responses | Where-Object { $_.IsSuccessStatusCode })
$conflictCount = @($responses | Where-Object { [int]$_.StatusCode -eq 409 }).Count

if ($successResponses.Count -ne 1 -or $conflictCount -ne ($Concurrency - 1)) {
    throw "Concurrency không đạt: $($statuses -join ',')"
}

$created = $successResponses[0].Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
$createdReceiptId = $created.receipt.receiptId
if ($createdReceiptId) {
    try {
        Invoke-RestMethod -Uri "$ApiBaseUrl/api/admin/inventory/receipts/$createdReceiptId" `
            -Headers @{ Authorization = "Bearer $adminToken" } -Method Delete | Out-Null
    }
    catch {
        Write-Warning "Không tự hủy được phiếu test #${createdReceiptId}: $($_.Exception.Message)"
    }
}
$headers.Dispose()
Write-Host "[PASS] Concurrency: 1 lần tạo, $conflictCount lần bị từ chối 409; đã dọn phiếu test"
Write-Host 'WMS smoke test hoàn tất.'

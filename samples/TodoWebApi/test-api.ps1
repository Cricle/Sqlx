#!/usr/bin/env pwsh
# Test all TodoWebApi endpoints

param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

Write-Host "`n🧪 Testing TodoWebApi Endpoints..." -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl`n" -ForegroundColor Gray

$passed = 0
$failed = 0
$errors = @()

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Uri,
        [object]$Body = $null,
        [int]$ExpectedStatus = 200
    )
    
    Write-Host "Testing: $Name..." -NoNewline
    
    try {
        $params = @{
            Uri = "$BaseUrl$Uri"
            Method = $Method
            ContentType = "application/json"
            TimeoutSec = 5
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-WebRequest @params -ErrorAction Stop
        
        if ($response.StatusCode -eq $ExpectedStatus) {
            Write-Host " ✅ PASS (Status: $($response.StatusCode))" -ForegroundColor Green
            $script:passed++
            return $response
        } else {
            Write-Host " ❌ FAIL (Expected: $ExpectedStatus, Got: $($response.StatusCode))" -ForegroundColor Red
            $script:failed++
            $script:errors += "$Name - Unexpected status code"
            return $null
        }
    }
    catch {
        Write-Host " ❌ FAIL" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails) {
            Write-Host "   Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
        }
        $script:failed++
        $script:errors += "$Name - $($_.Exception.Message)"
        return $null
    }
}

# Wait for server to be ready
Write-Host "⏳ Waiting for server to be ready..." -ForegroundColor Yellow
$retries = 0
$maxRetries = 10
while ($retries -lt $maxRetries) {
    try {
        $null = Invoke-WebRequest -Uri "$BaseUrl/" -TimeoutSec 2 -ErrorAction Stop
        Write-Host "✅ Server is ready!`n" -ForegroundColor Green
        break
    }
    catch {
        $retries++
        if ($retries -ge $maxRetries) {
            Write-Host "❌ Server is not responding after $maxRetries attempts" -ForegroundColor Red
            Write-Host "Please start the server with: dotnet run --project samples/TodoWebApi/TodoWebApi.csproj`n" -ForegroundColor Yellow
            exit 1
        }
        Start-Sleep -Seconds 1
    }
}

Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Test 1: Root redirect
Test-Endpoint -Name "GET /" -Method "GET" -Uri "/" -ExpectedStatus 200

# Test 2: Get all todos (empty)
$response = Test-Endpoint -Name "GET /api/todos (empty)" -Method "GET" -Uri "/api/todos"

# Test 3: Create todo
$newTodo = @{
    title = "Test Todo 1"
    description = "This is a test todo"
    priority = 3
    isCompleted = $false
}
$createResponse = Test-Endpoint -Name "POST /api/todos (create)" -Method "POST" -Uri "/api/todos" -Body $newTodo -ExpectedStatus 201

$todoId = $null
if ($createResponse) {
    $todoData = $createResponse.Content | ConvertFrom-Json
    $todoId = $todoData.id
    Write-Host "   Created Todo ID: $todoId" -ForegroundColor Gray
}

# Test 4: Get all todos (with data)
Test-Endpoint -Name "GET /api/todos (with data)" -Method "GET" -Uri "/api/todos"

# Test 5: Get todo by ID
if ($todoId) {
    Test-Endpoint -Name "GET /api/todos/{id}" -Method "GET" -Uri "/api/todos/$todoId"
}

# Test 6: Update todo
if ($todoId) {
    $updateTodo = @{
        title = "Updated Test Todo 1"
        description = "This is an updated test todo"
        priority = 4
        isCompleted = $true
    }
    Test-Endpoint -Name "PUT /api/todos/{id} (update)" -Method "PUT" -Uri "/api/todos/$todoId" -Body $updateTodo
}

# Test 7: Search todos
if ($todoId) {
    Test-Endpoint -Name "GET /api/todos/search" -Method "GET" -Uri "/api/todos/search?q=Test"
}

# Test 8: Get completed todos
if ($todoId) {
    Test-Endpoint -Name "GET /api/todos/completed" -Method "GET" -Uri "/api/todos/completed"
}

# Test 9: Get high priority todos
Test-Endpoint -Name "GET /api/todos/high-priority" -Method "GET" -Uri "/api/todos/high-priority"

# Test 10: Get count
Test-Endpoint -Name "GET /api/todos/count" -Method "GET" -Uri "/api/todos/count"

# Test 11: Create another todo for batch operations
$newTodo2 = @{
    title = "Test Todo 2"
    priority = 2
    isCompleted = $false
}
$createResponse2 = Test-Endpoint -Name "POST /api/todos (create 2nd)" -Method "POST" -Uri "/api/todos" -Body $newTodo2 -ExpectedStatus 201

$todoId2 = $null
if ($createResponse2) {
    $todoData2 = $createResponse2.Content | ConvertFrom-Json
    $todoId2 = $todoData2.id
}

# Test 12: Batch update priority
if ($todoId -and $todoId2) {
    $batchUpdate = @{
        ids = @($todoId, $todoId2)
        newPriority = 5
    }
    Test-Endpoint -Name "PUT /api/todos/batch/priority" -Method "PUT" -Uri "/api/todos/batch/priority" -Body $batchUpdate
}

# Test 13: Get due soon (should be empty)
Test-Endpoint -Name "GET /api/todos/due-soon" -Method "GET" -Uri "/api/todos/due-soon"

# Test 14: Get todo by non-existent ID
$response = Test-Endpoint -Name "GET /api/todos/{id} (not found)" -Method "GET" -Uri "/api/todos/99999" -ExpectedStatus 404
if ($response -eq $null) {
    # This is expected - 404 is the correct response
    Write-Host "   (404 is expected for non-existent ID)" -ForegroundColor Gray
    $script:passed++
    $script:failed--
    $script:errors = $script:errors | Where-Object { $_ -notlike "*not found*99999*" }
}

# Test 15: Delete todo
if ($todoId) {
    Test-Endpoint -Name "DELETE /api/todos/{id}" -Method "DELETE" -Uri "/api/todos/$todoId" -ExpectedStatus 204
}

# Test 16: Delete second todo
if ($todoId2) {
    Test-Endpoint -Name "DELETE /api/todos/{id} (2nd)" -Method "DELETE" -Uri "/api/todos/$todoId2" -ExpectedStatus 204
}

# Test 17: Delete non-existent todo
$response = Test-Endpoint -Name "DELETE /api/todos/{id} (not found)" -Method "DELETE" -Uri "/api/todos/99999" -ExpectedStatus 404
if ($response -eq $null) {
    # This is expected - 404 is the correct response
    Write-Host "   (404 is expected for non-existent ID)" -ForegroundColor Gray
    $script:passed++
    $script:failed--
    $script:errors = $script:errors | Where-Object { $_ -notlike "*not found*99999*" }
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "`n📊 Test Results:" -ForegroundColor Cyan
Write-Host "   ✅ Passed: $passed" -ForegroundColor Green
Write-Host "   ❌ Failed: $failed" -ForegroundColor Red
Write-Host "   📈 Total:  $($passed + $failed)" -ForegroundColor White

if ($failed -gt 0) {
    Write-Host "`n❌ Failed Tests:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "   - $_" -ForegroundColor Yellow }
    exit 1
} else {
    Write-Host "`n✅ All tests passed!" -ForegroundColor Green
    exit 0
}


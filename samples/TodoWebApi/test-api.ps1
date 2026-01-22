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
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host " ✅ PASS (Status: $statusCode)" -ForegroundColor Green
            $script:passed++
            return $null
        }
        
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
Write-Host "📋 BASIC CRUD OPERATIONS" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Test 1: Root redirect
Test-Endpoint -Name "GET /" -Method "GET" -Uri "/" -ExpectedStatus 200

# Test 2: Get all todos (empty)
Test-Endpoint -Name "GET /api/todos (empty)" -Method "GET" -Uri "/api/todos"

# Test 3: Create todo 1
$newTodo1 = @{
    title = "High Priority Task"
    description = "This is a high priority task"
    priority = 4
    isCompleted = $false
    estimatedMinutes = 120
    dueDate = (Get-Date).AddDays(2).ToString("o")
}
$createResponse1 = Test-Endpoint -Name "POST /api/todos (create #1)" -Method "POST" -Uri "/api/todos" -Body $newTodo1 -ExpectedStatus 201

$todoId1 = $null
if ($createResponse1) {
    $todoData = $createResponse1.Content | ConvertFrom-Json
    $todoId1 = $todoData.id
    Write-Host "   Created Todo ID: $todoId1" -ForegroundColor Gray
}

# Test 4: Create todo 2
$newTodo2 = @{
    title = "Medium Priority Task"
    description = "This is a medium priority task"
    priority = 2
    isCompleted = $false
    estimatedMinutes = 60
}
$createResponse2 = Test-Endpoint -Name "POST /api/todos (create #2)" -Method "POST" -Uri "/api/todos" -Body $newTodo2 -ExpectedStatus 201

$todoId2 = $null
if ($createResponse2) {
    $todoData2 = $createResponse2.Content | ConvertFrom-Json
    $todoId2 = $todoData2.id
    Write-Host "   Created Todo ID: $todoId2" -ForegroundColor Gray
}

# Test 5: Create todo 3 (low priority)
$newTodo3 = @{
    title = "Low Priority Task"
    priority = 1
    isCompleted = $false
}
$createResponse3 = Test-Endpoint -Name "POST /api/todos (create #3)" -Method "POST" -Uri "/api/todos" -Body $newTodo3 -ExpectedStatus 201

$todoId3 = $null
if ($createResponse3) {
    $todoData3 = $createResponse3.Content | ConvertFrom-Json
    $todoId3 = $todoData3.id
    Write-Host "   Created Todo ID: $todoId3" -ForegroundColor Gray
}

# Test 6: Create todo 4 (overdue)
$newTodo4 = @{
    title = "Overdue Task"
    priority = 3
    isCompleted = $false
    dueDate = (Get-Date).AddDays(-2).ToString("o")
}
$createResponse4 = Test-Endpoint -Name "POST /api/todos (create #4 overdue)" -Method "POST" -Uri "/api/todos" -Body $newTodo4 -ExpectedStatus 201

$todoId4 = $null
if ($createResponse4) {
    $todoData4 = $createResponse4.Content | ConvertFrom-Json
    $todoId4 = $todoData4.id
    Write-Host "   Created Todo ID: $todoId4" -ForegroundColor Gray
}

# Test 7: Get all todos (with data)
Test-Endpoint -Name "GET /api/todos (with data)" -Method "GET" -Uri "/api/todos"

# Test 8: Get todo by ID
if ($todoId1) {
    Test-Endpoint -Name "GET /api/todos/{id}" -Method "GET" -Uri "/api/todos/$todoId1"
}

# Test 9: Update todo
if ($todoId1) {
    $updateTodo = @{
        title = "Updated High Priority Task"
        description = "This task has been updated"
        priority = 5
        isCompleted = $false
    }
    Test-Endpoint -Name "PUT /api/todos/{id} (update)" -Method "PUT" -Uri "/api/todos/$todoId1" -Body $updateTodo
}

# Test 10: Delete todo by ID (will delete todoId3)
if ($todoId3) {
    Test-Endpoint -Name "DELETE /api/todos/{id}" -Method "DELETE" -Uri "/api/todos/$todoId3" -ExpectedStatus 204
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 QUERY & FILTER OPERATIONS" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Test 11: Search todos
Test-Endpoint -Name "GET /api/todos/search" -Method "GET" -Uri "/api/todos/search?q=Priority"

# Test 12: Get completed todos (should be empty)
Test-Endpoint -Name "GET /api/todos/completed" -Method "GET" -Uri "/api/todos/completed"

# Test 13: Get high priority todos
Test-Endpoint -Name "GET /api/todos/high-priority" -Method "GET" -Uri "/api/todos/high-priority"

# Test 14: Get todos by specific priority
Test-Endpoint -Name "GET /api/todos/priority/2" -Method "GET" -Uri "/api/todos/priority/2"

# Test 15: Get overdue todos
Test-Endpoint -Name "GET /api/todos/overdue" -Method "GET" -Uri "/api/todos/overdue"

# Test 16: Get todos due soon
Test-Endpoint -Name "GET /api/todos/due-soon" -Method "GET" -Uri "/api/todos/due-soon"

# Test 17: Get todos with pagination
Test-Endpoint -Name "GET /api/todos/paged" -Method "GET" -Uri "/api/todos/paged?page=1&pageSize=2"

# Test 18: Check if todo exists
if ($todoId1) {
    Test-Endpoint -Name "GET /api/todos/{id}/exists (true)" -Method "GET" -Uri "/api/todos/$todoId1/exists"
}

# Test 19: Check if non-existent todo exists
Test-Endpoint -Name "GET /api/todos/99999/exists (false)" -Method "GET" -Uri "/api/todos/99999/exists"

# Test 20: Get todos by IDs
if ($todoId1 -and $todoId2) {
    $batchGet = @{
        ids = @($todoId1, $todoId2)
    }
    Test-Endpoint -Name "POST /api/todos/by-ids" -Method "POST" -Uri "/api/todos/by-ids" -Body $batchGet
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 COUNT & STATISTICS" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Test 21: Get total count
Test-Endpoint -Name "GET /api/todos/count" -Method "GET" -Uri "/api/todos/count"

# Test 22: Get pending count
Test-Endpoint -Name "GET /api/todos/count/pending" -Method "GET" -Uri "/api/todos/count/pending"

# Test 23: LINQ count overdue
Test-Endpoint -Name "GET /api/todos/linq/count-overdue" -Method "GET" -Uri "/api/todos/linq/count-overdue"

# Test 24: Get statistics
Test-Endpoint -Name "GET /api/todos/queryable/stats" -Method "GET" -Uri "/api/todos/queryable/stats"

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔄 BATCH OPERATIONS" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Test 25: Batch update priority
if ($todoId1 -and $todoId2) {
    $batchPriority = @{
        ids = @($todoId1, $todoId2)
        newPriority = 3
    }
    Test-Endpoint -Name "PUT /api/todos/batch/priority" -Method "PUT" -Uri "/api/todos/batch/priority" -Body $batchPriority
}

# Test 26: Batch complete todos
if ($todoId2 -and $todoId4) {
    $batchComplete = @{
        ids = @($todoId2, $todoId4)
    }
    Test-Endpoint -Name "PUT /api/todos/batch/complete" -Method "PUT" -Uri "/api/todos/batch/complete" -Body $batchComplete
}

# Test 27: Mark single todo as completed
if ($todoId1) {
    Test-Endpoint -Name "PUT /api/todos/{id}/complete" -Method "PUT" -Uri "/api/todos/$todoId1/complete" -ExpectedStatus 204
}

# Test 28: Update actual minutes
if ($todoId1) {
    $updateMinutes = @{
        actualMinutes = 90
    }
    Test-Endpoint -Name "PUT /api/todos/{id}/actual-minutes" -Method "PUT" -Uri "/api/todos/$todoId1/actual-minutes" -Body $updateMinutes -ExpectedStatus 204
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🎯 LINQ & QUERYABLE EXAMPLES" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Test 29: LINQ high priority pending
Test-Endpoint -Name "GET /api/todos/linq/high-priority-pending" -Method "GET" -Uri "/api/todos/linq/high-priority-pending"

# Test 30: Queryable priority paged
Test-Endpoint -Name "GET /api/todos/queryable/priority-paged" -Method "GET" -Uri "/api/todos/queryable/priority-paged?page=1&pageSize=5"

# Test 31: Queryable titles
Test-Endpoint -Name "GET /api/todos/queryable/titles" -Method "GET" -Uri "/api/todos/queryable/titles"

# Test 32: Queryable search advanced
Test-Endpoint -Name "GET /api/todos/queryable/search-advanced" -Method "GET" -Uri "/api/todos/queryable/search-advanced?keyword=Task"

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🗑️  DELETE OPERATIONS" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Test 33: Delete all completed todos
Test-Endpoint -Name "DELETE /api/todos/completed" -Method "DELETE" -Uri "/api/todos/completed"

# Test 34: Batch delete remaining todos
if ($todoId1 -and $todoId2) {
    $batchDelete = @{
        ids = @($todoId1, $todoId2, $todoId4)
    }
    Test-Endpoint -Name "DELETE /api/todos/batch" -Method "DELETE" -Uri "/api/todos/batch" -Body $batchDelete
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "❌ ERROR HANDLING" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Test 35: Get non-existent todo
Test-Endpoint -Name "GET /api/todos/99999 (not found)" -Method "GET" -Uri "/api/todos/99999" -ExpectedStatus 404

# Test 36: Update non-existent todo
$updateNonExistent = @{
    title = "Non-existent"
    priority = 1
    isCompleted = $false
}
Test-Endpoint -Name "PUT /api/todos/99999 (not found)" -Method "PUT" -Uri "/api/todos/99999" -Body $updateNonExistent -ExpectedStatus 404

# Test 37: Delete non-existent todo
Test-Endpoint -Name "DELETE /api/todos/99999 (not found)" -Method "DELETE" -Uri "/api/todos/99999" -ExpectedStatus 404

# Test 38: Complete non-existent todo
Test-Endpoint -Name "PUT /api/todos/99999/complete (not found)" -Method "PUT" -Uri "/api/todos/99999/complete" -ExpectedStatus 404

# Test 39: Update actual minutes for non-existent todo
$updateMinutesNonExistent = @{
    actualMinutes = 100
}
Test-Endpoint -Name "PUT /api/todos/99999/actual-minutes (not found)" -Method "PUT" -Uri "/api/todos/99999/actual-minutes" -Body $updateMinutesNonExistent -ExpectedStatus 404

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

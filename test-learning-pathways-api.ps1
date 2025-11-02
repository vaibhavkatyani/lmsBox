# Test script for Learning Pathways API
# This script tests the new learning pathways endpoints

$BaseUrl = "http://localhost:5121"
$AdminEmail = "admin@dev.local"

Write-Host "=== Learning Pathways API Test Script ===" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Admin Email: $AdminEmail" -ForegroundColor Yellow
Write-Host ""

# Step 1: Request login link
Write-Host "Step 1: Requesting login link..." -ForegroundColor Cyan
try {
    $loginRequest = @{
        email = $AdminEmail
    }
    
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body ($loginRequest | ConvertTo-Json) -ContentType "application/json"
    Write-Host "✅ Login link requested successfully" -ForegroundColor Green
    Write-Host "Response: $($loginResponse.message)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "🔍 CHECK THE SERVER CONSOLE LOGS for the login token!" -ForegroundColor Yellow
    Write-Host "Look for a log entry with the login token or link." -ForegroundColor Yellow
    Write-Host ""
    
    # Step 2: Prompt for token
    $token = Read-Host "Enter the login token from the server logs"
    
    if ([string]::IsNullOrWhiteSpace($token)) {
        Write-Host "❌ No token provided. Exiting." -ForegroundColor Red
        exit 1
    }
    
    # Step 3: Verify login link to get JWT
    Write-Host "Step 2: Verifying login token to get JWT..." -ForegroundColor Cyan
    
    $verifyRequest = @{
        token = $token.Trim()
    }
    
    $jwtResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/verify-login-link" -Method Post -Body ($verifyRequest | ConvertTo-Json) -ContentType "application/json"
    
    $jwtToken = $jwtResponse.token
    $expires = $jwtResponse.expires
    
    Write-Host "✅ JWT token obtained successfully" -ForegroundColor Green
    Write-Host "Token expires: $expires" -ForegroundColor White
    Write-Host ""
    
    # Step 4: Test Learning Pathways API
    Write-Host "Step 3: Testing Learning Pathways API..." -ForegroundColor Cyan
    
    $headers = @{
        "Authorization" = "Bearer $jwtToken"
        "Accept" = "application/json"
    }
    
    # Test GET learning pathways
    $pathwaysResponse = Invoke-RestMethod -Uri "$BaseUrl/api/admin/learning-pathways?sort=updated_desc" -Method Get -Headers $headers
    
    Write-Host "✅ Learning pathways API called successfully" -ForegroundColor Green
    Write-Host "Total pathways: $($pathwaysResponse.total)" -ForegroundColor White
    Write-Host ""
    
    # Display pathway information
    if ($pathwaysResponse.items.Count -gt 0) {
        Write-Host "🛤️ Learning Pathways List:" -ForegroundColor Cyan
        foreach ($pathway in $pathwaysResponse.items) {
            Write-Host "  - ID: $($pathway.id)" -ForegroundColor Yellow
            Write-Host "    Title: $($pathway.title)" -ForegroundColor White
            Write-Host "    Difficulty: $($pathway.difficultyLevel)" -ForegroundColor Gray
            Write-Host "    Courses: $($pathway.courseCount)" -ForegroundColor Gray
            Write-Host "    Active: $($pathway.isActive)" -ForegroundColor Gray
            Write-Host ""
        }
    } else {
        Write-Host "📝 No learning pathways found. Database is empty." -ForegroundColor Yellow
        Write-Host ""
    }
    
    # Test creating a new learning pathway (if we have courses available)
    Write-Host "Step 4: Testing course availability for pathway creation..." -ForegroundColor Cyan
    
    $coursesResponse = Invoke-RestMethod -Uri "$BaseUrl/api/admin/courses?pageSize=5" -Method Get -Headers $headers
    
    if ($coursesResponse.items.Count -gt 0) {
        Write-Host "✅ Found $($coursesResponse.items.Count) courses available for pathways" -ForegroundColor Green
        
        # Show available courses
        Write-Host "📚 Available Courses:" -ForegroundColor Cyan
        foreach ($course in $coursesResponse.items) {
            Write-Host "  - $($course.id): $($course.title)" -ForegroundColor White
        }
        
        Write-Host ""
        Write-Host "📝 You can now create learning pathways using these courses!" -ForegroundColor Yellow
    } else {
        Write-Host "⚠️ No courses available. Create some courses first to test pathway creation." -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "🎉 Learning Pathways API test completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your JWT token for manual testing:" -ForegroundColor Yellow
    Write-Host $jwtToken -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $responseText = $reader.ReadToEnd()
        Write-Host "Response: $responseText" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "📝 Manual testing with curl:" -ForegroundColor Yellow
Write-Host "# List learning pathways:" -ForegroundColor Gray
Write-Host "curl -H 'Authorization: Bearer YOUR_TOKEN_HERE' '$BaseUrl/api/admin/learning-pathways'" -ForegroundColor Gray
Write-Host ""
Write-Host "# Create a learning pathway:" -ForegroundColor Gray
Write-Host 'curl -X POST -H "Authorization: Bearer YOUR_TOKEN_HERE" -H "Content-Type: application/json" -d "{\"title\":\"Test Pathway\",\"description\":\"A test learning pathway\",\"difficultyLevel\":\"Beginner\",\"estimatedDurationHours\":10,\"courses\":[]}" "' + $BaseUrl + '/api/admin/learning-pathways"' -ForegroundColor Gray
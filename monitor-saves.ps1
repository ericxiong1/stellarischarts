# Monitor Stellaris saves folder and automatically upload new snapshots
param(
    [string]$SavesFolder = "c:\Users\Eric\Desktop\stellaris-charts\saves",
    [string]$ApiUrl = "http://localhost:5000/api/saves/upload"
)

# Create saves folder if it doesn't exist
if (-not (Test-Path $SavesFolder)) {
    New-Item -ItemType Directory -Path $SavesFolder | Out-Null
    Write-Host "Created saves folder: $SavesFolder" -ForegroundColor Cyan
}

$processedFiles = @{}
$checkInterval = 5  # Check every 5 seconds

Write-Host "Stellaris Gamestate Monitor" -ForegroundColor Cyan
Write-Host "Monitoring folder: $SavesFolder" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Cyan
Write-Host "Check interval: $checkInterval seconds" -ForegroundColor Cyan
Write-Host "`nPress Ctrl+C to stop monitoring`n" -ForegroundColor Yellow

while ($true) {
    try {
        # Get all files in the saves folder
        $files = Get-ChildItem -Path $SavesFolder -File | Sort-Object LastWriteTime -Descending
        
        foreach ($file in $files) {
            # Skip if already processed
            if ($processedFiles.ContainsKey($file.FullName)) {
                continue
            }
            
            # Skip if file is currently being written to (less than 1 second old)
            $age = (Get-Date) - $file.LastWriteTime
            if ($age.TotalSeconds -lt 1) {
                Write-Host "File still being written: $($file.Name), skipping..." -ForegroundColor Gray
                continue
            }
            
            Write-Host "`n[$(Get-Date -Format 'HH:mm:ss')] Found new gamestate: $($file.Name)" -ForegroundColor Green
            Write-Host "File size: $([math]::Round($file.Length / 1024 / 1024, 2)) MB" -ForegroundColor Yellow
            
            try {
                Write-Host "Uploading..." -ForegroundColor Cyan
                $startTime = Get-Date

                # Derive timestamp from filename if possible (autosave_YYYY.MM.DD.sav)
                $timestamp = $file.LastWriteTimeUtc
                if ($file.Name -match '_(\d{4})\.(\d{2})\.(\d{2})') {
                    try {
                        $timestamp = [DateTime]::SpecifyKind([DateTime]::new([int]$matches[1], [int]$matches[2], [int]$matches[3], 0, 0, 0), [DateTimeKind]::Utc)
                    } catch {
                        # fallback to LastWriteTimeUtc
                    }
                }

                # Use curl to upload the file
                $timestampHeader = "X-File-Timestamp: $($timestamp.ToString("o"))"
                $output = & curl.exe -s -H $timestampHeader -F "file=@$($file.FullName)" "$ApiUrl" 2>&1
                
                $elapsed = (Get-Date) - $startTime
                
                if ($LASTEXITCODE -eq 0) {
                    $responseJson = $output | ConvertFrom-Json
                    Write-Host "Success!" -ForegroundColor Green
                    Write-Host "  Message: $($responseJson.message)" -ForegroundColor Green
                    Write-Host "  Countries processed: $($responseJson.countries)" -ForegroundColor Green
                    Write-Host "  Time: $($elapsed.TotalSeconds) seconds" -ForegroundColor Green
                    
                    # Mark as processed
                    $processedFiles[$file.FullName] = $true
                } else {
                    Write-Host "Upload failed" -ForegroundColor Red
                    Write-Host "  Response: $output" -ForegroundColor Red
                }
            }
            catch {
                Write-Host "Upload failed" -ForegroundColor Red
                Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        
        # Wait before checking again
        Start-Sleep -Seconds $checkInterval
    }
    catch {
        Write-Host ("Error during monitoring: " + $_.Exception.Message) -ForegroundColor Red
        Start-Sleep -Seconds $checkInterval
    }
}

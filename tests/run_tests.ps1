<#
    Touhou Web Arena - regression suite runner.

    Why this exists in its current form: the suites are written in two styles.
    Eight of them count assertions and gate quit() on the result, so a failure
    shows up as a non-zero exit code. The other fifteen use Godot's bare
    assert(), which aborts the failing function, prints to stderr, and then lets
    the script reach quit(0) anyway. Judging purely on exit codes therefore
    reported those fifteen suites as passing no matter what they found - roughly
    500 assertions that could never fail the run.

    So a suite fails here if its exit code is non-zero, OR it printed
    "SCRIPT ERROR" to stderr, OR it hung. That one string covers failed
    assertions, null property access, calls to missing functions and parse
    errors, and no passing suite emits it. Plain "ERROR:" lines are deliberately
    NOT treated as failures: several suites legitimately produce them
    (test_replay_system push_error()s on purpose to exercise a negative path,
    and Godot reports leaked resources at teardown).

    The hang case is real rather than theoretical: assert() aborts its enclosing
    function, so one firing inside _init() skips the quit() at the end of it and
    Godot never exits.

    Set the GODOT environment variable to override the executable path.
#>

$ErrorActionPreference = "Stop"

# Generous ceiling: the slowest suite today runs in about 4 seconds.
$suiteTimeoutSec = 60

$godot = $env:GODOT
if (-not $godot) {
    $godot = "$env:USERPROFILE\Desktop\Godot_v4.7.2-stable_win64.exe"
}
if (-not (Test-Path $godot)) {
    Write-Host "Godot executable not found at: $godot" -ForegroundColor Red
    Write-Host "Set the GODOT environment variable to point at it." -ForegroundColor Yellow
    exit 2
}

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$passed = 0
$failed = 0
$failures = @()
$totalTimer = [Diagnostics.Stopwatch]::StartNew()

Write-Host ""
Write-Host "========================================================"
Write-Host " Touhou Web Arena - Running Automated Test Suite"
Write-Host "========================================================"
Write-Host ""

foreach ($file in Get-ChildItem (Join-Path $projectRoot "tests\test_*.gd") | Sort-Object Name) {
    $rel = "res://tests/" + $file.Name
    $timer = [Diagnostics.Stopwatch]::StartNew()

    # Driving System.Diagnostics.Process directly rather than Start-Process:
    # Start-Process -PassThru without -Wait leaves ExitCode null (which silently
    # marks every suite failed), and -Wait gives no way to time out a hung suite.
    # This gives both a real exit code and a timeout.
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $godot
    $psi.Arguments = '--headless --script ' + $rel
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi
    $proc.Start() | Out-Null

    # Drain both pipes concurrently; a suite that fills one while we block on the
    # other would deadlock.
    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()

    $timedOut = $false
    if (-not $proc.WaitForExit($suiteTimeoutSec * 1000)) {
        $timedOut = $true
        try { $proc.Kill() } catch { }
        $proc.WaitForExit()
    }
    $timer.Stop()

    $exitCode = $proc.ExitCode
    $stderrText = $stderrTask.Result
    $null = $stdoutTask.Result
    $proc.Dispose()

    $scriptErrors = @()
    if ($stderrText) {
        $scriptErrors = @($stderrText -split "`r?`n" | Where-Object { $_ -match "SCRIPT ERROR" })
    }

    $reasons = @()
    if ($timedOut) {
        $reasons += "timed out after ${suiteTimeoutSec}s (killed)"
    }
    elseif ($exitCode -ne 0) {
        $reasons += "exit code $exitCode"
    }
    if ($scriptErrors.Count -gt 0) {
        $reasons += "$($scriptErrors.Count) script error(s) on stderr"
    }

    $secs = [math]::Round($timer.Elapsed.TotalSeconds, 1)

    if ($reasons.Count -eq 0) {
        Write-Host ("  [PASS] {0}  ({1}s)" -f $file.Name, $secs) -ForegroundColor Green
        $passed++
    }
    else {
        Write-Host ("  [FAIL] {0}  ({1}s) - {2}" -f $file.Name, $secs, ($reasons -join "; ")) -ForegroundColor Red
        foreach ($line in $scriptErrors | Select-Object -First 3) {
            Write-Host ("         " + $line.Trim()) -ForegroundColor DarkYellow
        }
        $failed++
        $failures += $file.Name
    }
}

$totalTimer.Stop()

Write-Host ""
Write-Host "========================================================"
if ($failed -eq 0) {
    Write-Host (" All {0} test suites PASSED  ({1:N1}s)" -f $passed, $totalTimer.Elapsed.TotalSeconds) -ForegroundColor Green
    exit 0
}
else {
    Write-Host (" {0} of {1} suites FAILED  ({2:N1}s)" -f $failed, ($passed + $failed), $totalTimer.Elapsed.TotalSeconds) -ForegroundColor Red
    foreach ($name in $failures) {
        Write-Host ("   - " + $name) -ForegroundColor Red
    }
    exit 1
}

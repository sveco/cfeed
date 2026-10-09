# End to end check that cfeed redraws when its console is resized.
# Starts cfeed in a hidden console, resizes that console with the Windows console API, sends keys to open a
# feed and an article, and reads the screen back after every size. Windows only, needs internet access for
# the feeds in settings.conf. The files in the exe folder are copied, the real cfeed.db is not touched.
#
# Usage: powershell -File scripts\resize-check.ps1 [-ExeDir CRR\bin\Debug] [-SettleMs 1500] [-Dump]
# Exit code 0 when every check passed.
param(
    [string]$ExeDir = (Join-Path (Split-Path $PSScriptRoot -Parent) 'CRR\bin\Debug'),
    [int]$SettleMs = 1500,
    [switch]$Dump   # print the screen after every size
)
$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class ConsoleProbe
{
    [StructLayout(LayoutKind.Sequential)] public struct COORD { public short X; public short Y; public COORD(short x, short y) { X = x; Y = y; } }
    [StructLayout(LayoutKind.Sequential)] public struct SMALL_RECT { public short Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct CSBI { public COORD dwSize; public COORD dwCursorPosition; public ushort wAttributes; public SMALL_RECT srWindow; public COORD dwMaximumWindowSize; }
    [StructLayout(LayoutKind.Explicit, Size = 20)] public struct INPUT_RECORD
    {
        [FieldOffset(0)] public ushort EventType;
        [FieldOffset(4)] public int bKeyDown;
        [FieldOffset(8)] public ushort wRepeatCount;
        [FieldOffset(10)] public ushort wVirtualKeyCode;
        [FieldOffset(12)] public ushort wVirtualScanCode;
        [FieldOffset(14)] public ushort UnicodeChar;
        [FieldOffset(16)] public uint dwControlKeyState;
    }

    [DllImport("kernel32.dll", SetLastError = true)] static extern bool FreeConsole();
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool AttachConsole(uint pid);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] static extern IntPtr CreateFile(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetConsoleScreenBufferInfo(IntPtr h, out CSBI info);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetConsoleScreenBufferSize(IntPtr h, COORD size);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetConsoleWindowInfo(IntPtr h, bool absolute, ref SMALL_RECT rect);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] static extern bool ReadConsoleOutputCharacter(IntPtr h, StringBuilder sb, uint len, COORD origin, out uint read);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteConsoleInput(IntPtr h, INPUT_RECORD[] buffer, uint length, out uint written);

    static IntPtr output = IntPtr.Zero;
    static IntPtr input = IntPtr.Zero;

    public static string Attach(uint pid)
    {
        FreeConsole();
        if (!AttachConsole(pid)) return "AttachConsole failed, error " + Marshal.GetLastWin32Error();
        // GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, OPEN_EXISTING
        output = CreateFile("CONOUT$", 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
        input = CreateFile("CONIN$", 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
        if (output == new IntPtr(-1) || input == new IntPtr(-1)) return "CreateFile CONOUT$/CONIN$ failed, error " + Marshal.GetLastWin32Error();
        return null;
    }

    public static void Detach() { FreeConsole(); output = IntPtr.Zero; input = IntPtr.Zero; }

    public static string Resize(short w, short h)
    {
        CSBI info;
        if (!GetConsoleScreenBufferInfo(output, out info)) return "GetConsoleScreenBufferInfo failed, error " + Marshal.GetLastWin32Error();
        short bufferHeight = 300;
        // the buffer must never be smaller than the window: grow it first, then move the window, then trim it
        if (!SetConsoleScreenBufferSize(output, new COORD(Math.Max(w, info.dwSize.X), Math.Max(bufferHeight, info.dwSize.Y)))) return "grow buffer failed, error " + Marshal.GetLastWin32Error();
        SMALL_RECT r = new SMALL_RECT(); r.Left = 0; r.Top = 0; r.Right = (short)(w - 1); r.Bottom = (short)(h - 1);
        if (!SetConsoleWindowInfo(output, true, ref r)) return "SetConsoleWindowInfo failed, error " + Marshal.GetLastWin32Error();
        if (!SetConsoleScreenBufferSize(output, new COORD(w, bufferHeight))) return "trim buffer failed, error " + Marshal.GetLastWin32Error();
        return null;
    }

    public static string Size()
    {
        CSBI info;
        if (!GetConsoleScreenBufferInfo(output, out info)) return "0x0";
        return (info.srWindow.Right - info.srWindow.Left + 1) + "x" + (info.srWindow.Bottom - info.srWindow.Top + 1) + "@" + info.srWindow.Top;
    }

    public static string[] ReadWindow()
    {
        CSBI info;
        GetConsoleScreenBufferInfo(output, out info);
        int w = info.srWindow.Right - info.srWindow.Left + 1;
        int h = info.srWindow.Bottom - info.srWindow.Top + 1;
        string[] rows = new string[h];
        for (int i = 0; i < h; i++)
        {
            // the API does not null terminate, so cut the text at the number of characters it reports
            StringBuilder sb = new StringBuilder(w + 1);
            uint read;
            ReadConsoleOutputCharacter(output, sb, (uint)w, new COORD(info.srWindow.Left, (short)(info.srWindow.Top + i)), out read);
            string text = sb.ToString();
            rows[i] = text.Substring(0, (int)Math.Min(read, (uint)text.Length));
        }
        return rows;
    }

    public static void SendKey(ushort virtualKey, char ch)
    {
        INPUT_RECORD[] records = new INPUT_RECORD[2];
        for (int i = 0; i < 2; i++)
        {
            records[i].EventType = 1; // KEY_EVENT
            records[i].bKeyDown = i == 0 ? 1 : 0;
            records[i].wRepeatCount = 1;
            records[i].wVirtualKeyCode = virtualKey;
            records[i].wVirtualScanCode = 0;
            records[i].UnicodeChar = ch;
        }
        uint written;
        WriteConsoleInput(input, records, 2, out written);
    }
}
'@

if (-not (Test-Path (Join-Path $ExeDir 'cfeed.exe'))) { throw "cfeed.exe not found in $ExeDir" }

# work on a copy: the app creates cfeed.db and logs next to the exe
$work = Join-Path $env:TEMP ('cfeed-resize-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $work | Out-Null
Get-ChildItem $ExeDir -File | Where-Object { $_.Extension -in '.exe', '.dll', '.config', '.conf', '.opml' } | Copy-Item -Destination $work

$report = New-Object System.Collections.ArrayList
$failures = 0
function Add-Line($text) { [void]$script:report.Add($text) }
function Check($name, $ok, $detail) {
    if (-not $ok) { $script:failures++ }
    Add-Line ('  {0,-4} {1}{2}' -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $(if ($detail) { '  (' + $detail + ')' } else { '' }))
}
function Press($vk, $ch, $times = 1) { for ($i = 0; $i -lt $times; $i++) { [ConsoleProbe]::SendKey([uint16]$vk, [char]$ch); Start-Sleep -Milliseconds 150 } }
function Set-Size($w, $h) {
    $err = [ConsoleProbe]::Resize([int16]$w, [int16]$h)
    if ($err) { throw $err }
    Start-Sleep -Milliseconds $SettleMs
}
$VK = @{ Enter = 0x0D; Escape = 0x1B; Down = 0x28 }

# Checks what every view has in common: header on the first row, footer on the last row, both as wide as the
# window, no copy of them left over from the previous size, and no text running to the very edge of the window
# (which would mean it was wrapped for the old width).
function Verify-Screen($label, $w, $h, $headerText, $footerText, $checkWrap = $false) {
    $rows = [ConsoleProbe]::ReadWindow()
    Add-Line ("{0}, {1}x{2} (console reports {3})" -f $label, $w, $h, [ConsoleProbe]::Size())
    if ($Dump) { for ($i = 0; $i -lt $rows.Count; $i++) { Add-Line ('    {0,2}|{1}' -f $i, $rows[$i].TrimEnd()) } }
    $first = $rows[0].TrimEnd(); $last = $rows[$rows.Count - 1].TrimEnd()
    $headerRows = @($rows | Where-Object { $_.Contains($headerText) }).Count
    $footerRows = @($rows | Where-Object { $_.Contains($footerText) }).Count
    Check 'header on the first row' ($first.Contains($headerText)) $first.Trim()
    Check 'header spans the new width' ($first.Length -ge $w - 3) ("{0} of {1} columns" -f $first.Length, $w)
    Check 'footer on the last row' ($last.Contains($footerText)) $last.Trim()
    Check 'footer spans the new width' ($last.Length -ge $w - 3) ("{0} of {1} columns" -f $last.Length, $w)
    Check 'no stale header or footer left behind' ($headerRows -eq 1 -and $footerRows -eq 1) ("$headerRows header row(s), $footerRows footer row(s)")
    if ($checkWrap) {
        # Article text is wrapped by the app to the window width. A row that runs to the edge of the window
        # was wrapped for a wider window and then wrapped again by the console.
        $body = $rows[1..($rows.Count - 2)]
        # the scrollbar is drawn in the last column, so rows that end in a scrollbar character are fine
        $scrollbar = [char[]]@(0x25B2, 0x25BC, 0x2588, 0x2592)
        $wide = @($body | Where-Object { $t = $_.TrimEnd(); $t.Length -ge $w - 1 -and $scrollbar -notcontains $t[$t.Length - 1] }).Count
        Check 'article text wrapped for the new width' ($wide -eq 0) ("$wide row(s) reach the window edge")
    }
    return $rows
}

$proc = Start-Process -FilePath (Join-Path $work 'cfeed.exe') -WorkingDirectory $work -WindowStyle Hidden -PassThru
try {
    Start-Sleep -Milliseconds 3000
    if ($proc.HasExited) { throw "cfeed.exe exited early with code $($proc.ExitCode)" }
    $err = [ConsoleProbe]::Attach([uint32]$proc.Id)
    if ($err) { throw $err }

    # ---- feed list -------------------------------------------------------------------------------
    Add-Line '== feed list'
    $listRows = $null
    foreach ($s in @(@(140, 45), @(100, 30), @(80, 20), @(60, 15), @(15, 5), @(100, 30), @(130, 40))) {
        Set-Size $s[0] $s[1]
        if ($s[0] -lt 20 -or $s[1] -lt 6) {
            Add-Line ("{0}x{1} (console reports {2})" -f $s[0], $s[1], [ConsoleProbe]::Size())
            Check 'too small, app keeps running' (-not $proc.HasExited) $null
            continue
        }
        $rows = Verify-Screen 'feed list' $s[0] $s[1] 'cfeed v' 'Q:Quit'
        $bodyRows = @($rows[1..($rows.Count - 2)] | Where-Object { $_.Trim().Length -gt 0 }).Count
        # the largest size shows every feed, that is how many rows the smaller sizes should fill
        if ($listRows -eq $null) { $listRows = [Math]::Min($bodyRows, $s[1] - 4) }
        $expected = [Math]::Min($listRows, $s[1] - 4)
        Check 'list fills the available rows' ($bodyRows -ge $expected) ("$bodyRows rows with text, expected at least $expected")
    }

    # ---- article list ----------------------------------------------------------------------------
    Add-Line '== article list (BBC News - World)'
    Set-Size 100 30
    Press $VK.Down 0 2           # third feed in the list
    Press $VK.Enter 13
    Start-Sleep -Milliseconds 2500
    foreach ($s in @(@(100, 30), @(70, 12), @(130, 45), @(90, 25))) {
        Set-Size $s[0] $s[1]
        $rows = Verify-Screen 'article list' $s[0] $s[1] 'Articles in' 'ESC/Backspace:Back'
        $bodyRows = @($rows[1..($rows.Count - 2)] | Where-Object { $_.Trim().Length -gt 0 }).Count
        Check 'list fills the available rows' ($bodyRows -ge [Math]::Min(20, $s[1] - 4)) ("$bodyRows rows with text")
    }

    # ---- article ---------------------------------------------------------------------------------
    Add-Line '== article'
    Set-Size 100 30
    Press $VK.Enter 13
    Start-Sleep -Milliseconds 6000   # downloads the article page
    foreach ($s in @(@(100, 30), @(70, 20), @(130, 45), @(90, 25))) {
        Set-Size $s[0] $s[1]
        $rows = Verify-Screen 'article' $s[0] $s[1] 'Article:' 'ESC/Backspace' $true
        $bodyRows = @($rows[1..($rows.Count - 2)] | Where-Object { $_.Trim().Length -gt 0 }).Count
        Check 'article text is on screen' ($bodyRows -ge 8) ("$bodyRows rows with text")
    }

    # ---- back to the feed list, resized while the other views were open -----------------------------
    Add-Line '== back in the feed list'
    Press $VK.Escape 27
    Start-Sleep -Milliseconds 1500
    Press $VK.Escape 27
    Start-Sleep -Milliseconds 1500
    [void](Verify-Screen 'feed list again' 90 25 'cfeed v' 'Q:Quit')
    Check 'app still running' (-not $proc.HasExited) $null
}
catch {
    $failures++
    Add-Line ("ERROR: " + $_.Exception.Message)
}
finally {
    [ConsoleProbe]::Detach()
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
    Start-Sleep -Milliseconds 300
    # what the app logged about the redraws
    $log = Get-ChildItem $work -Filter '*_log.txt' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($log) {
        $lines = @(Get-Content $log.FullName | Where-Object { $_ -match 'Redraw|Cannot read console' -or $_ -match 'RuntimeBinder' })
        Add-Line ("app log: " + $lines.Count + " redraw error line(s)")
        $lines | Select-Object -First 8 | ForEach-Object { Add-Line ("    " + $_.Substring(0, [Math]::Min(200, $_.Length))) }
        if ($lines.Count -gt 0) { $failures++ }
    }
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

$report | ForEach-Object { $_ }
if ($failures -eq 0) { 'ALL CHECKS PASSED' } else { "$failures CHECK(S) FAILED" }
exit $(if ($failures -eq 0) { 0 } else { 1 })

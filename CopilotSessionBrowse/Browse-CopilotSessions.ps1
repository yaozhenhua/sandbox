<#
.SYNOPSIS
    Interactive console browser for GitHub Copilot CLI sessions.
.DESCRIPTION
    Lists all Copilot sessions from ~/.copilot/session-state, grouped by
    repository and branch. Supports keyboard navigation and deletion.
.NOTES
    Keys: Up/Down = navigate, Delete = delete session, Esc = quit
#>

[CmdletBinding()]
param(
    [string]$SessionRoot = (Join-Path $env:USERPROFILE ".copilot\session-state")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

#region -- Data Model ----------------------------------------------------------

function Read-Sessions {
    param([string]$Root)

    $dirs = Get-ChildItem $Root -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^[0-9a-f]{8}-' }

    $sessions = [System.Collections.Generic.List[PSCustomObject]]::new()

    foreach ($d in $dirs) {
        $yamlPath = Join-Path $d.FullName "workspace.yaml"
        $info = [ordered]@{
            Id         = $d.Name
            Path       = $d.FullName
            Repository = ''
            GitRoot    = ''
            Cwd        = ''
            Branch     = ''
            HostType   = ''
            Summary    = ''
            CreatedAt  = $d.CreationTime
            UpdatedAt  = $d.LastWriteTime
            SizeMB     = 0.0
            IsLocked   = $false
        }

        # Detect active lock
        $locks = Get-ChildItem $d.FullName -Filter "inuse.*.lock" -File -ErrorAction SilentlyContinue
        foreach ($lf in $locks) {
            if ($lf.Name -match 'inuse\.(\d+)\.lock') {
                $pid_ = [int]$Matches[1]
                try {
                    $proc = Get-Process -Id $pid_ -ErrorAction SilentlyContinue
                    if ($proc) { $info.IsLocked = $true }
                } catch {}
            }
        }

        # Parse workspace.yaml (simple key: value, no YAML module needed)
        if (Test-Path $yamlPath) {
            $lines = Get-Content $yamlPath -ErrorAction SilentlyContinue
            foreach ($line in $lines) {
                if ($line -match '^\s*repository:\s*(.+)$')  { $info.Repository = $Matches[1].Trim() }
                if ($line -match '^\s*git_root:\s*(.+)$')   { $info.GitRoot    = $Matches[1].Trim() }
                if ($line -match '^\s*cwd:\s*(.+)$')         { $info.Cwd        = $Matches[1].Trim() }
                if ($line -match '^\s*branch:\s*(.+)$')      { $info.Branch     = $Matches[1].Trim() }
                if ($line -match '^\s*host_type:\s*(.+)$')   { $info.HostType   = $Matches[1].Trim() }
                if ($line -match '^\s*summary:\s*(.+)$')     { $info.Summary    = $Matches[1].Trim().Trim("'`"") }
                if ($line -match '^\s*created_at:\s*(.+)$') {
                    try { $info.CreatedAt = [datetime]::Parse($Matches[1].Trim()) } catch {}
                }
                if ($line -match '^\s*updated_at:\s*(.+)$') {
                    try { $info.UpdatedAt = [datetime]::Parse($Matches[1].Trim()) } catch {}
                }
            }
        }

        # Compute size
        $bytes = (Get-ChildItem $d.FullName -Recurse -File -ErrorAction SilentlyContinue |
                  Measure-Object -Property Length -Sum).Sum
        $info.SizeMB = if ($bytes) { [math]::Round($bytes / 1MB, 2) } else { 0.0 }

        $sessions.Add([PSCustomObject]$info)
    }

    return $sessions
}

function Group-Sessions {
    param([System.Collections.Generic.List[PSCustomObject]]$Sessions)

    # Group by repo+branch, sort groups alphabetically, sessions by UpdatedAt desc
    # Fall back to git_root when repository is empty
    $grouped = $Sessions | Group-Object {
        $repo = if ($_.Repository) { $_.Repository } elseif ($_.GitRoot) { $_.GitRoot } else { '' }
        "$repo|$($_.Branch)"
    } | Sort-Object { $_.Name }

    $result = [System.Collections.Generic.List[PSCustomObject]]::new()
    foreach ($g in $grouped) {
        $parts = $g.Name -split '\|', 2
        $repo   = if ($parts[0]) { $parts[0] } else { '(unknown)' }
        $branch = if ($parts.Length -gt 1 -and $parts[1]) { $parts[1] } else { '(no branch)' }

        $sorted = $g.Group | Sort-Object UpdatedAt -Descending

        $result.Add([PSCustomObject]@{
            Repo     = $repo
            Branch   = $branch
            Sessions = $sorted
        })
    }
    return $result
}

#endregion

#region -- Display Model -------------------------------------------------------

# Each display line: Type (group|session|detail|blank), Text, SessionIndex (or -1), Colors
function Build-DisplayLines {
    param($Groups, [int]$Width)

    $lines = [System.Collections.Generic.List[PSCustomObject]]::new()
    $sessionIndex = 0

    foreach ($g in $Groups) {
        # Blank separator between groups
        if ($lines.Count -gt 0) {
            $lines.Add([PSCustomObject]@{ Type='blank'; Text=''; SessionIndex=-1 })
        }

        # Group header
        $hdr = "  $([char]0xd83d)$([char]0xdcc1) $($g.Repo) ($($g.Branch))"
        $lines.Add([PSCustomObject]@{ Type='group'; Text=$hdr; SessionIndex=-1 })

        foreach ($s in $g.Sessions) {
            $num = $sessionIndex + 1
            $summ = if ($s.Summary) { $s.Summary } else { '(no summary)' }
            $lock = if ($s.IsLocked) { ' [ACTIVE]' } else { '' }
            # Truncate summary if it would overflow the line
            $prefix = "     {0,3}. " -f $num
            $maxSumm = $Width - $prefix.Length - $lock.Length - 2
            if ($maxSumm -gt 10 -and $summ.Length -gt $maxSumm) {
                $summ = $summ.Substring(0, $maxSumm - 3) + '...'
            }
            $line1 = "$prefix$summ$lock"

            $created = $s.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            $updated = $s.UpdatedAt.ToString("yyyy-MM-dd HH:mm")
            $sizeStr = "{0,6:N2} MB" -f $s.SizeMB
            $idShort = $s.Id.Substring(0, 8)
            $line2 = "          Created: $created   Modified: $updated   $sizeStr   [$idShort]"

            $cwdLine = if ($s.Cwd) { "          cwd: $($s.Cwd)" } else { $null }

            $lines.Add([PSCustomObject]@{ Type='session'; Text=$line1; SessionIndex=$sessionIndex })
            $lines.Add([PSCustomObject]@{ Type='detail';  Text=$line2; SessionIndex=$sessionIndex })
            if ($cwdLine) {
                $lines.Add([PSCustomObject]@{ Type='detail'; Text=$cwdLine; SessionIndex=$sessionIndex })
            }

            $sessionIndex++
        }
    }

    return $lines
}

#endregion

#region -- Rendering -----------------------------------------------------------

function Render-Screen {
    param(
        $DisplayLines,
        [int]$SelectedSessionIndex,
        [int]$ScrollOffset,
        [int]$ContentHeight,
        [int]$Width,
        [int]$TotalSessions,
        [string]$StatusMessage = ''
    )

    [Console]::CursorVisible = $false
    [Console]::SetCursorPosition(0, 0)

    # Header
    $title = "  Copilot Session Browser"
    $count = "$TotalSessions sessions  "
    $pad   = $Width - $title.Length - $count.Length
    if ($pad -lt 0) { $pad = 0 }
    $headerLine = $title + (' ' * $pad) + $count

    Write-Host $headerLine.PadRight($Width).Substring(0, $Width) -NoNewline -ForegroundColor White -BackgroundColor DarkCyan

    # Separator
    Write-Host (([char]0x2500).ToString() * $Width) -NoNewline -ForegroundColor DarkGray

    # Content area
    $endIdx = [Math]::Min($ScrollOffset + $ContentHeight, $DisplayLines.Count)

    for ($i = $ScrollOffset; $i -lt ($ScrollOffset + $ContentHeight); $i++) {
        if ($i -lt $DisplayLines.Count) {
            $dl = $DisplayLines[$i]
            $isSelected = ($dl.SessionIndex -ge 0 -and $dl.SessionIndex -eq $SelectedSessionIndex)

            $text = $dl.Text
            if ($text.Length -gt $Width) { $text = $text.Substring(0, $Width) }
            $text = $text.PadRight($Width)

            $colors = @{}
            if ($isSelected) {
                $colors = @{ ForegroundColor = 'White'; BackgroundColor = 'DarkBlue' }
            }
            elseif ($dl.Type -eq 'group') {
                $colors = @{ ForegroundColor = 'Yellow' }
            }
            elseif ($dl.Type -eq 'detail') {
                $colors = @{ ForegroundColor = 'DarkGray' }
            }

            Write-Host $text -NoNewline @colors
        }
        else {
            Write-Host (' ' * $Width) -NoNewline
        }
    }

    # Separator
    Write-Host (([char]0x2500).ToString() * $Width) -NoNewline -ForegroundColor DarkGray

    # Status bar
    if ($StatusMessage) {
        Write-Host $StatusMessage.PadRight($Width).Substring(0, [Math]::Min($StatusMessage.Length + 20, $Width)) -NoNewline -ForegroundColor White -BackgroundColor DarkRed
    }
    else {
        $keys = "  $([char]0x2191)$([char]0x2193) Navigate    Del Delete session    Esc Quit"
        Write-Host $keys.PadRight($Width).Substring(0, $Width) -NoNewline -ForegroundColor White -BackgroundColor DarkCyan
    }

    [Console]::SetCursorPosition(0, 0)
}

#endregion

#region -- Main Loop -----------------------------------------------------------

function Start-Browser {
    param([string]$Root)

    # Load data
    $allSessions = Read-Sessions -Root $Root
    if ($allSessions.Count -eq 0) {
        Write-Host "No sessions found in $Root"
        return
    }

    $groups = Group-Sessions -Sessions $allSessions

    # Build a flat list of sessions for index lookup
    $flatSessions = [System.Collections.Generic.List[PSCustomObject]]::new()
    foreach ($g in $groups) {
        foreach ($s in $g.Sessions) {
            $flatSessions.Add($s)
        }
    }

    $width = [Console]::WindowWidth
    $displayLines = Build-DisplayLines -Groups $groups -Width $width
    $totalSessions = $flatSessions.Count

    # Find all selectable session indices
    $selectableIndices = ($displayLines | Where-Object { $_.Type -eq 'session' } |
        Select-Object -ExpandProperty SessionIndex -Unique)

    if ($selectableIndices.Count -eq 0) {
        Write-Host "No selectable sessions."
        return
    }

    $selectedSessionIndex = $selectableIndices[0]
    $scrollOffset = 0
    $contentHeight = [Console]::WindowHeight - 4   # header + 2 separators + status

    $statusMessage = ''
    $needsRedraw = $true

    # Save and clear screen
    $savedFg = [Console]::ForegroundColor
    $savedBg = [Console]::BackgroundColor
    [Console]::Clear()

    try {
        while ($true) {
            # Handle window resize
            $newWidth = [Console]::WindowWidth
            $newContentH = [Console]::WindowHeight - 4
            if ($newWidth -ne $width -or $newContentH -ne $contentHeight) {
                $width = $newWidth
                $contentHeight = $newContentH
                $displayLines = Build-DisplayLines -Groups $groups -Width $width
                [Console]::Clear()
                $needsRedraw = $true
            }

            if ($needsRedraw) {
                # Ensure selected session lines are visible
                $firstLine = -1
                $lastLine = -1
                for ($i = 0; $i -lt $displayLines.Count; $i++) {
                    if ($displayLines[$i].SessionIndex -eq $selectedSessionIndex) {
                        if ($firstLine -lt 0) { $firstLine = $i }
                        $lastLine = $i
                    }
                }
                if ($firstLine -ge 0) {
                    if ($firstLine -lt $scrollOffset) { $scrollOffset = [Math]::Max(0, $firstLine - 1) }
                    if ($lastLine -ge $scrollOffset + $contentHeight) { $scrollOffset = $lastLine - $contentHeight + 1 }
                }

                Render-Screen -DisplayLines $displayLines `
                              -SelectedSessionIndex $selectedSessionIndex `
                              -ScrollOffset $scrollOffset `
                              -ContentHeight $contentHeight `
                              -Width $width `
                              -TotalSessions $totalSessions `
                              -StatusMessage $statusMessage
                $needsRedraw = $false
                $statusMessage = ''
            }

            # Wait for key
            $key = [Console]::ReadKey($true)

            switch ($key.Key) {
                'Escape' {
                    return
                }

                'UpArrow' {
                    $curIdx = [Array]::IndexOf($selectableIndices, $selectedSessionIndex)
                    if ($curIdx -gt 0) {
                        $selectedSessionIndex = $selectableIndices[$curIdx - 1]
                        $needsRedraw = $true
                    }
                }

                'DownArrow' {
                    $curIdx = [Array]::IndexOf($selectableIndices, $selectedSessionIndex)
                    if ($curIdx -lt $selectableIndices.Count - 1) {
                        $selectedSessionIndex = $selectableIndices[$curIdx + 1]
                        $needsRedraw = $true
                    }
                }

                'PageUp' {
                    $curIdx = [Array]::IndexOf($selectableIndices, $selectedSessionIndex)
                    $newIdx = [Math]::Max(0, $curIdx - [Math]::Floor($contentHeight / 3))
                    $selectedSessionIndex = $selectableIndices[$newIdx]
                    $needsRedraw = $true
                }

                'PageDown' {
                    $curIdx = [Array]::IndexOf($selectableIndices, $selectedSessionIndex)
                    $newIdx = [Math]::Min($selectableIndices.Count - 1, $curIdx + [Math]::Floor($contentHeight / 3))
                    $selectedSessionIndex = $selectableIndices[$newIdx]
                    $needsRedraw = $true
                }

                'Home' {
                    $selectedSessionIndex = $selectableIndices[0]
                    $scrollOffset = 0
                    $needsRedraw = $true
                }

                'End' {
                    $selectedSessionIndex = $selectableIndices[$selectableIndices.Count - 1]
                    $needsRedraw = $true
                }

                'Delete' {
                    $session = $flatSessions[$selectedSessionIndex]
                    if ($session.IsLocked) {
                        $statusMessage = "  Cannot delete: session is in use by another process."
                        $needsRedraw = $true
                        break
                    }

                    $summ = if ($session.Summary) { $session.Summary } else { $session.Id }
                    $prompt = "  Delete '$summ'? (y/n) "

                    # Draw prompt on status bar
                    [Console]::SetCursorPosition(0, [Console]::WindowHeight - 1)
                    Write-Host $prompt.PadRight($width).Substring(0, $width) -NoNewline -ForegroundColor White -BackgroundColor DarkRed
                    [Console]::CursorVisible = $false

                    $confirm = [Console]::ReadKey($true)
                    if ($confirm.KeyChar -eq 'y' -or $confirm.KeyChar -eq 'Y') {
                        try {
                            Remove-Item $session.Path -Recurse -Force
                            $statusMessage = "  Deleted: $summ"

                            # Reload everything
                            $allSessions = Read-Sessions -Root $Root
                            $groups = Group-Sessions -Sessions $allSessions
                            $flatSessions = [System.Collections.Generic.List[PSCustomObject]]::new()
                            foreach ($g in $groups) {
                                foreach ($s in $g.Sessions) { $flatSessions.Add($s) }
                            }
                            $displayLines = Build-DisplayLines -Groups $groups -Width $width
                            $totalSessions = $flatSessions.Count

                            $selectableIndices = @($displayLines | Where-Object { $_.Type -eq 'session' } |
                                Select-Object -ExpandProperty SessionIndex -Unique)

                            if ($selectableIndices.Count -eq 0) {
                                [Console]::Clear()
                                Write-Host "All sessions deleted."
                                return
                            }

                            if ($selectedSessionIndex -ge $selectableIndices.Count) {
                                $selectedSessionIndex = $selectableIndices[$selectableIndices.Count - 1]
                            }
                            else {
                                $selectedSessionIndex = $selectableIndices[[Math]::Min($selectedSessionIndex, $selectableIndices.Count - 1)]
                            }
                        }
                        catch {
                            $statusMessage = "  Error deleting: $_"
                        }
                    }
                    else {
                        $statusMessage = "  Cancelled."
                    }

                    [Console]::Clear()
                    $needsRedraw = $true
                }
            }
        }
    }
    finally {
        # Restore console state
        [Console]::ForegroundColor = $savedFg
        [Console]::BackgroundColor = $savedBg
        [Console]::CursorVisible = $true
        [Console]::Clear()
        Write-Host "Session browser closed."
    }
}

#endregion

# -- Entry point --
Start-Browser -Root $SessionRoot

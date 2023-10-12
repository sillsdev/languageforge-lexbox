param(
    [Parameter(Mandatory)][string] $traceDir,
    [Parameter(Mandatory)][string[]] $secrets
)

Add-Type -Assembly  System.IO.Compression.FileSystem

# trace = Core trace info
# network = Network log
# json = Network log payloads
$filesToMask = "\.(json|trace|network)$"
$files = Get-ChildItem -path $traceDir -Filter *.zip

Write-Output "Masking $($secrets.Count) secrets in $($files.Count) traces.";

foreach ($file in $files) {
    try {
        Write-Output "Masking $file"
        $zip = [System.IO.Compression.ZipFile]::Open($file.FullName, "Update")
        $entries = $zip.Entries.Where({ $_.Name -match $filesToMask })
        $traceMaskCount = 0;
        foreach ($entry in $entries) {
            $reader = [System.IO.StreamReader]::new($entry.Open())
            $content = $reader.ReadToEnd()
            $entryMaskCount = 0;
            foreach ($secret in $secrets) {
                $pieces = $content -split "\b$secret\b"
                $entryMaskCount += $pieces.Count - 1;
                $content = $pieces -join $pieces, "*******"
            }
            $reader.Dispose()
            $writer = [System.IO.StreamWriter]::new($entry.Open())
            $writer.BaseStream.SetLength(0)
            $writer.Write($content)
            $writer.Dispose()
            Write-Output "- $entry ($entryMaskCount)"
        }
        Write-Output "Finished masking $file ($traceMaskCount)"
    }
    catch {
        Write-Warning $_.Exception.Message
        continue
    }
    finally {
        if ($zip) {
            $zip.Dispose()
        }
    }
}

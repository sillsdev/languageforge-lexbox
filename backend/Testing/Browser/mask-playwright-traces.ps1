param(
    [Parameter(Mandatory)][string] $dir,
    [Parameter(Mandatory)][string[]] $values
)

Add-Type -Assembly  System.IO.Compression.FileSystem

# trace = Core trace info
# network = Network log
# json = Network log payloads
$filesToEdit = "\.(json|trace|network)$"
$files = Get-ChildItem -path $dir -Filter *.zip

Write-Output "Masking $($values.Count) values in $($files.Count) traces.";

foreach ($file in $files) {
    try {
        Write-Output "Masking $file"
        $zip = [System.IO.Compression.ZipFile]::Open($file.FullName, "Update")
        $entries = $zip.Entries.Where({ $_.Name -match $filesToEdit })
        $traceMaskCount = 0;
        foreach ($entry in $entries) {
            $reader = [System.IO.StreamReader]::new($entry.Open())
            $content = $reader.ReadToEnd()
            $entryMaskCount = 0;
            foreach ($value in $values) {
                $pieces = $content -split "\b$value\b"
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

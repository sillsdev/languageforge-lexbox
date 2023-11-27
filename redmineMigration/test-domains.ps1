# Define URLs to test
$protocols = @("http", "https")
$domains = @("languageforge.org", "languagedepot.org")
$subdomains = @("hg-public", "hg-private", "resumable", "admin")

# Generate the combinations of URLs
$urls = foreach ($domain in $domains) {
    foreach ($subdomain in $subdomains) {
        foreach ($protocol in $protocols) {
            "${protocol}://$subdomain.$domain/api/healthz"
        }
    }
}

# Function to send HTTP request and check the response status
function Test-HttpResponse {
    param (
        [string]$url
    )

    try {
        $response = Invoke-WebRequest -Uri $url -Method Get -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host "[$url] Success: $($response.StatusCode)"
        } else {
            Write-Host "[$url] Error: Unexpected status code - $($response.StatusCode)"
        }
    } catch {
        Write-Host "[$url] Error: $_"
    }
}

# Run tests for each URL in the matrix
foreach ($url in $urls) {
    Test-HttpResponse -url $url
}

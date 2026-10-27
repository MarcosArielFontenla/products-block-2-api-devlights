$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

$solutionRoot = Split-Path -Parent $PSScriptRoot
$apiDirectory = Join-Path $solutionRoot 'src\SecondBlock.Products.API'
$apiDll = Join-Path $apiDirectory 'bin\Debug\net10.0\SecondBlock.Products.API.dll'
if (-not (Test-Path -LiteralPath $apiDll)) {
    throw 'Run this first: dotnet build SecondBlock.Products.API.slnx'
}

# Use an available port without interfering with the API running in Visual Studio.
$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$baseAddress = "http://127.0.0.1:$port"
$logDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('ProductsApiSmoke-' + [guid]::NewGuid())
New-Item -ItemType Directory -Path $logDirectory | Out-Null
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(5)
$apiProcess = $null
$originalAspNetEnvironment = $env:ASPNETCORE_ENVIRONMENT
$originalDotNetEnvironment = $env:DOTNET_ENVIRONMENT
$script:checks = 0

function Invoke-Api {
    param([string]$Method, [string]$Path, [string]$Body)

    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method), $baseAddress + $Path)
    $response = $null
    try {
        if ($PSBoundParameters.ContainsKey('Body')) {
            $request.Content = [System.Net.Http.StringContent]::new(
                $Body, [System.Text.Encoding]::UTF8, 'application/json')
        }
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        return [pscustomobject]@{
            Status = [int]$response.StatusCode
            Body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            Location = $response.Headers.Location
        }
    }
    finally {
        if ($null -ne $response) { $response.Dispose() }
        $request.Dispose()
    }
}

function Assert-Condition {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
    $script:checks++
}

function Assert-Status {
    param($Response, [int]$Expected, [string]$Operation)
    Assert-Condition ($Response.Status -eq $Expected) (
        "$Operation expected $Expected but received $($Response.Status): $($Response.Body)")
}

try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:DOTNET_ENVIRONMENT = 'Development'
    $apiProcess = Start-Process -FilePath (Get-Command dotnet).Source `
        -ArgumentList @("`"$apiDll`"", '--urls', $baseAddress) `
        -WorkingDirectory $apiDirectory -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $logDirectory 'stdout.log') `
        -RedirectStandardError (Join-Path $logDirectory 'stderr.log')

    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($apiProcess.HasExited) { throw 'The API exited before starting.' }
        try {
            $initial = Invoke-Api GET '/api/products'
            $ready = $true
            break
        }
        catch { Start-Sleep -Milliseconds 250 }
    }
    Assert-Condition $ready 'The API did not respond in time.'
    Assert-Status $initial 200 'Initial GET'
    Assert-Condition ($initial.Body -eq '[]') 'The initial product list must be empty.'
    Assert-Status (Invoke-Api GET '/api/products/999999') 404 'GET missing product'

    $invalidBodies = @(
        @{ Json = '{"name":"AB","price":1,"category":"Hardware"}'; Field = 'Name' }
        @{ Json = (@{ name = ('A' * 101); price = 1; category = 'Hardware' } | ConvertTo-Json); Field = 'Name' }
        @{ Json = '{"price":1,"category":"Hardware"}'; Field = 'Name' }
        @{ Json = '{"name":"Keyboard","category":"Hardware"}'; Field = 'Price' }
        @{ Json = '{"name":"Keyboard","price":null,"category":"Hardware"}'; Field = 'Price' }
        @{ Json = '{"name":"Keyboard","price":0,"category":"Hardware"}'; Field = 'Price' }
        @{ Json = '{"name":"Keyboard","price":-1,"category":"Hardware"}'; Field = 'Price' }
        @{ Json = '{"name":"Keyboard","price":1,"category":"   "}'; Field = 'Category' }
    )
    foreach ($invalid in $invalidBodies) {
        $response = Invoke-Api POST '/api/products' $invalid.Json
        Assert-Status $response 400 'Invalid POST'
        $problem = $response.Body | ConvertFrom-Json
        Assert-Condition ($problem.errors.PSObject.Properties.Name -contains $invalid.Field) (
            "Missing validation error for $($invalid.Field).")
    }
    Assert-Status (Invoke-Api POST '/api/products' '{') 400 'Malformed JSON'
    Assert-Status (Invoke-Api POST '/api/products' 'null') 400 'Body null'
    Assert-Status (Invoke-Api POST '/api/products' '') 400 'Empty body'

    $created = Invoke-Api POST '/api/products' `
        '{"id":999,"name":"Keyboard","price":150.50,"category":"Hardware","isActive":false}'
    Assert-Status $created 201 'Valid POST'
    $product = $created.Body | ConvertFrom-Json
    $path = "/api/products/$($product.id)"
    Assert-Condition ($product.id -gt 0 -and $product.id -ne 999) 'The server must assign the product ID.'
    Assert-Condition ($product.isActive -eq $true) 'New products must be active.'
    Assert-Condition ($created.Location.AbsolutePath -eq $path) 'Location must point to the product GET endpoint.'
    $found = Invoke-Api GET $path
    Assert-Status $found 200 'GET after POST'
    Assert-Condition (($found.Body | ConvertFrom-Json).name -eq 'Keyboard') 'The product was not preserved between requests.'
    $list = Invoke-Api GET '/api/products'
    Assert-Status $list 200 'GET list containing a product'
    Assert-Condition (@($list.Body | ConvertFrom-Json).Count -eq 1) 'Invalid POST requests must not save products.'

    Assert-Status (Invoke-Api PUT $path '{"name":"AB","price":0,"category":""}') 400 'Invalid PUT'
    $unchanged = (Invoke-Api GET $path).Body | ConvertFrom-Json
    Assert-Condition ($unchanged.name -eq 'Keyboard' -and $unchanged.price -eq 150.50) 'Invalid PUT changed the stored product.'
    $validUpdate = '{"name":"Monitor","price":800,"category":"Displays"}'
    $updated = Invoke-Api PUT $path $validUpdate
    Assert-Status $updated 204 'Valid PUT'
    Assert-Condition ([string]::IsNullOrEmpty($updated.Body)) 'PUT 204 must not return a body.'
    $found = Invoke-Api GET $path
    Assert-Status $found 200 'GET after PUT'
    $updatedProduct = $found.Body | ConvertFrom-Json
    Assert-Condition ($updatedProduct.name -eq 'Monitor' -and $updatedProduct.price -eq 800 `
        -and $updatedProduct.category -eq 'Displays' -and $updatedProduct.isActive) 'PUT did not save all changes.'
    Assert-Status (Invoke-Api PUT '/api/products/999999' $validUpdate) 404 'PUT missing product'

    $smallPrice = Invoke-Api POST '/api/products' '{"name":"Washer","price":0.001,"category":"Parts"}'
    Assert-Status $smallPrice 201 'Positive price below one cent'
    $smallId = ($smallPrice.Body | ConvertFrom-Json).id
    Assert-Condition ($smallId -gt $product.id) 'IDs must be unique and increasing.'

    $deleted = Invoke-Api DELETE $path
    Assert-Status $deleted 204 'DELETE existing product'
    Assert-Condition ([string]::IsNullOrEmpty($deleted.Body)) 'DELETE 204 must not return a body.'
    Assert-Status (Invoke-Api GET $path) 404 'GET after DELETE'
    Assert-Status (Invoke-Api DELETE $path) 404 'Repeated DELETE'
    Assert-Status (Invoke-Api PUT $path $validUpdate) 404 'PUT after DELETE'
    Assert-Status (Invoke-Api DELETE "/api/products/$smallId") 204 'DELETE second product'
    Assert-Condition ((Invoke-Api GET '/api/products').Body -eq '[]') 'The final product list must be empty.'

    $openApi = Invoke-Api GET '/openapi/v1.json'
    Assert-Status $openApi 200 'OpenAPI'
    $document = $openApi.Body | ConvertFrom-Json
    Assert-Condition ($document.paths.PSObject.Properties.Name -contains '/api/products/{id}') 'OpenAPI does not describe the product endpoints.'

    $swagger = Invoke-Api GET '/swagger/index.html'
    Assert-Status $swagger 200 'Swagger UI'
    Assert-Condition ($swagger.Body.Contains('swagger-ui')) 'Swagger UI markup is missing.'
    foreach ($asset in @('swagger-ui.css', 'swagger-ui-bundle.js', 'swagger-ui-standalone-preset.js')) {
        Assert-Status (Invoke-Api GET "/swagger/$asset") 200 "Swagger UI asset $asset"
    }
    Write-Output "HTTP verification passed: $script:checks checks."
}
catch {
    Write-Output "API logs: $logDirectory"
    Get-Content -LiteralPath (Join-Path $logDirectory 'stderr.log') -ErrorAction SilentlyContinue
    throw
}
finally {
    $client.Dispose()
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id
        $apiProcess.WaitForExit()
    }
    $env:ASPNETCORE_ENVIRONMENT = $originalAspNetEnvironment
    $env:DOTNET_ENVIRONMENT = $originalDotNetEnvironment
}

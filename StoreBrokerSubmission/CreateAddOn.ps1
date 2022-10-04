Param (
[Parameter(Mandatory=$true)][string]$AppId,
[Parameter(Mandatory=$true)][string]$Title,
[Parameter(Mandatory=$true)][string]$ProductType
)

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add("Content-Type", "application/json")
$headers.Add("Authorization", "Bearer ")

$body = "{
`n    `"applicationIds`": [  `"$AppId`"  ],
`n    `"productId`": `"$Title`",
`n    `"productType`": `"$ProductType`"
`n}"

$body

$response = Invoke-RestMethod 'https://manage.devcenter.microsoft.com/v1.0/my/inappproducts' -Method 'POST' -Headers $headers -Body $body
$response | ConvertTo-Json

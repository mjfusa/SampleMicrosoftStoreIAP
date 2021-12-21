Param (
[Parameter(Mandatory=$true)][string]$AppId,
[Parameter(Mandatory=$true)][string]$Title,
[Parameter(Mandatory=$true)][string]$ProductType
)

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add("Content-Type", "application/json")
$headers.Add("Authorization", "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ik1yNS1BVWliZkJpaTdOZDFqQmViYXhib1hXMCIsImtpZCI6Ik1yNS1BVWliZkJpaTdOZDFqQmViYXhib1hXMCJ9.eyJhdWQiOiJodHRwczovL21hbmFnZS5kZXZjZW50ZXIubWljcm9zb2Z0LmNvbSIsImlzcyI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0L2Q2ZmY0ODhiLTBiZjktNDE5My1hYTkyLWM2MWEyOTdlZTZmNC8iLCJpYXQiOjE2NDAwNDI2NzIsIm5iZiI6MTY0MDA0MjY3MiwiZXhwIjoxNjQwMDQ2NTcyLCJhaW8iOiJFMlpnWU5CL3VMc3kyUFduNjF2OVBSMW5IL3NFQVFBPSIsImFwcGlkIjoiYWVmMDgyZWItZjVhNC00MmI1LWE4NTYtNjkyNGY3M2RhODYwIiwiYXBwaWRhY3IiOiIxIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvZDZmZjQ4OGItMGJmOS00MTkzLWFhOTItYzYxYTI5N2VlNmY0LyIsIm9pZCI6ImVmYzA2NjlkLWE1ODUtNDY5OC05MGUyLThlM2U4ZGFjY2Y3NiIsInJoIjoiMC5BUndBaTBqXzF2a0xrMEdxa3NZYUtYN205T3VDOEs2azliVkNxRlpwSlBjOXFHQWNBQUEuIiwic3ViIjoiZWZjMDY2OWQtYTU4NS00Njk4LTkwZTItOGUzZThkYWNjZjc2IiwidGlkIjoiZDZmZjQ4OGItMGJmOS00MTkzLWFhOTItYzYxYTI5N2VlNmY0IiwidXRpIjoiYWJ6ZkR2blF0RUdJdEd3bDBMc0VBQSIsInZlciI6IjEuMCJ9.QPHmvZQWZiBuzz9A_lCfjpIyjHhYrrh3LrNYxmy5ZtooyD_JqNxSe4rwVxzZsLh8KRIm9q9GIHlsrMqe_VQDiWTyA8lJj4I57wZlUahECoVWQvmHRPo1jlDAo4QHTknfZCFqMwSr8EkVrbVjs4VitaSDx1qQeiraf-D_ggkY48ZskcleBma-lwYR0NwBgG3wlDKsuJmZ0nsm0oLSGryoWntpe07S4JG8kdVZK6xLvCApo6CDzFqDBtAAt3M6WyGIeC5WRn8Aj7qTsZ2x5BTuDnVkN4N4dkYsPUquic_vVWSELxQuqCIvJCpUSvz6hvx2WEqHnFw95gOP_bmWdmL7Ig")

$body = "{
`n    `"applicationIds`": [  `"$AppId`"  ],
`n    `"productId`": `"$Title`",
`n    `"productType`": `"$ProductType`"
`n}"

$body

$response = Invoke-RestMethod 'https://manage.devcenter.microsoft.com/v1.0/my/inappproducts' -Method 'POST' -Headers $headers -Body $body
$response | ConvertTo-Json
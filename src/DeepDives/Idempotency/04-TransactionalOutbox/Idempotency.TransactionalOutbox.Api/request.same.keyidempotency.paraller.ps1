$key = [guid]::NewGuid().ToString()

$body = @{
    description = "Concurrent expense"
    amount = 100
} | ConvertTo-Json

$job1 = Start-Job {
    param($key, $body)

    Invoke-RestMethod `
        -Uri "http://localhost:5132/expenses" `
        -Method Post `
        -Headers @{ "Idempotency-Key" = $key } `
        -ContentType "application/json" `
        -Body $body
} -ArgumentList $key, $body

$job2 = Start-Job {
    param($key, $body)

    Invoke-RestMethod `
        -Uri "http://localhost:5133/expenses" `
        -Method Post `
        -Headers @{ "Idempotency-Key" = $key } `
        -ContentType "application/json" `
        -Body $body
} -ArgumentList $key, $body

Wait-Job $job1, $job2

Receive-Job $job1
Receive-Job $job2
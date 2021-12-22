$args[0]
$args[1]

Update-InAppProductSubmission -IapId $args[0] -SubmissionDataPath (".\Durables\" + $args[1] + "\Submisson\Submission.json") -PackagePath (".\Durables\" + $args[1] + "\Submisson\Submission.zip") -Force -UpdateListings
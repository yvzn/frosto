<#
.SYNOPSIS
	Build a .env.production configuration file
.DESCRIPTION
	.env.production can be useful for building front-end assets
	if passing variables and arguments to build tools does not work
	(this is the case in some build environments)
.PARAMETER SignUpUrl
	URL of SignUp page
.PARAMETER HealthCheckUrl
	URL of HealthCheck page
.PARAMETER SupportUrl
	URL of Support page
.PARAMETER CheckSubscriptionUrl
	URL of Check Subscription page
.PARAMETER UnsubscribeUrl
	URL of Unsubscribe page
#>

param (
	[string] $SignUpUrl,
	[string] $HealthCheckUrl,
	[string] $SupportUrl,
	[string] $CheckSubscriptionUrl,
	[string] $UnsubscribeUrl
)

Write-Output "SignUpUrl: $SignUpUrl"
Write-Output "HealthCheckUrl: $HealthCheckUrl"
Write-Output "SupportUrl: $SupportUrl"
Write-Output "CheckSubscriptionUrl: $CheckSubscriptionUrl"
Write-Output "UnsubscribeUrl: $UnsubscribeUrl"

$dotEnv = Get-Content -Path .env
$dotEnvForProd = $dotEnv
$dotEnvForProd = $dotEnvForProd.Replace("VITE_SIGNUP_URL=", "VITE_SIGNUP_URL=$SignUpUrl")
$dotEnvForProd = $dotEnvForProd.Replace("VITE_HEALTHCHECK_URL=", "VITE_HEALTHCHECK_URL=$HealthCheckUrl")
$dotEnvForProd = $dotEnvForProd.Replace("VITE_SUPPORT_URL=", "VITE_SUPPORT_URL=$SupportUrl")
$dotEnvForProd = $dotEnvForProd.Replace("VITE_CHECKSUBSCRIPTION_URL=", "VITE_CHECKSUBSCRIPTION_URL=$CheckSubscriptionUrl")
$dotEnvForProd = $dotEnvForProd.Replace("VITE_UNSUBSCRIBE_URL=", "VITE_UNSUBSCRIBE_URL=$UnsubscribeUrl")

Set-Content -Path .env.production -Value $dotEnvForProd

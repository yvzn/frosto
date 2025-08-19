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
#>

param (
	[string] $SignUpUrl,
	[string] $HealthCheckUrl
)

Write-Output "SignUpUrl: $SignUpUrl"
Write-Output "HealthCheckUrl: $HealthCheckUrl"

$dotEnv = Get-Content -Path .env
$dotEnvForProd = $dotEnv
$dotEnvForProd = $dotEnvForProd.Replace("VITE_SIGNUP_URL=", "VITE_SIGNUP_URL=$SignUpUrl")
$dotEnvForProd = $dotEnvForProd.Replace("VITE_HEALTHCHECK_URL=", "VITE_HEALTHCHECK_URL=$HealthCheckUrl")

Set-Content -Path .env.production -Value $dotEnvForProd

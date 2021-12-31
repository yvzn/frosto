<#
.SYNOPSIS
	Build a .env.production configuration file
.DESCRIPTION
	.env.production can be useful for building front-end assets
	if passing variables and arguments to build tools does not work
	(this is the case in some build environments)
.PARAMETER SignUpUrl
	URL of SignUp page
#>

param (
	[string] $SignUpUrl
)

Write-Output "SignUpUrl: $SignUpUrl"

$dotEnv = Get-Content -Path .env
$dotEnvForProd = $dotEnv.Replace("VITE_SIGNUP_URL=", "VITE_SIGNUP_URL=$SignUpUrl")

Set-Content -Path .env.production -Value $dotEnvForProd

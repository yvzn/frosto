<#
.SYNOPSIS
	Build a .env.production configuration file
.DESCRIPTION
	.env.production can be useful for building front-end assets
	if passing variables and arguments to build tools does not work
	(this is the case in some build environments)
.PARAMETER HomePageUrl
	URL of Home page
#>

param (
	[string] $HomePageUrl
)

Write-Output "HomePageUrl: $HomePageUrl"

$dotEnv = Get-Content -Path .env
$dotEnvForProd = $dotEnv.Replace("VITE_HOMEPAGE_URL=", "VITE_HOMEPAGE_URL=$HomePageUrl")

Set-Content -Path .env.production -Value $dotEnvForProd

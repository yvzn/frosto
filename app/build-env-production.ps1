<#
.SYNOPSIS
	Build a .env.production configuration file
.DESCRIPTION
	.env.production can be useful for building front-end assets
	if passing variables and arguments to build tools does not work
	(this is the case in some build environments)
.PARAMETER HealthCheckUrl
	URL of HealthCheck page
.PARAMETER SupportUrl
	URL of Support page
.PARAMETER CheckSubscriptionUrl
	URL of Check Subscription page
.PARAMETER SiteFrUrl
	URL of the French version of the website
.PARAMETER SiteEnUrl
	URL of the English version of the website
#>

param (
	[string] $HealthCheckUrl,
	[string] $SupportUrl,
	[string] $CheckSubscriptionUrl,
	[string] $SiteFrUrl,
	[string] $SiteEnUrl
)

Write-Output "HealthCheckUrl: $HealthCheckUrl"
Write-Output "SupportUrl: $SupportUrl"
Write-Output "CheckSubscriptionUrl: $CheckSubscriptionUrl"
Write-Output "SiteFrUrl: $SiteFrUrl"
Write-Output "SiteEnUrl: $SiteEnUrl"

$dotEnv = Get-Content -Path .env
$dotEnvForProd = $dotEnv
$dotEnvForProd = $dotEnvForProd.Replace("VITE_HEALTHCHECK_URL=", "VITE_HEALTHCHECK_URL=$HealthCheckUrl")
$dotEnvForProd = $dotEnvForProd.Replace("VITE_SUPPORT_URL=", "VITE_SUPPORT_URL=$SupportUrl")
$dotEnvForProd = $dotEnvForProd.Replace("VITE_CHECKSUBSCRIPTION_URL=", "VITE_CHECKSUBSCRIPTION_URL=$CheckSubscriptionUrl")
$dotEnvForProd = $dotEnvForProd.Replace("VITE_SITE_FR_URL=", "VITE_SITE_FR_URL=$SiteFrUrl")
$dotEnvForProd = $dotEnvForProd.Replace("VITE_SITE_EN_URL=", "VITE_SITE_EN_URL=$SiteEnUrl")

Set-Content -Path .env.production -Value $dotEnvForProd

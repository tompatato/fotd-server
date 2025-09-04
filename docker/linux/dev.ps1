param(
	[Parameter(Position = 0, Mandatory = $true)]
	[ValidateSet("build", "test")]
	[string]$Command,

	[Parameter(Position = 1, Mandatory = $true)]
	[ValidateSet("cpp", "dotnet", "all")]
	[string]$Target,

	[Parameter(Position = 2, Mandatory = $true)]
	[ValidateSet("Debug", "Release")]
	[string]$Config,

	[Parameter(Position = 3, ValueFromRemainingArguments = $true)]
	[string[]]$ExtraArgs
)

# Locate docker-compose.yml relative to this script
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ComposeFile = Join-Path $ScriptDir "docker-compose.yml"

function Invoke-Docker {
	param(
		[string]$Service,
		[string]$Script,
		[string[]]$Args
	)
	docker compose -f $ComposeFile run --rm $Service $Script @Args
}

switch ($Command) {
	"build" {
		switch ($Target) {
			"cpp"    { Invoke-Docker "cpp-build" "build.sh" @($Config + $ExtraArgs) }
			"dotnet" { Invoke-Docker "dotnet-build" "build.sh" @($Config + $ExtraArgs) }
			"all" {
				Invoke-Docker "cpp-build" "build.sh" @($Config + $ExtraArgs)
				if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
				Invoke-Docker "dotnet-build" "build.sh" @($Config + $ExtraArgs)
			}
		}
	}
	"test" {
		switch ($Target) {
			"cpp"    { Invoke-Docker "cpp-build" "test.sh" @($Config + $ExtraArgs) }
			"dotnet" { Invoke-Docker "dotnet-build" "test.sh" @($Config + $ExtraArgs) }
			"all" {
				Invoke-Docker "cpp-build" "test.sh" @($Config + $ExtraArgs)
				Invoke-Docker "dotnet-build" "test.sh" @($Config + $ExtraArgs)
			}
		}
	}
}

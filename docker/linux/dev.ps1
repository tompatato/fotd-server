param(
	[Parameter(Position = 0, Mandatory = $true)]
	[ValidateSet("build", "test")]
	[string]$Command,

	[Parameter(Position = 1, Mandatory = $true)]
	[ValidateSet("cpp", "dotnet", "all")]
	[string]$Target,

	[Parameter(Position = 2, ValueFromRemainingArguments = $true)]
	[string[]]$Args
)

# Locate docker-compose.yml relative to this script
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ComposeFile = Join-Path $ScriptDir "docker-compose.yml"

function Invoke-Docker {
	param(
		[string]$Service,
		[string]$Script,
		[string[]]$ExtraArgs
	)
	docker compose -f $ComposeFile run --rm $Service $Script @ExtraArgs
}

switch ($Command) {
	"build" {
		switch ($Target) {
			"cpp" { Invoke-Docker "cpp-build" "build.sh" $Args }
			"dotnet" { Invoke-Docker "dotnet-build" "build.sh" $Args }
			"all" {
				Invoke-Docker "cpp-build" "build.sh" $Args
				if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
				Invoke-Docker "dotnet-build" "build.sh" $Args
			}
		}
	}
	"test" {
		switch ($Target) {
			"cpp" { Invoke-Docker "cpp-build" "test.sh" $Args }
			"dotnet" { Invoke-Docker "dotnet-build" "test.sh" $Args }
			"all" {
				Invoke-Docker "cpp-build" "test.sh" $Args
				Invoke-Docker "dotnet-build" "test.sh" $Args
			}
		}
	}
}

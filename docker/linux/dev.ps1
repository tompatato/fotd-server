param(
	[Parameter(Position=0, Mandatory=$true)]
	[ValidateSet("build","test")]
	[string]$Command,

	[Parameter(Position=1, Mandatory=$true)]
	[ValidateSet("cpp","dotnet","all")]
	[string]$Target,

	[Parameter(Position=2, Mandatory=$true)]
	[ValidateSet("Debug","Release")]
	[string]$Config,

	[Parameter(Position=3, ValueFromRemainingArguments=$true)]
	[string[]]$ExtraArgs
)

if (-not $ExtraArgs) { $ExtraArgs = @() }

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ComposeFile = Join-Path $ScriptDir "docker-compose.yml"

function Run-Docker {
	param(
		[string]$Service,
		[string]$Script,
		[string]$Config,
		[string[]]$ExtraArgs
	)

	# Build a flat array of arguments: Config first, then any extra args
	$Args = @($Config) + $ExtraArgs
	docker compose -f $ComposeFile run --rm $Service $Script @Args
}

function Should-SkipBuild {
	return $env:DEV_SKIP_BUILD -eq "1"
}

switch ($Command) {
	"build" {
		switch ($Target) {
			"cpp"    { Run-Docker "cpp-build" "build.sh" $Config $ExtraArgs }
			"dotnet" { Run-Docker "dotnet-build" "build.sh" $Config $ExtraArgs }
			"all" {
				Run-Docker "cpp-build" "build.sh" $Config $ExtraArgs
				if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
				Run-Docker "dotnet-build" "build.sh" $Config $ExtraArgs
			}
		}
	}
	"test" {
		switch ($Target) {
			"cpp" {
				if (-not (Should-SkipBuild)) {
					Run-Docker "cpp-build" "build.sh" $Config
					if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
				}
				Run-Docker "cpp-build" "test.sh" $Config $ExtraArgs
			}
			"dotnet" {
				if (-not (Should-SkipBuild)) {
					Run-Docker "dotnet-build" "build.sh" $Config
					if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
				}
				Run-Docker "dotnet-build" "test.sh" $Config $ExtraArgs
			}
			"all" {
				if (-not (Should-SkipBuild)) {
					Run-Docker "cpp-build" "build.sh" $Config
					if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

					Run-Docker "dotnet-build" "build.sh" $Config
					if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
				}
				Run-Docker "cpp-build" "test.sh" $Config $ExtraArgs
				Run-Docker "dotnet-build" "test.sh" $Config $ExtraArgs
			}
		}
	}
}

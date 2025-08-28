param(
	[Parameter(Mandatory=$true)]
	[ValidateSet("build", "test")]
	[string]$Command,

	[Parameter(Mandatory=$true)]
	[ValidateSet("cpp", "dotnet", "all")]
	[string]$Target,

	[string[]]$Args
)

switch ($Command) {
	"build" {
		switch ($Target) {
			"cpp" {
				docker compose run --rm cpp-build build.sh @Args
			}
			"dotnet" {
				docker compose run --rm dotnet-build build.sh @Args
			}
			"all" {
				# The dotnet build depends on the cpp build!
				docker compose run --rm cpp-build build.sh @Args
				if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

				docker compose run --rm dotnet-build build.sh @Args
			}
		}
	}
	"test" {
		switch ($Target) {
			"cpp" {
				docker compose run --rm cpp-build test.sh @Args
			}
			"dotnet" {
				docker compose run --rm dotnet-build test.sh @Args
			}
			"all" {
				docker compose run --rm cpp-build test.sh @Args
				docker compose run --rm dotnet-build test.sh @Args
			}
		}
	}
}

# Latest Ubuntu LTS with .NET 8 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install rsync for source syncing
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y \
	rsync \
	git \
	&& rm -rf /var/lib/apt/lists/*

# Copy build/test scripts into container
COPY build.sh /usr/local/bin/build.sh
COPY test.sh /usr/local/bin/test.sh
RUN chmod +x /usr/local/bin/build.sh /usr/local/bin/test.sh

# Working dirs
WORKDIR /src
VOLUME ["/out"]

# Default command (can be overridden in docker-compose or CLI)
CMD ["bash"]

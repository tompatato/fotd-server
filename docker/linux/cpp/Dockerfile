# Ubuntu 14.04 with GCC 4.8 for RakNet compatibility
FROM ubuntu:14.04

# Install basic tools and GCC 4.8 + CMake
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y \
    build-essential \
    gcc-4.8 g++-4.8 \
    rsync \
    git \
    wget \
    tar \
    && rm -rf /var/lib/apt/lists/*

# Set GCC/G++ 4.8 as default
RUN update-alternatives --install /usr/bin/gcc gcc /usr/bin/gcc-4.8 100 \
 && update-alternatives --install /usr/bin/g++ g++ /usr/bin/g++-4.8 100

# Install modern CMake (3.28.3 at the time of writing)
RUN wget -q https://github.com/Kitware/CMake/releases/download/v3.28.3/cmake-3.28.3-linux-x86_64.tar.gz \
	&& tar --strip-components=1 -xz -f cmake-3.28.3-linux-x86_64.tar.gz -C /usr/local \
	&& rm cmake-3.28.3-linux-x86_64.tar.gz

# Copy build/test scripts into container
COPY build.sh /usr/local/bin/build.sh
COPY test.sh /usr/local/bin/test.sh
RUN chmod +x /usr/local/bin/build.sh /usr/local/bin/test.sh

# Working dirs
WORKDIR /src
VOLUME ["/out"]

# Default command (can be overridden in docker-compose or CLI)
CMD ["bash"]

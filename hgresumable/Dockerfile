FROM ghcr.io/sillsdev/hgresume:v2023-02-24

# Install modern Mercurial (need at least 5.7 or later, image comes with 5.6.1)
RUN set -ex && \
    apt-get update && \
    apt-get install -y python3 python3-pip && \
    pip3 install --upgrade mercurial && \
    apt-get remove -y --purge \
            libpython3-dev python3-pip python3-setuptools build-essential \
            libffi-dev mercurial \
        && \
    apt-get autoremove --purge -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists

# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build

# Restore first, using only the project/props files, so this layer is cached and
# only re-runs when a .csproj or package version changes — not on every source
# edit. Keep this list in sync with FwHeadless's ProjectReference graph; a missing
# entry makes the restore below fail loudly at build time.
COPY *.props ./
COPY FwHeadless/FwHeadless.csproj FwHeadless/
COPY FixFwData/FixFwData.csproj FixFwData/
COPY LexCore/LexCore.csproj LexCore/
COPY LexData/LexData.csproj LexData/
COPY WebServiceDefaults/WebServiceDefaults.csproj WebServiceDefaults/
COPY FwLite/MiniLcm/MiniLcm.csproj FwLite/MiniLcm/
COPY FwLite/FwDataMiniLcmBridge/FwDataMiniLcmBridge.csproj FwLite/FwDataMiniLcmBridge/
COPY FwLite/LcmCrdt/LcmCrdt.csproj FwLite/LcmCrdt/
COPY FwLite/FwLiteProjectSync/FwLiteProjectSync.csproj FwLite/FwLiteProjectSync/
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore "FwHeadless/FwHeadless.csproj"

COPY . .

ARG APP_VERSION
LABEL version=$APP_VERSION

RUN --mount=type=cache,target=/root/.nuget/packages dotnet build /p:InformationalVersion=$APP_VERSION "FwHeadless/FwHeadless.csproj" -c Release -o /app/build

FROM build AS publish
# Publish RID-specific but framework-dependent (--no-self-contained relies on the shared framework
# in the base image), so NuGet copies only linux-x64 native assets instead of every RID's.
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish -r linux-x64 --no-self-contained /p:InformationalVersion=$APP_VERSION "FwHeadless/FwHeadless.csproj" -c Release -o /app/publish

# Chorus Mercurial 6.5 needs Python 3.12. aspnet:11.0 is Ubuntu 26.04 (python3 is 3.14, too new).
# Build 3.12 in a throwaway stage (same base, so glibc/ssl match); curl/gnupg/deadsnakes never
# enter the runtime image.
FROM base AS python312
RUN apt-get update \
	&& apt-get install --yes --no-install-recommends ca-certificates curl gnupg \
	&& curl -fsSL 'https://keyserver.ubuntu.com/pks/lookup?op=get&search=0xF23C5A6CF475977595C89F51BA6932366A755776' \
		| gpg --dearmor -o /etc/apt/keyrings/deadsnakes.gpg \
	&& echo "deb [signed-by=/etc/apt/keyrings/deadsnakes.gpg] https://ppa.launchpadcontent.net/deadsnakes/ppa/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") main" \
		> /etc/apt/sources.list.d/deadsnakes.list \
	&& apt-get update \
	&& apt-get install --yes --no-install-recommends python3.12 \
	&& rm -rf /var/lib/apt/lists/*

FROM base AS runtime
RUN mkdir -p /var/lib/fw-headless /var/www/.local/share && chown -R www-data:www-data /var/lib/fw-headless /var/www/.local/share
# python3 is a runtime dependency of Mercurial: the bundled Linux `hg` is a `#!/usr/bin/env python3`
# script (not a frozen binary), so hg — and therefore all Send/Receive — needs an interpreter. This
# base image (aspnet:11.0, Ubuntu 26.04) ships python3 = 3.14, but Mercurial 6.5's demandimport
# breaks on 3.12+, so instead of the distro python3 we bring in python3.12 from the python312 stage,
# plus the shared libs its stdlib extensions need (the aspnet image doesn't ship them).
RUN apt-get update \
	&& apt-get install --yes --no-install-recommends tini iputils-ping ca-certificates \
		libexpat1 zlib1g libbz2-1.0 libcrypt1 libdb5.3t64 libffi8 liblzma5 \
		libncursesw6 libreadline8t64 libsqlite3-0 libssl3t64 libtinfo6 libuuid1 \
	&& rm -rf /var/lib/apt/lists/*
COPY --from=python312 /usr/bin/python3.12 /usr/bin/python3.12
COPY --from=python312 /usr/lib/python3.12 /usr/lib/python3.12
COPY --from=python312 /etc/python3.12 /etc/python3.12
# Mercurial 6.5's demandimport breaks on 3.12+ (circular import of threading.RLock).
# Chorus spawns hg as a child, so this must be in the image env.
ENV HGDEMANDIMPORT=disable
WORKDIR /app
# ChorusMerge ships a net8.0 build; the `chorusmerge` wrapper sets DOTNET_ROLL_FORWARD=Major so it
# runs on the .NET 11 shared framework already in this base image — no separate net8 runtime needed.
#
# Copy the published app in, and in the SAME layer drop the compiled Mercurial extension modules
# (zstd et al.) built for Python versions other than our python3.12 — the package ships them for
# 3.8–3.13, but hg only ever loads the set matching its interpreter. Pruning during the copy (not
# after) actually reclaims the space instead of hiding it under a whiteout. Keyed to python3.12 so
# the hg debuginstall smoke-test below fails the build if the wrong set were ever kept.
RUN --mount=type=bind,from=publish,source=/app/publish,target=/publish \
    cp -a /publish/. /app/ \
    && pyabi="cpython-$(python3.12 -c 'import sys; print(f"{sys.version_info.major}{sys.version_info.minor}")')" \
    && find /app/Mercurial -name '*.cpython-3*-x86_64-linux-gnu.so' ! -name "*.${pyabi}-x86_64-linux-gnu.so" -delete
# Ensure exec bits were not stripped by dotnet CLI tools. FixFwData is a standalone apphost that
# liblcm shells out to at runtime for FwData repair, so it must be executable in the shipped image.
RUN chmod +x chorusmerge FixFwData && chmod +x Mercurial/hg && chmod +x Mercurial/chg 2>/dev/null || true
# Fix up mercurial.ini path to fixutf8; point hg at python3.12 (bundled shebang is python3, which is
# 3.14 on this base image — too new for Mercurial 6.5).
RUN sed -i -e 's/fixutf8 = \/FwHeadless/fixutf8 = \/app/' Mercurial/mercurial.ini \
	&& sed -i '1s|^#!.*python3.*|#!/usr/bin/env python3.12|' Mercurial/hg
# Fail loudly if ChorusMerge finds no *-ChorusPlugin.dll (sillsdev/chorus#393 / #392). Needed at
# runtime for real merges, and exercised by the chorusmerge smoke-test below.
ENV ChorusRequirePlugins=1
USER www-data:www-data
ENV XDG_DATA_HOME=/var/www/.local/share
ENTRYPOINT ["tini", "--"]
CMD ["dotnet", "FwHeadless.dll"]

# Build-time smoke tests run in a throwaway stage derived from the real runtime image, so their
# fixtures and temp churn never land in the shipped `final` image. `final` depends on this stage
# (via the marker COPY below), so buildkit must build it — and a failing smoke test fails the build.
FROM runtime AS smoketest

# Smoke-test: the bundled Mercurial is soundly installed. `hg debuginstall` loads the compiled
# extension modules and verifies the compression engines (zstd, zlib, ...), so it catches a missing
# interpreter, a broken native asset, or a mis-pruned python-ABI module set (see the prune above) —
# the foundation of every Send/Receive — before the image ships. (ui.username is set only so
# debuginstall doesn't flag its absence as a problem.)
RUN cd /app \
    && ./Mercurial/hg debuginstall --config ui.username=smoke@ci | grep -q "no problems detected" \
    && echo "hg smoke OK: $(./Mercurial/hg version | head -1)"
# Smoke-test FixFwData: FwHeadless runs under dotnet (AspNetCore shared framework) but FixFwData is a
# standalone apphost (Core only). Web publish does not copy the extra assemblies FixFwData needs
# (e.g. Logging.Abstractions); the csproj PublishFixFwData target must publish it into the same
# output. Running it here catches a broken publish early. Bind-mount the fixture so it isn't shipped.
RUN --mount=type=bind,source=Testing/test-template-repo.zip,target=/tmp/test-template-repo.zip \
    test -f FixFwData \
    && python3.12 -c "import zipfile; open('/tmp/smoke.fwdata','wb').write(zipfile.ZipFile('/tmp/test-template-repo.zip').read('kevin-test-01.fwdata'))" \
    && ./FixFwData /tmp/smoke.fwdata; ec=$?; rm -f /tmp/smoke.fwdata; \
    if [ $ec -gt 1 ]; then echo "FixFwData smoke test failed with exit $ec"; exit 1; fi
# Smoke-test: chorusmerge must engage the FieldWorks merge handler, not silently keep one whole file
# (#2508). Runs the real wrapper from a non-/app cwd, exactly as hg does. Bind-mount the fixture/script
# so they don't ship. ChorusRequirePlugins=1 is inherited from the runtime stage.
RUN --mount=type=bind,source=FwHeadless/chorusmerge-smoke,target=/opt/chorusmerge-smoke \
    bash /opt/chorusmerge-smoke/run.sh
RUN touch /tmp/.smoke-passed

FROM runtime AS final
# Pull a 0-byte marker from the smoke-test stage so buildkit must build (and pass) it before this
# image is produced, without inheriting any of its test tooling or fixtures.
COPY --from=smoketest /tmp/.smoke-passed /tmp/.smoke-passed

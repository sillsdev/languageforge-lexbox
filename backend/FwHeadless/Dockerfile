# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

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

FROM base AS runtime
RUN mkdir -p /var/lib/fw-headless /var/www/.local/share && chown -R www-data:www-data /var/lib/fw-headless /var/www/.local/share
# python3 is a runtime dependency of Mercurial: the bundled Linux `hg` is a `#!/usr/bin/env python3`
# script (not a frozen binary), so hg — and therefore all Send/Receive — needs it present.
RUN apt-get update \
	&& apt-get install --yes --no-install-recommends tini iputils-ping python3 \
	&& rm -rf /var/lib/apt/lists/*
WORKDIR /app
# ChorusMerge ships a net8.0 build; the `chorusmerge` wrapper sets DOTNET_ROLL_FORWARD=Major so it
# runs on the .NET 10 shared framework already in this base image — no separate net8 runtime needed.
#
# Copy the published app in, and in the SAME layer drop the compiled Mercurial extension modules
# (zstd et al.) built for Python versions other than this image's python3 — the package ships them
# for 3.8–3.13, but hg only ever loads the set matching /usr/bin/python3. Pruning during the copy
# (not after) actually reclaims the space instead of hiding it under a whiteout. Keyed to the live
# python3 so a base-image Python bump keeps the right set by construction; the hg debuginstall
# smoke-test below fails the build if the wrong set were ever kept.
RUN --mount=type=bind,from=publish,source=/app/publish,target=/publish \
    cp -a /publish/. /app/ \
    && pyabi="cpython-$(python3 -c 'import sys; print(f"{sys.version_info.major}{sys.version_info.minor}")')" \
    && find /app/Mercurial -name '*.cpython-3*-x86_64-linux-gnu.so' ! -name "*.${pyabi}-x86_64-linux-gnu.so" -delete
# Ensure exec bits were not stripped by dotnet CLI tools. FixFwData is a standalone apphost that
# liblcm shells out to at runtime for FwData repair, so it must be executable in the shipped image.
RUN chmod +x chorusmerge FixFwData && chmod +x Mercurial/hg && chmod +x Mercurial/chg 2>/dev/null || true
# Fix up mercurial.ini path to fixutf8
RUN sed -i -e 's/fixutf8 = \/FwHeadless/fixutf8 = \/app/' Mercurial/mercurial.ini
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
USER root
# Smoke-test: the bundled Mercurial is soundly installed. `hg debuginstall` loads the compiled
# extension modules and verifies the compression engines (zstd, zlib, ...), so it catches a missing
# python3, a broken native asset, or a mis-pruned python-ABI module set (see the prune above) — the
# foundation of every Send/Receive — before the image ships. (ui.username is set only so debuginstall
# doesn't flag its absence as a problem.)
RUN cd /app \
    && ./Mercurial/hg debuginstall --config ui.username=smoke@ci | grep -q "no problems detected" \
    && echo "hg smoke OK: $(./Mercurial/hg version | head -1)"
# Smoke-test FixFwData: FwHeadless runs under dotnet (AspNetCore shared framework) but FixFwData is a
# standalone apphost (Core only). Web publish does not copy the extra assemblies FixFwData needs
# (e.g. Logging.Abstractions); the csproj PublishFixFwData target must publish it into the same
# output. Running it here catches a broken publish early. Bind-mount the fixture so it isn't shipped.
RUN --mount=type=bind,source=Testing/test-template-repo.zip,target=/tmp/test-template-repo.zip \
    test -f FixFwData \
    && python3 -c "import zipfile; open('/tmp/smoke.fwdata','wb').write(zipfile.ZipFile('/tmp/test-template-repo.zip').read('kevin-test-01.fwdata'))" \
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

# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build

COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore "FwHeadless/FwHeadless.csproj"

ARG APP_VERSION
LABEL version=$APP_VERSION

RUN --mount=type=cache,target=/root/.nuget/packages dotnet build /p:InformationalVersion=$APP_VERSION "FwHeadless/FwHeadless.csproj" -c Release -o /app/build

FROM build AS publish
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish /p:InformationalVersion=$APP_VERSION "FwHeadless/FwHeadless.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS dotnet8

# Chorus Mercurial 6.5 needs Python 3.12. aspnet:11.0-preview is Ubuntu 26.04
# (python3 is 3.14). Build 3.12 in a throwaway stage (same base, so glibc/ssl match);
# curl/gnupg/deadsnakes never enter the runtime image.
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

FROM base AS final
RUN mkdir -p /var/lib/fw-headless /var/www/.local/share && chown -R www-data:www-data /var/lib/fw-headless /var/www/.local/share
# Shared libs python3.12 / stdlib extensions need; the aspnet image does not ship them.
# Installing python3.12 itself here would also pull distro python3 (3.14).
RUN apt-get update \
	&& apt-get install --yes --no-install-recommends tini iputils-ping ca-certificates \
		libexpat1 zlib1g libbz2-1.0 libcrypt1 libdb5.3t64 libffi8 liblzma5 \
		libncursesw6 libreadline8t64 libsqlite3-0 libssl3t64 libtinfo6 libuuid1 \
	&& rm -rf /var/lib/apt/lists/*
COPY --from=python312 /usr/bin/python3.12 /usr/bin/python3.12
COPY --from=python312 /usr/lib/python3.12 /usr/lib/python3.12
COPY --from=python312 /etc/python3.12 /etc/python3.12
WORKDIR /app
# Mercurial 6.5's demandimport breaks on 3.12+ (circular import of threading.RLock).
# Chorus spawns hg as a child, so this must be in the image env.
ENV HGDEMANDIMPORT=disable
# dotnet 8 runtime is for ChorusMerge
COPY --from=dotnet8 /usr/share/dotnet/host/fxr/ /usr/share/dotnet/host/fxr/
COPY --from=dotnet8 /usr/share/dotnet/shared/Microsoft.NETCore.App/ /usr/share/dotnet/shared/Microsoft.NETCore.App/
COPY --from=publish /app/publish .
# Ensure Chorus and Mercurial exec bits were not stripped by dotnet CLI tools
RUN chmod +x chorusmerge && chmod +x Mercurial/hg && chmod +x Mercurial/chg 2>/dev/null || true
# Fix up mercurial.ini path to fixutf8; point hg at python3.12 (shebang is python3)
RUN sed -i -e 's/fixutf8 = \/FwHeadless/fixutf8 = \/app/' Mercurial/mercurial.ini \
	&& sed -i '1s|^#!.*python3.*|#!/usr/bin/env python3.12|' Mercurial/hg
# Smoke-test: bundled hg must start (python3.12). Chorus treats hg exit 1 as
# success, so a dead interpreter looks like "no .hg" at clone time.
RUN python3.12 --version \
    && ./Mercurial/hg version \
    || { echo "hg version smoke test failed (python3.12 $(python3.12 --version 2>&1))"; exit 1; }
COPY Testing/test-template-repo.zip /tmp/test-template-repo.zip
# Smoke-test FixFwData at image build time: FwHeadless runs under dotnet (AspNetCore shared
# framework) but FixFwData is a standalone apphost (Core only). Web publish does not copy the
# extra assemblies FixFwData needs (e.g. Logging.Abstractions); the csproj PublishFixFwData
# target must publish it into the same output. Running it here catches a broken publish early.
RUN test -f FixFwData \
    && chmod +x FixFwData \
    && python3.12 -c "import zipfile; open('/tmp/smoke.fwdata','wb').write(zipfile.ZipFile('/tmp/test-template-repo.zip').read('kevin-test-01.fwdata'))" \
    && ./FixFwData /tmp/smoke.fwdata; ec=$?; rm -f /tmp/test-template-repo.zip /tmp/smoke.fwdata; \
    if [ $ec -gt 1 ]; then echo "FixFwData smoke test failed with exit $ec"; exit 1; fi
# Smoke-test: chorusmerge must engage the FieldWorks merge handler, not silently keep one
# whole file (#2508). Runs the real wrapper from a non-/app cwd, exactly as hg does.
# Bind-mount rather than COPY so the fixture/script don't ship in the runtime image.
RUN --mount=type=bind,source=FwHeadless/chorusmerge-smoke,target=/opt/chorusmerge-smoke \
    bash /opt/chorusmerge-smoke/run.sh
USER www-data:www-data
ENV XDG_DATA_HOME=/var/www/.local/share
ENTRYPOINT ["tini", "--"]
CMD ["dotnet", "FwHeadless.dll"]

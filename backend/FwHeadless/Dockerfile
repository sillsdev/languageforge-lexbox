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
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish /p:InformationalVersion=$APP_VERSION "FwHeadless/FwHeadless.csproj" -c Release -o /app/publish

FROM base AS final
RUN mkdir -p /var/lib/fw-headless /var/www/.local/share && chown -R www-data:www-data /var/lib/fw-headless /var/www/.local/share
RUN apt-get update \
	&& apt-get install --yes --no-install-recommends tini iputils-ping python3 \
	&& rm -rf /var/lib/apt/lists/*
WORKDIR /app
# ChorusMerge ships a net8.0 build; the `chorusmerge` wrapper sets DOTNET_ROLL_FORWARD=Major so it
# runs on the .NET 10 shared framework already in this base image — no separate net8 runtime needed.
COPY --from=publish /app/publish .
# Ensure Chorus and Mercurial exec bits were not stripped by dotnet CLI tools
RUN chmod +x chorusmerge && chmod +x Mercurial/hg && chmod +x Mercurial/chg 2>/dev/null || true
# Fix up mercurial.ini path to fixutf8
RUN sed -i -e 's/fixutf8 = \/FwHeadless/fixutf8 = \/app/' Mercurial/mercurial.ini
COPY Testing/test-template-repo.zip /tmp/test-template-repo.zip
# Smoke-test FixFwData at image build time: FwHeadless runs under dotnet (AspNetCore shared
# framework) but FixFwData is a standalone apphost (Core only). Web publish does not copy the
# extra assemblies FixFwData needs (e.g. Logging.Abstractions); the csproj PublishFixFwData
# target must publish it into the same output. Running it here catches a broken publish early.
RUN test -f FixFwData \
    && chmod +x FixFwData \
    && python3 -c "import zipfile; open('/tmp/smoke.fwdata','wb').write(zipfile.ZipFile('/tmp/test-template-repo.zip').read('kevin-test-01.fwdata'))" \
    && ./FixFwData /tmp/smoke.fwdata; ec=$?; rm -f /tmp/test-template-repo.zip /tmp/smoke.fwdata; \
    if [ $ec -gt 1 ]; then echo "FixFwData smoke test failed with exit $ec"; exit 1; fi
# Fail loudly if ChorusMerge finds no *-ChorusPlugin.dll (sillsdev/chorus#393 / #392); must be set
# before the chorusmerge smoke-test RUN below.
ENV ChorusRequirePlugins=1
# Smoke-test: chorusmerge must engage the FieldWorks merge handler, not silently keep one
# whole file (#2508). Runs the real wrapper from a non-/app cwd, exactly as hg does.
# Bind-mount rather than COPY so the fixture/script don't ship in the runtime image.
RUN --mount=type=bind,source=FwHeadless/chorusmerge-smoke,target=/opt/chorusmerge-smoke \
    bash /opt/chorusmerge-smoke/run.sh
USER www-data:www-data
ENV XDG_DATA_HOME=/var/www/.local/share
ENTRYPOINT ["tini", "--"]
CMD ["dotnet", "FwHeadless.dll"]

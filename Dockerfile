# syntax=docker/dockerfile:1
#
# Multi-architecture image for the GenericHost sample, built from a single Dockerfile.
# Structure follows https://github.com/f2calv/multi-arch-container-dotnet
#
# The library itself ships as a NuGet package; this image exists for the sample, which
# doubles as the Signal voice speech-to-text proving harness. The harness has a hard
# runtime dependency on ffmpeg, and shipping ffmpeg in the image is the whole reason this
# repository builds a container - it removes "install ffmpeg on the host" from the
# acceptance procedure and makes the conversion step reproducible on any machine.
#
# ------------------------------------------------------------------------------
# Stage 1 of 2: build
#
# Pinned to $BUILDPLATFORM and CROSS-COMPILES to $TARGETPLATFORM; emulating the
# target under QEMU instead is typically 10-50x slower.
# ------------------------------------------------------------------------------
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY ["Directory.Build.props", "Directory.Packages.props", "global.json", "icon.png", "./"]

ARG PROJECT=samples/GenericHost/GenericHost.csproj
# Release only. The Debug configuration project-references sibling CasCap.Common repositories
# through relative paths that deliberately do not exist inside the build context.
ARG CONFIGURATION=Release

# -- Dependency layer ----------------------------------------------------------
# Cached until a csproj/props or package version changes. Copy every project manifest first
# (--parents preserves directory structure) so editing source (.cs) files reuses the cached
# restore. Restore is platform-agnostic, so keep it before ARG TARGETARCH to share it across
# architectures.
COPY --parents samples/**/*.csproj src/**/*.csproj ./
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "$PROJECT" -p:Configuration="$CONFIGURATION"

# -- Compile layer -------------------------------------------------------------
COPY . .

# buildx injects TARGETARCH/TARGETVARIANT automatically:
#   linux/amd64 -> amd64, linux/arm64 -> arm64, linux/arm/v7 -> arm + v7
# Concatenating the two gives a single flat token to switch on.
#
# linux/arm/v7 is deliberately unsupported here: the harness targets developer machines and
# the 64-bit cluster nodes, and the Debian ffmpeg build is already the dominant cost of the
# image. Re-adding it means restoring the armv7 arm below and widening build.ps1/build.sh.
ARG TARGETARCH
ARG TARGETVARIANT
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked <<EOF
set -eux
# https://learn.microsoft.com/dotnet/core/rid-catalog
case "${TARGETARCH}${TARGETVARIANT}" in
    amd64) RID=linux-x64   ;;
    arm64) RID=linux-arm64 ;;
    *) echo "unsupported platform: linux/${TARGETARCH}/${TARGETVARIANT}" >&2; exit 1 ;;
esac
dotnet publish "$PROJECT" -c "$CONFIGURATION" -o /app/publish -r "$RID" --self-contained false
EOF

# ------------------------------------------------------------------------------
# Stage 2 of 2: final
#
# No --platform override here, so buildx resolves the base image for
# $TARGETPLATFORM and the resulting image is genuinely native to the target.
#
# Alternatives, smallest to largest:
#   mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled  no shell and no apt, so ffmpeg cannot
#                                                        be installed - disqualifying
#   mcr.microsoft.com/dotnet/aspnet:10.0-alpine          musl; ffmpeg is one `apk add`, but the
#                                                        alpine ffmpeg build differs from the
#                                                        Debian one the deployed workloads use,
#                                                        so the harness would prove a different
#                                                        decoder than production runs
#   mcr.microsoft.com/dotnet/aspnet:10.0                 full Debian, largest (used here)
#
# aspnet rather than runtime: the transitive CasCap.Common health-check package carries a
# FrameworkReference to Microsoft.AspNetCore.App, and the runtime image fails at startup with
# "No frameworks were found".
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# -- Runtime dependencies ------------------------------------------------------
# Installed before the application layers so editing source never re-runs this install.
# ffmpeg is a hard dependency, not temporary debug tooling: Signal voice notes arrive as AAC
# and a stock speech-to-text server expects 16 kHz mono signed 16-bit PCM WAV.
RUN <<EOF
set -eux
apt-get update
apt-get install -y --no-install-recommends ffmpeg
rm -rf /var/lib/apt/lists/*
ffmpeg -version
EOF

COPY --link --from=build /app/publish .

# -- Provenance ----------------------------------------------------------------
# Supplied by the CI workflow (.github/workflows/ci.yml) or by build.ps1/build.sh.
ARG GIT_REPOSITORY=n/a
ENV GIT_REPOSITORY=$GIT_REPOSITORY
ARG GIT_BRANCH=n/a
ENV GIT_BRANCH=$GIT_BRANCH
ARG GIT_COMMIT=n/a
ENV GIT_COMMIT=$GIT_COMMIT
ARG GIT_TAG=n/a
ENV GIT_TAG=$GIT_TAG

ARG GITHUB_WORKFLOW=n/a
ENV GITHUB_WORKFLOW=$GITHUB_WORKFLOW
ARG GITHUB_RUN_ID=0
ENV GITHUB_RUN_ID=$GITHUB_RUN_ID
ARG GITHUB_RUN_NUMBER=0
ENV GITHUB_RUN_NUMBER=$GITHUB_RUN_NUMBER

# https://github.com/opencontainers/image-spec/blob/main/annotations.md
LABEL org.opencontainers.image.title="CasCap.Api.SignalCli GenericHost" \
    org.opencontainers.image.description="signal-cli REST API sample worker and Signal voice speech-to-text proving harness" \
    org.opencontainers.image.source="https://github.com/f2calv/CasCap.Api.SignalCli" \
    org.opencontainers.image.licenses="Unlicense" \
    org.opencontainers.image.version="$GIT_TAG" \
    org.opencontainers.image.revision="$GIT_COMMIT"

USER $APP_UID
ENTRYPOINT ["dotnet", "GenericHost.dll"]

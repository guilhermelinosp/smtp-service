ARG LAUNCHING_FROM_VS
ARG FINAL_BASE_IMAGE=${LAUNCHING_FROM_VS:+aotdebug}

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
USER root
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
USER root
WORKDIR /src
RUN apk update && apk add --no-cache \
    build-base \
    zlib-dev \
    libgdiplus-dev \
    gcc g++ musl-dev \
    libc-dev \
    make \
    curl \
    bash \
    && rm -rf /var/cache/apk/*

COPY ["./smtp.producer.csproj", "smtp.producer/"]
WORKDIR "/src/smtp.producer"
RUN dotnet restore "smtp.producer.csproj"
COPY . .
RUN dotnet build "smtp.producer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "smtp.producer.csproj" -c Release -o /app/publish  \
    -p:UseAppHost=true \
    -p:PublishTrimmed=true \
    -p:PublishAot=true \
    --self-contained true 
        
FROM ${FINAL_BASE_IMAGE:-mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine} AS final
USER root
WORKDIR /app
EXPOSE 8080 
COPY --from=publish /app/publish .
ENTRYPOINT ["./smtp.producer"]

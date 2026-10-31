# Multi-stage Dockerfile for CitizenRadar
# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["CitizenRadar.csproj", "./"]
RUN dotnet restore "CitizenRadar.csproj"

# Copy remaining source code and publish
COPY . .
RUN dotnet publish "CitizenRadar.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install native multimedia and graphics dependencies for OpenCV and FFmpeg
RUN apt-get update && apt-get install -y --no-install-recommends \
    ffmpeg \
    libgdiplus \
    libgl1 \
    libglib2.0-0 \
    curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Default port 5000 (cloud platforms like Render, Railway, Hugging Face override via $PORT)
ENV PORT=5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "CitizenRadar.dll", "data/traffic.mp4", "models/yolov8n.onnx"]

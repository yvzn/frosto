# Frosto Orchestration Tool

A command-line tool for building and running multiple projects simultaneously with health monitoring.

## Overview

This .NET-based orchestration tool simplifies managing a multi-service architecture. It:

- Displays all available projects from `config.json` with their build/run capabilities
- Lets you select which projects to build and run
- Runs build tasks sequentially and handles failures gracefully
- Launches run tasks in separate terminal windows
- Monitors project health with progressive delay intervals (3s → 5s → 10s → 30s)
- Displays a live status dashboard showing running projects and health status

## Prerequisites

- .NET 10.0 or higher

## Configuration

The tool reads from `config.json` in the same directory. Each project entry should have:

```json
{
  "projectName": {
    "path": "../path/to/project",
    "build": "dotnet build",        // Optional: build command
    "run": "dotnet run",            // Optional: run command
    "url": "http://localhost:5000", // Optional: project URL
    "health": "http://localhost:5000/api/health" // Optional: health endpoint
  }
}
```

### Configuration Fields

| Field | Description |
|-------|-------------|
| `path` | Relative path to project directory |
| `build` | Build command (e.g., `dotnet build`, `npm run build`) |
| `run` | Run command (e.g., `dotnet run`, `npm start`) |
| `url` | Project URL (informational) |
| `health` | Health check endpoint URL for monitoring |

## Usage

### Build and Run

```bash
dotnet run
```

## Example config.json

```json
{
  "front": {
    "path": "../front",
    "run": "npm start",
    "url": "http://localhost:4200",
    "health": "http://localhost:4200"
  },
  "server": {
    "path": "../server",
    "build": "dotnet build",
    "run": "dotnet run",
    "url": "http://localhost:5066/",
    "health": "http://localhost:5066/api/health"
  },
  "batch": {
    "path": "../batch",
    "build": "dotnet build",
    "run": "func host start",
    "url": "http://localhost:7137/",
    "health": "http://localhost:7137/api/health"
  },
  "storage": {
    "path": "../db/storage",
    "run": "npm start"
  }
}
```

## Command-Line Options

```bash
dotnet run --verbose    # Show full exception details on errors
```

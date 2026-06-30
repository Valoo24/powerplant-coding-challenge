# Powerplant Coding Challenge — Solution

REST API that solves the **unit commitment problem**: given an electricity load and a set of powerplants, determine how much power each plant should produce to meet the demand at minimum cost.

The production plan is computed using a **merit-order algorithm** — plants are ranked by their real cost per MWh (wind at zero cost, then gas-fired, then turbojet) and activated in that order until the load is satisfied, while respecting each plant's Pmin and Pmax constraints.

## Tech stack

- .NET 10 / ASP.NET Core
- MediatR (CQRS pipeline)
- FluentValidation (input validation via MediatR pipeline behavior)
- xUnit (unit tests)

## Project structure

```
├── power-plant-coding-challenge-API/      # ASP.NET Core Web API (controllers, middleware)
├── power-plant-coding-challenge-core/     # Application logic (MediatR handler, validator)
├── power-plant-coding-challenge-domain/   # Domain models and enums
└── power-plant-coding-challenge-tests/    # xUnit test suite
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — for running locally
- [Docker](https://www.docker.com/) — for running in a container

## Configuration

MediatR v12+ requires a license key.

For local development, add it to `appsettings.Development.json`:

```json
{
  "MediatR": {
    "LicenseKey": "YOUR_LICENSE_KEY"
  }
}
```

For Docker, the key is injected via the `MediatR__LicenseKey` environment variable defined in `compose.yaml` — no additional setup needed.

## Build

From the `power-plant-coding-challenge/` directory:

```bash
dotnet build
```

## Run

```bash
cd power-plant-coding-challenge-API
dotnet run
```

The API starts on **http://localhost:8888**.

## Endpoint

**POST** `/productionplan`

Example request bodies are available in `../example_payloads/`.

```bash
curl -X POST http://localhost:8888/productionplan \
  -H "Content-Type: application/json" \
  -d @../example_payloads/payload1.json
```

Example response:

```json
[
  { "name": "windpark1",               "p": 90.0  },
  { "name": "windpark2",               "p": 21.6  },
  { "name": "gasfiredbig1",            "p": 368.4 },
  { "name": "gasfiredbig2",            "p": 0.0   },
  { "name": "gasfiredsomewhatsmaller", "p": 0.0   },
  { "name": "tj1",                     "p": 0.0   }
]
```

Validation errors return HTTP 400 with a `ProblemDetails` body. Unexpected errors return HTTP 500.

## Run with Docker

From the `power-plant-coding-challenge/` directory:

```bash
docker compose up --build
```

The API starts on **http://localhost:8888** (the container listens on port 8080, mapped to 8888 on the host).

To stop the container:

```bash
docker compose down
```

## Run tests

```bash
dotnet test
```

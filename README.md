# Industrial Processing System

Kolokvijum 1 — thread-safe servis za obradu poslova (producer/consumer
sa prioritetima, asinhrono izvrsavanje preko `Task<int>`, retry sa
timeout-om i event-driven log + XML izvjestaji).

## Pokretanje

```
dotnet build
dotnet run --project src/IndustrialProcessing.App
```

`config.xml` se kopira u izlazni folder pri build-u. ENTER zaustavlja
producer-e i generise finalni izvjestaj.

## Testovi

```
dotnet test --collect:"XPlat Code Coverage"
```

Rezultat posljednjeg pokretanja: `Coverage.txt` (sazetak) i
`coverage.cobertura.xml` (puni izvjestaj).

## Konfiguracija (`config.xml`)

```xml
<SystemConfig>
  <WorkerCount>5</WorkerCount>
  <MaxQueueSize>100</MaxQueueSize>
  <Jobs>
    <Job Type="Prime" Payload="numbers:10_000,threads:3" Priority="1"/>
    <Job Type="IO" Payload="delay:1500" Priority="2"/>
  </Jobs>
</SystemConfig>
```

- `WorkerCount` — broj worker niti
- `MaxQueueSize` — kapacitet reda; novi `Submit` baca `InvalidOperationException`
  kada je red popunjen
- `Jobs/Job` — pocetni poslovi koji se ucitavaju iz konfiguracije
- `Payload`:
  - Prime: `numbers:N,threads:T` — broji proste do `N`, paralelno na `T`
    niti (clamp na `[1, 8]`)
  - IO: `delay:MS` — `Thread.Sleep(MS)`, vraca random broj `[0, 100]`

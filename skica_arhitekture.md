# Tačka 1 — Skica arhitekture sistema

## 1. Pregled sistema

```
+---------------------+       net.tcp        +---------------------+      fajl sistem      +-----------------------+
|                     |                      |                     |                       |                       |
|   KLIJENT           |   StartSession       |   WCF SERVIS        |    write/append       |   Data/<VehicleId>/   |
|   (Console App)     | -------------------> |   (Console Host)    | --------------------> |   <YYYY-MM-DD>/       |
|                     |   PushSample x N     |                     |                       |     session.csv       |
|                     | -------------------> |                     |                       |     rejects.csv       |
|                     |   EndSession         |                     |                       |                       |
|                     | -------------------> |                     |                       |                       |
+---------------------+                      +---------------------+                       +-----------------------+
        |                                              |
        | čita                                         | piše analitiku
        v                                              v
+---------------------+                      +---------------------+
| Charging_Profile.csv|                      |    transfer.log     |
| (1 od 12 foldera)   |                      |    (eventi)         |
+---------------------+                      +---------------------+
```

## 2. Komponente

```
KLIJENT  (projekat Client)                       SERVER  (projekat Server)
+----------------------------------+              +------------------------------------+
|  Program.cs                      |              |  Program.cs                        |
|   - lista 12 foldera             |              |   - kreira ServiceHost             |
|   - korisnik bira vozilo         |              |   - pretplata na evente            |
|   - poziva StartSession,         |              |   - upis u transfer.log            |
|     PushSample (red po red),     |              |                                    |
|     EndSession                   |              |  ChargingService                   |
|                                  |              |   - StartSession (otvara fajlove)  |
|  ChargingClient                  |              |   - PushSample (deserializacija    |
|   - ChannelFactory<IChargingSrv> |              |     bajtova, validacija, upis,     |
|   - serijalizacija ChargingData  |              |     analitika)                     |
|     u byte[] preko SampleOptions |              |   - EndSession (zatvara fajlove)   |
|   - Dispose pattern (Abort/Close)|              |   - IDisposable nad FileStream     |
|                                  |              |     i StreamWriter                 |
|  CsvReader                       |              |                                    |
|   - StreamReader nad CSV-om      |              |  TransferEvents.cs                 |
|   - InvariantCulture parse       |              |   - OnTransferStarted              |
|   - yield return red po red      |              |   - OnSampleReceived               |
|   - log neispravnih redova       |              |   - OnTransferCompleted            |
|   - Dispose pattern              |              |   - OnWarningRaised                |
+----------------------------------+              +------------------------------------+

                COMMON  (Class Library, vidljiv obema stranama)
                +------------------------------------------------+
                |  IChargingService    [ServiceContract]         |
                |     - StartSession                             |
                |     - PushSample                               |
                |     - EndSession                               |
                |                                                |
                |  ChargingData        [DataContract]            |
                |     - Timestamp, VehicleId, RowIndex           |
                |     - Voltage/Current/Real/Reactive/Apparent   |
                |       Power i Frequency (Min, Avg, Max)        |
                |     - ToBytes() / FromBytes() preko            |
                |       MemoryStream + BinaryWriter/Reader       |
                |                                                |
                |  SampleOptions       [DataContract]            |
                |     - byte[] Data                              |
                |     - IDisposable                              |
                |                                                |
                |  ChargingFault       [DataContract]            |
                |     - Message                                  |
                +------------------------------------------------+
```

## 3. Sekvenca razmene poruka

```
KLIJENT                                                                          SERVER
  |                                                                                |
  |  1) Korisnik bira jedno od 12 vozila iz foldera                                |
  |     Otvaranje Charging_Profile.csv                                             |
  |                                                                                |
  |  2) StartSession(vehicleId)                                                    |
  |  ----------------------------------------------------------------------------> |
  |                                                                       Kreiranje:
  |                                                                  Data/<Veh>/<Dan>/
  |                                                                  session.csv, rejects.csv
  |                                                                  Raise OnTransferStarted
  |                                                                                |
  |  3) Petlja po redovima CSV-a:                                                  |
  |     ChargingData -> ToBytes() -> SampleOptions                                 |
  |     PushSample(options)                                                        |
  |  ----------------------------------------------------------------------------> |
  |                                                                  FromBytes(options.Data)
  |                                                                  Validacija (Timestamp,
  |                                                                    Voltage>0, Frequency>0)
  |                                                                  Ako ne valja -> rejects.csv
  |                                                                    + FaultException
  |                                                                  Inace -> session.csv
  |                                                                  Analitika (struja,
  |                                                                    reaktivna/prividna snaga)
  |                                                                  Raise OnSampleReceived
  |                                                                  Eventualno OnWarningRaised
  |                                                                                |
  |     <----------------------------------------------------------- void / Fault  |
  |                                                                                |
  |     (ponavlja se za svaki red)                                                 |
  |                                                                                |
  |  4) EndSession(vehicleId)                                                      |
  |  ----------------------------------------------------------------------------> |
  |                                                                  Zatvaranje session.csv
  |                                                                    i rejects.csv
  |                                                                  Raise OnTransferCompleted
  |                                                                                |
  |  5) Klijent upisuje parse_errors.log (ako ima)                                 |
  |     Dispose nad ChargingClient i CsvReader                                     |
  |                                                                                |
```

## 4. Struktura fajl sistema

### Klijent (ulaz)

```
Data/EV-CPW Dataset/                <-- DataPath iz App.config
    Marka_Model_1/
        Charging_Profile.csv
    Marka_Model_2/
        Charging_Profile.csv
    ...
    Marka_Model_12/
        Charging_Profile.csv
        parse_errors.log            <-- ako bude neispravnih redova
```

### Server (izlaz)

```
Data/                               <-- DataPath iz App.config (relativno u bin/Debug)
    <VehicleId>/
        <YYYY-MM-DD>/
            session.csv             <-- prihvaceni redovi, append
            rejects.csv             <-- odbijeni redovi + razlog
transfer.log                        <-- zapisi svih dogadjaja
```

## 5. WCF Endpoint

| | |
|---|---|
| Adresa | `net.tcp://localhost:4000/ChargingService` |
| Binding | `netTcpBinding` (`transferMode="Buffered"`, `maxReceivedMessageSize=10485760`) |
| Contract | `Common.IChargingService` |
| Security | `None` (lokalna mreza) |
| Timeout | 10 minuta send/receive |

## 6. Format jedne poruke (PushSample)

Klijent svaki `ChargingData` red konvertuje u niz bajtova preko `MemoryStream` + `BinaryWriter` (zadatak 7) i zatim ga pakuje u `SampleOptions` koji se serijalizuje kao `[DataContract]`:

```
ChargingData (in-memory objekat)
        |
        | ToBytes()
        v
+--------------------------------------------------------------+
| Timestamp.Ticks (8B) |  18 x double (8B = 144B)              |
|                      |  Voltage/Current/Real/Reactive/       |
|                      |  Apparent/Frequency  -> Min,Avg,Max   |
|----------------------+---------------------------------------|
| RowIndex (4B)        |  VehicleId (length-prefixed UTF-8)    |
+--------------------------------------------------------------+
        |
        | wrap u SampleOptions { byte[] Data }
        v
   posiljka preko WCF-a (jedan Sample po poruci)
```

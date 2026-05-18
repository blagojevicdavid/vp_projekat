# Tacka 1 — Skica arhitekture sistema

```
+------------------+        net.tcp (WCF)        +------------------+
|                  |  -- StartSession(vozilo) -> |                  |
|     KLIJENT      |  -- PushSample(red) ------> |     SERVER       |
|  (Console App)   |  -- PushSample(red) ------> |  (Console Host)  |
|                  |  -- ...                     |                  |
|                  |  -- EndSession(vozilo) ---->|                  |
+------------------+                             +------------------+
        |                                                 |
        | cita                                            | pise
        v                                                 v
+------------------+                             +------------------+
| Charging_Profile |                             | Data/vozilo/     |
|     .csv         |                             |   session.csv    |
| (1 od 12 foldera)|                             |   rejects.csv    |
+------------------+                             +------------------+


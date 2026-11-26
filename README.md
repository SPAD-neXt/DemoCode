# DemoCode
Demo Code / Snippets for SPAD.neXt Serial/Remote Interfaces

| Folder | Content |
|---|---|
| `WebSocket/GaugeDemo-JS` | HTML/JS gauges fed by the SPAD.neXt data subscription (`ws://<pc>:28001/data`) |
| `SerialV2/RemoteSerialDemo` | A serial device over WebSocket instead of a COM port (`ws://<pc>:28001/serialapi/...`) |
| `Addons`, `Scripts`, `VoiceAttack` | Addon, script and VoiceAttack samples |

## Server-Sent Events are gone

SPAD.neXt no longer supports Server-Sent Events (`/event-stream`). The former `ServerSentEvents` samples are removed:

- Data subscription (`/data/subscribe/{Session}` ...): use the WebSocket `ws://<pc>:28001/data`, see `WebSocket/GaugeDemo-JS`.
- Serial API (`/serialapi/{Session}/connect` ...): use the WebSocket `ws://<pc>:28001/serialapi/<deviceGuid>/SPAD/<instance>`, see `SerialV2/RemoteSerialDemo`.

## Data subscription over WebSocket

Connect to `ws://<pc>:28001/data`. From another computer add `?apikey=<your api key>`; on the same computer no key is needed.
All messages are text messages with one JSON object.

You send:

| `cmd` | Fields | Meaning |
|---|---|---|
| `subscribe` | `dataRefs`: list of names | Subscribe to data. The current values are sent right away. |
| `unsubscribe` | `dataRefs`: list of names, empty = all | Stop receiving data. |
| `config` | `interval`: milliseconds | Changes are collected and sent every 500 ms by default. `0` sends every change at once. |
| `requestupdate` | | Send all subscribed values again. |

Add an `id` to a command and the reply carries the same `id`.

SPAD.neXt sends:

| `type` | Fields | Meaning |
|---|---|---|
| `connected` | `version`, `interval` | The connection is ready. |
| `reply` | `cmd`, `id`, `success`, `message`; for `subscribe` also `subscribed`, `failed` | Answer to a command. |
| `update` | `data`: name and value pairs | Changed values. |

```
-> {"cmd":"subscribe","id":1,"dataRefs":["A:INDICATED ALTITUDE,FEET","LOCAL:MYVAR"]}
<- {"type":"reply","cmd":"subscribe","id":1,"success":true,"subscribed":["A:INDICATED ALTITUDE,FEET","LOCAL:MYVAR"],"failed":[]}
<- {"type":"update","data":{"A:INDICATED ALTITUDE,FEET":1250.4,"LOCAL:MYVAR":0.0}}
```

Subscriptions end when the connection is closed. After a reconnect subscribe again.

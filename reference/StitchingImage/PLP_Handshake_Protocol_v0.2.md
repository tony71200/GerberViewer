
# PLP Handshake Protocol Specification (v0.2)

## 1. Overview
This document defines the application-level handshake protocol between **AH01** (controller/Client PC) and **ISP** (Image Stitching Process: Host).  
The protocol is JSON-based and is used to:
- Check connection status (Heartbeat)
- Trigger and control image stitching processes
- Exchange acknowledgements, results, and error information

---

## 2. Participants
- **AH01**: Client PC / Robot controller that sends commands
- **ISP**: Host: Image Stitching Process that executes tasks and returns responses

---

## 3. Task List

| Task | Direction | Description |
|----|----|----|
| Heartbeat | AH01 → ISP | Check if ISP connection is alive |
| Stitching | AH01 → ISP | Trigger the image stitching process |

---

## 4. Command and Respond Naming Convention

### 4.1 Command Format (AH01 → ISP)
```
<PREFIX>.<TASK_ID>.<ACTION>
```
Example:
- `AH01.LINK.START`
- `AH01.STITCH.START`
- `AH01.STITCH.END`

### 4.2 Respond Format (ISP → AH01)
```
<PREFIX>.<TASK_ID>.<ACTION>
```
Example:
- `ISP.STITCH.ACK`
- `ISP.STITCH.RESULTS`

---

## 5. Actions Definition

| Action | Description |
|----|----|
| START | Request to start a task |
| ACK | Acknowledge received command |
| RESULTS | Return processing results |
| END | Inform task completion |
| ERROR | Return error information |

---

## 6. ACK Status

| ACK Value | Meaning |
|----|----|
| OK | ISP is ready |
| BUSY | ISP is busy |
| ERROR | ISP encountered an error |

---

## 7. Error Code Definition

### 7.1 Connection / Shared Folder Errors (CONXXX)
| Code | Description |
|----|----|
| CON001 | No TCP/IP connection available |
| CON002 | Shared folder not found |
| CON003 | Shared folder access timed out |
| CON004 | Shared folder unavailable or IO error |
| CON005 | Shared folder access denied |

### 7.2 Stitching / Data Errors (DATXXX)
| Code | Description |
|----|----|
| DAT001 | Shared folder has no images |
| DAT002 | Shared folder has insufficient images |
| DAT003 | Image read failed |
| DAT004 | Invalid image format |
| DAT005 | Image save failed |
| DAT006 | Stitching failed |
| DAT007 | Stitching cancelled |
| DAT008 | Image count mismatch |

---

## 8. AH01 → ISP JSON Command Format

### 8.1 General Structure
```json
{
  "cmd": "<AH01.TASK.ACTION>",
  "message_id": <int>,
  "source": "<string>",
  "destination": "<string>",
  "task": "<string>",
  "action": "<string>",
  "timestamp": "<ISO-8601 datetime>",
  "data": { }
}
```

---

## 9. Stitching Data Payload

### 9.1 Structure
```json
{
  "shared_folder": "<string>",
  "rows": <int>,
  "columns": <int>,
  "group_id": <int>,
  "start_point": <int>,
  "direction": <int>,
  "overlap": <float>,
  "resolution": <float>
}
```

### 9.2 Field Description

| Field | Type | Description |
|----|----|----|
| shared_folder | string | Path to shared folder containing input images |
| rows | int | Number of rows in image grid |
| columns | int | Number of columns in image grid |
| group_id | int | Image group identifier |
| start_point | int | Robot start point (0:TL, 1:TR, 2:BL, 3:BR) |
| direction | int | Robot movement direction (0:Left, 1:Right, 2:Down, 3:Up) |
| overlap | float | Overlap ratio between adjacent images |
| resolution | float | Physical resolution (e.g. mm/pixel or µm/pixel) |

---

## 10. Example: Start Stitching Command
```json
{
  "cmd": "AH01.STITCH.START",
  "message_id": 1001,
  "source": "AH01",
  "destination": "ISP",
  "task": "STITCH",
  "action": "START",
  "timestamp": "2025-12-12T10:00:00",
  "data": {
    "shared_folder": "D:/Images",
    "rows": 3,
    "columns": 10,
    "group_id": 1,
    "start_point": 1,
    "direction": 1,
    "overlap": 0.2,
    "resolution": 0.005
  }
}
```

---

## 11. ISP → AH01 JSON Respond Format

### 11.1 General Structure
```json
{
  "respond": "<ISP.TASK.ACTION>",
  "message_id": <int>,
  "source": "<string>",
  "destination": "<string>",
  "task": "<string>",
  "action": "<string>",
  "timestamp": "<ISO-8601 datetime>",
  "data": {
    "status": <int>,
    "errors": "<string>"
  }
}
```

### 11.2 Status Definition
| Status | Meaning |
|----|----|
| 0 | Success |
| 1 | Failed |

---

## 12. Example: Stitching Result Respond (Failed)
```json
{
  "respond": "ISP.STITCH.RESULTS",
  "message_id": 1001,
  "source": "ISP",
  "destination": "AH01",
  "task": "STITCH",
  "action": "RESULTS",
  "timestamp": "2025-12-12T10:00:00",
  "data": {
    "status": 1,
    "errors": "CON001"
  }
}
```

---

## 13. Example: ACK Respond
```json
{
  "respond": "ISP.STITCH.ACK",
  "message_id": 1001,
  "source": "ISP",
  "destination": "AH01",
  "task": "STITCH",
  "action": "ACK",
  "timestamp": "2025-12-12T10:00:00",
  "data": {
    "ACK": "OK",
    "errors": ""
  }
}
```

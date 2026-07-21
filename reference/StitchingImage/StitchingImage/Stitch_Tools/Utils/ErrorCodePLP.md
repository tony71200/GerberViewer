# Error Code Reference (PLP / Connection)

This document summarizes error codes used by the PLP application and TCP/IP protocol responses.

## Connection Errors (ErrorCodeConnection)

| Code | Meaning |
| --- | --- |
| CON001 | No TCP/IP connection available |
| CON002 | Shared folder not found |
| CON003 | Shared folder access timed out |
| CON004 | Shared folder unavailable or IO error |
| CON005 | Shared folder access denied |

## PLP Processing Errors (ErrorCodePLP)

| Code | Meaning |
| --- | --- |
| DAT001 | Shared folder has no images |
| DAT002 | Shared folder has insufficient images |
| DAT003 | Image read failed |
| DAT004 | Invalid image format |
| DAT005 | Image save failed |
| DAT006 | Stitching failed |
| DAT007 | Stitching cancelled |
| DAT008 | Image count mismatch |

## Main/Application Errors (ErrorCodePLP)

| Code | Meaning |
| --- | --- |
| MAI001 | Application startup failed |
| MAI002 | UI thread exception |
| MAI003 | Unhandled exception |

## Function/Runtime Errors (ErrorCodePLP)

| Code | Meaning |
| --- | --- |
| FUN001 | HALCON libraries missing |
| FUN002 | HALCON stitching failed |
| FUN003 | Fallback stitching failed |

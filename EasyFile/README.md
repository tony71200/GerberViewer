# EasyFile

EasyFile centralizes file-format models and JSON helpers for inspection and bin-code data.

| File | Class/Function | Purpose |
| --- | --- | --- |
| JsonUtilities.cs | JsonConveter | Provides JSON serialization and deserialization helpers. |
| JsonUtilities.cs | JFile | Reads and writes JSON-backed files. |
| FileFormats.cs | IEFile | Defines common operations for supported inspection file formats. |
| FileFormats.cs | ContrelFile | Implements parsing or writing for control-format files. |
| FileFormats.cs | SinfFileFormat | Implements parsing or writing for SINF inspection map files. |
| BinCodeEditor.cs | BinCodeEditor | Edits bin-code tables used to classify inspection results. |
| Models.cs | BinCodeTable / BinInfo / BinType | Model bin-code definitions, bin metadata, and bin categories. |
| Models.cs | Judgment / JudgmentParms | Represent inspection judgment results and judgment parameters. |
| Models.cs | CompleteInspectionResult / InspectionResult | Store complete and per-item inspection result data. |
| Models.cs | HeaderInfo / FileType | Describe file headers and supported file-type identifiers. |

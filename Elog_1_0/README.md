# Elog_1_0

Elog_1_0 provides compatibility logging and simple file helpers for projects that referenced the original Elog assembly.

| File | Class/Function | Purpose |
| --- | --- | --- |
| Elog.cs | Elog | Writes categorized log messages to a dated text file and optional WinForms list box. |
| Elog.cs | Elog.SetOpenFile | Enables file logging and configures the log folder and base file name. |
| Elog.cs | Elog.SetOpenListBox | Enables list-box logging and installs owner-draw coloring support. |
| Elog.cs | Elog.SetDeleteFile | Deletes old files from a folder according to a retention period. |
| Elog.cs | Elog.WriteInfo / WriteWarning / WriteError | Writes messages with the matching log level and configured color. |
| Elog.cs | Elog.Dispose | Detaches list-box drawing events when logging is no longer needed. |
| EasyFile.cs | EasyFile | Singleton helper for UTF-8 text-file and directory operations. |
| EasyFile.cs | EasyFile.ReadFile / WriteFile / AppendFile | Reads, writes, or appends text file content. |
| EasyFile.cs | EasyFile.MakeDirectory / DropDirectoryAllFile | Creates folders and clears top-level files from a folder. |

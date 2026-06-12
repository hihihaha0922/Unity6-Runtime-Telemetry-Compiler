# Unity 6 Runtime Telemetry Compiler & UI Analyzer

A high-performance, native Unity 6 engine extension designed to recursively audit active UI Toolkit (`UITK`) hierarchies, monitor managed heap memory trends, and stream performance diagnostics using asynchronous binary serialization.

## 📺 System Demonstration
![Runtime Telemetry Compiler Demo]



https://github.com/user-attachments/assets/0a705183-3921-4f94-9a88-722933d69fc1



---

## 📋 Architectural Overview

In large-scale RPG development (e.g., massive inventory matrices, nesting dialogue trees, and sprawling HUD systems), deeply nested user interfaces impose a severe rendering burden on the CPU main thread. 

This tool acts as an **in-engine flight recorder**. It tracks interaction and layout transformation costs frame-by-frame, instantly visualizing structural rendering weight through a color-coded **Thermal Budget Map** directly within the Unity Editor and via a custom Runtime HUD interface.

---

## 🛠️ Core Technical Features

### 1. Asynchronous Multi-Threaded I/O Pipeline
To ensure that profiling intensive diagnostic data does not trigger editor main-thread stutters or lockups for developers, the compiler utilizes the **.NET Task-based Asynchronous Pattern (TAP)**. 
* Clicking compile forks the thread execution path using `Task.Run()`.
* The main game loop instantly resets its local cache buffer snapshot to continue running at a fluid 60 FPS while a background CPU core processes disk serialization.

### 2. Low-Overhead Binary Serialization Stream
Rather than relying on heavy, string-allocated text structures like JSON or XML—which pollute the managed heap and trigger the Garbage Collection—this tool utilizes a fast **`BinaryWriter` stream** channel.
* Telemetry frames are packed into raw byte segments directly onto the disk sector.
* Files are stamped with a unique 4-byte Magic Signature (`L`, `A`, `R`, `I`) to secure data validation across automated studio build pipelines.

### 3. Recursive Tree-Walking Analytics
The analyzer features an optimized tree-traversal algorithm that hooks into the layout framework via `element.hierarchy.Children()`. It calculates layout density by analyzing two key metrics:
* **Active Element Density:** The exact volume of active drawing elements rendered on screen.
* **Maximum Tree Depth Pointer:** The deepest nesting layer threshold registered from the layout root down to the lowest active leaf node.

---

## 📊 Telemetry Data Flow Framework
[ Live Game Loop Pass ]
│
▼ (Tracks DeltaTime, GC Heap, UITK Elements)
[ Local System RAM Cache Buffer ]
│
▼ (Clones Snapshot & Forks Thread execution)
─── Task.Run() ───►  [ Background CPU Core Worker Thread ]
│
▼ (Low-overhead Byte Streaming)
[ Disk Block Sector (.bin) ]
│
▼ (OpenFilePanel Parser Loop)
[ Studio Analyzer Dashboard ]
---

## 📝 Setup & Implementation Guide

### Required Scripts
1. **`ProfilerTelemetryData.cs`**: The data struct defining the metrics profile tracking blueprint.
2. **`PerformanceProfilerCompiler.cs`**: The underlying engine processing data caching, asynchronous threading loops, and raw stream reading/writing.
3. **`AAAProfilerWindow.cs`**: The custom Unity Editor Toolkit presentation canvas providing the control console, log analyzer, and visual structural map.
4. **`UiPerformanceHudOverlay.cs`**: The runtime component that can be added to any in-scene `UIDocument` to display diagnostic telemetry overlay arrays dynamically on the game screen.

### Running the Profiler Sandbox
1. Attach the `UiStressTestSimulator.cs` component to any scene GameObject to dynamically inject a nested layout structure for stress testing.
2. Launch the tool from the top menu dropdown bar: **`Larian Workflow Suite` ➔ `Runtime Profiler & Compiler`**.
3. Initialize the live recording, capture engine data loops, export the binary files, and execute the analytical diagnostic overview to parse layout bottlenecks instantly.

---

## 💡 Key Architectural Optimizations (For Technical Interviews)
* **The Sawtooth Allocation Loop:** The current configuration generates a predictable memory sawtooth pattern (800 MB ➔ 1200 MB ➔ 800 MB) as temporary string updates fill up the managed heap before triggering .NET Garbage Collection sweeps.
* **Production Scaling Plan:** To run completely allocation-free in a live studio environment, the rendering pipeline can be refactored to use **Object Pooling** for the custom text labels, pre-allocated **`StringBuilder`** caches, and unmanaged **`ReadOnlySpan<char>`** slicing blocks to prevent temporary garbage generation entirely.

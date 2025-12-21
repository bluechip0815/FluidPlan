# FluidSimu - A C# Pneumatic Network Simulator

FluidSimu is a lightweight, console-based simulator for modeling the dynamic behavior of pneumatic systems. It allows users to define a network of components like pipes, valves, tanks, and pressure regulators in a simple JSON format and simulate the pressure and flow distribution over time.

The project is designed to be extensible and supports two primary modes of operation: a **profile-based mode** for running predefined scenarios and an **interactive mode** for real-time control and monitoring.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## ✨ Key Features

*   **Component-Based Modeling**: Define complex pneumatic circuits using simple JSON files.
*   **Dynamic Simulation**: Uses a time-step-based approach to solve for pressure changes in the network.
*   **Two Execution Modes**:
    *   **Profile Mode**: Run a complete, scripted simulation based on an `executionProfile.json` file.
    *   **Interactive Mode**: Control valves and setpoints live from the command line for testing and analysis.
*   **Visual Outputs**: Automatically generates a schematic of the pneumatic circuit (using Graphviz) and a pressure-over-time chart (using ScottPlot) for profile-based simulations.
*   **Extensible Architecture**: Easily add new types of pneumatic elements by implementing the `IPneumaticElement` interface.

## ⚙️ Getting Started

### Prerequisites

To build and run this project, you will need:

1.  **.NET 8.0 SDK** (or newer).
2.  **Graphviz**: This is required to generate the schematic diagrams.
    *   Install it from the official [Graphviz website](https://graphviz.org/download/).
    *   **Crucially**, ensure the Graphviz `bin` directory is added to your system's `PATH` environment variable so that `dot.exe` can be found by the simulator.

### Installation & Setup

1.  **Clone the repository:**
    ```bash
    git clone <your-repository-url>
    cd FluidSimu
    ```

2.  **Restore dependencies:**
    ```bash
    dotnet restore
    ```

3.  **Build the project:**
    ```bash
    dotnet build
    ```

## 🚀 Usage

The simulator can be run in two different modes from the command line.

### 1. Profile-Based Mode

This mode runs a full simulation based on a model file and an execution profile. It's ideal for running reproducible experiments and generating final results.

**Command:**
```bash
dotnet run <path-to-model.json> <path-to-executionProfile.json>
```
If file paths are omitted, it defaults to `model.json` and `executionProfile.json`.

**Example:**
```bash
dotnet run model.json executionProfile.json
```

**Output:**
The simulation will run to completion or until the `hardTimeLimit` is reached. All outputs are saved to a new directory inside the `output/` folder, named after the model. This includes:
*   `model.json` & `executionProfile.json` (a copy for reproducibility).
*   `simulation_result.csv`: A log of pressures for each element over time.
*   `<ModelName>_schema.png`: A visual schematic of the pneumatic circuit.
*   `<ModelName>_chart.png`: A plot showing the pressure curves for all elements.

### 2. Interactive Mode

This mode allows you to start a simulation and control your "actor" elements (Valves, EPUs) in real-time. It's perfect for debugging models, testing component interactions, and what-if analysis.

**Command:**
```bash
dotnet run -- --interactive <path-to-model.json>```
*Note: The `--` is important. It separates arguments for `dotnet` from arguments for your application.*

**Example:**
```bash
dotnet run -- --interactive model.json
```

**Interactive Commands:**
Once running, you can issue commands in the console:

| Command                 | Description                                                              | Example                    |
| ----------------------- | ------------------------------------------------------------------------ | -------------------------- |
| `run <steps>`           | Executes the simulation for a given number of time steps.                | `run 1000`                 |
| `<ElementName> <Value>` | Sets the control value for a controllable element (Valve state or EPU pressure). | `V1 1` or `EPU 3.5`      |
| `status`                | Displays the current pressure of all elements marked as `"visible": "true"`. | `status`                   |
| `quit`                  | Exits the interactive simulation.                                        | `quit`                     |


## 📂 Project Structure

*   `/Program.cs`: The main entry point, handling command-line arguments and orchestrating the selected simulation mode.
*   `/PneumaticModel.cs`: The core simulation engine. Responsible for creating elements, managing connections, and executing the simulation loop.
*   `/Elements/`: Contains the implementation for all pneumatic components (`PipeElement.cs`, `ValveElement.cs`, `TankElement.cs`, etc.).
*   `/Connector.cs`: Implements the logic for how connected elements interact and exchange charge (air).
*   `/FlowPhysics.cs`: A static class containing the core mathematical formulas for calculating fluid flow.
*   `/IControllable.cs`: The interface for elements that can be controlled in interactive mode.
*   `/ResultVisualizer.cs`: Uses the ScottPlot library to generate charts from the simulation results.
*   `/SchemaGenerator.cs`: Uses Graphviz to generate a visual schematic of the model.

## 📄 Modeling Concepts

### `model.json`
This file defines the physical structure of the pneumatic circuit.

*   `"elements"`: A list of all components in the system. Each element has a `name`, `type`, and a dictionary of `parameters`.
    *   To monitor an element's pressure in interactive mode, add `"visible": "true"` to its parameters.
*   `"connections"`: A list of strings defining how elements are connected.
    *   Symmetrical connections: `"Supply, R1"`
    *   Directional connections (for check valves): `"R2 > CV1"`

### `executionProfile.json`
This file defines the script for a **profile-based** simulation.

*   `"timeStepSeconds"`: The duration of each simulation step (e.g., `0.001` for 1ms).
*   `"valveTimelines"`: Defines when valves open (`1.0`) and close (`0.0`).
*   `"epuTimelines"`: Defines the target pressure setpoints for EPUs over time.

## 🤝 Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## 📜 License

This project is licensed under the MIT License - see the `LICENSE.md` file for details.
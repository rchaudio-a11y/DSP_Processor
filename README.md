# **Cognitive Engine — Recursive Development Framework (RDF)**  
*A modular narrative‑driven cognitive system built through recursive refinement, early validation, and architectural clarity.*

---

## **Overview**
This project implements a multi‑layer cognitive engine capable of generating, storing, analyzing, and interpreting narrative events across time. It includes:

- A dynamic attention model  
- Multi‑layer narrative memory  
- Entity tracking and trajectory analysis  
- Anomaly detection  
- Meta‑narrative generation  
- System health scoring  
- A recursive development methodology (RDF)

The system is built in VB.NET and structured for clarity, extensibility, and long‑term maintainability.

---

## **Core Features**

### **🧠 ProgramCognition (Phase 6.5)**
Implements the cognitive engine’s reasoning layer, including:

- Dynamic attention window  
- Raw and smoothed narrative rates  
- Importance scoring  
- Hysteresis‑based voice shifting  
- Memory distribution analysis  
- Entity awareness  
- Meta‑narrative generation  
- System health evaluation  
- Anomaly detection  

Phase 6.5 is fully implemented and tested.

---

### **📚 CognitiveMemory**
A structured memory layer supporting:

- Entity queries  
- Spatial queries  
- Temporal queries  
- Layer‑based narrative retrieval  
- Episodic and reassessment narratives  
- Memory completeness tracking  

This layer provides the factual substrate for ProgramCognition.

---

### **🧩 Entity Tracking**
Entities include:

- Unique IDs  
- Type and tier  
- Bounding boxes  
- Trajectories  
- Alive/dead state  
- Importance scores  
- First/last seen generations  

Entity data feeds into anomaly detection, attention modeling, and meta‑narratives.

---

### **📊 Narrative Layers**
Narratives are categorized into four layers:

- `EventLevel`  
- `EntityLevel`  
- `CausalLevel`  
- `EpisodicLevel`  

These layers allow the system to reason about structure, causality, and long‑term patterns.

---

### **⚠️ Anomaly Detection**
Detects issues such as:

- High collision rates  
- Rapid memory loss  
- Missing entity deaths  
- Unbalanced narrative distribution  

Uses `MetaCognitionAnomalyType` to avoid namespace conflicts with existing enums.

---

## **Development Methodology — RDF**
This project is built using the **Recursive Development Framework**, a methodology emphasizing:

- Recursion as the engine of progress  
- Early validation  
- Solving problems before they exist  
- Architecture‑first design  
- Documentation as a structural component  
- Quality over velocity  
- Coherence as the primary metric  

RDF ensures the system evolves cleanly without accumulating technical debt.

---

## **Project Structure**
```
/src
    /CognitiveMemory
    /ProgramCognition
    /Entities
    /Narratives
    /Detectors

/docs
    Phase-6.5-API-Audit.md
    Architecture.md
    RDF-Philosophy.md
    Issues-v0.6.9.md

/tests
    CognitiveEngine.Tests
```

---

## **Phase Status**
| Phase | Description | Status |
|-------|-------------|--------|
| 6.0 | Memory & narrative foundations | Complete |
| 6.1–6.4 | Entity tracking, trajectories, layers | Complete |
| **6.5** | **ProgramCognition (attention, anomalies, meta‑narratives)** | **Complete** |
| 6.6 | Observer voice modulation | Upcoming |
| 7.x | Higher‑order cognition | Planned |

---

## **Getting Started**

### **Requirements**
- .NET 8.0 or later  
- Visual Studio 2022 or JetBrains Rider  

### **Build**
Clone the repository and build the solution:

```bash
git clone https://github.com/YOUR_USERNAME/YOUR_REPO.git
cd YOUR_REPO
dotnet build
```

### **Run Tests**
```bash
dotnet test
```

---

## **License**
Choose a license (MIT recommended for open collaboration).

---

## **Contributions**
Contributions are welcome once the public API stabilizes.  
Please open an issue before submitting a pull request.

---

## **Acknowledgments**
This project is built using the Recursive Development Framework (RDF), a methodology emphasizing clarity, recursion, and architectural integrity.

Just tell me the style you want.

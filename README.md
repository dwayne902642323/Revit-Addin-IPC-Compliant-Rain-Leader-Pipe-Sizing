Storm Pipe Sizer – Revit C# Add-In

📌 Description

The Storm Pipe Sizer is a fully automated Revit C# add-in that sizes stormwater drainage piping per the International Plumbing Code (IPC). Built for MEP engineers, BIM professionals, and plumbing designers, this tool ensures all storm pipes are compliant with:
	•	IPC Table 1106.2 – Horizontal storm drain sizing based on GPM and slope
	•	IPC Table 1106.3 – Vertical leader sizing based on GPM only
	•	IPC §1101.7 – No reductions in pipe size in the direction of flow

It processes all storm pipes in the Revit model, calculates required diameters from first principles, and updates pipe sizes directly within the model—maintaining compliance with the most fundamental drainage design rules in the IPC.

⸻

🛠️ What It Does

This add-in automates the following tasks:
	•	✅ Scans all storm drainage pipes (typically classified as PipeSystemType.Global)
	•	✅ Determines pipe orientation based on actual slope, not just angles
	•	✅ Sizes each pipe using the correct IPC table:
	•	Table 1106.2 for horizontal drains, factoring in slope and flow
	•	Table 1106.3 for vertical leaders, using flow only
	•	✅ Sorts pipes by elevation to simulate downstream flow
	•	✅ Enforces no downstream reductions in diameter (IPC §1101.7)
	•	✅ Applies new diameters directly using Revit’s API
	•	✅ Displays confirmation to the user upon completion

⸻

🔍 Why This Matters

Manual storm pipe sizing is time-consuming, error-prone, and often requires back-referencing IPC tables. This add-in:
	•	Ensures uniform, enforceable code compliance
	•	Eliminates manual lookup errors
	•	Enables bulk QA-ready sizing across the entire model
	•	Bridges the gap between design automation and code enforcement

⸻

🧠 Engineering Logic

🔹 Horizontal Pipe Sizing (IPC Table 1106.2)

Looks up flow (in GPM) and matches to the required diameter based on pipe slope. Two common slopes are supported:
	•	0.0104 (1/8 in/ft)
	•	0.0208 (1/4 in/ft)

 🔹 Vertical Leader Sizing (IPC Table 1106.3)

Applies a simple flow threshold-to-diameter conversion using IPC-provided GPM limits for 2” to 12” pipes.

🔹 Flow Direction Management (IPC §1101.7)

Prevents downstream reductions in diameter by tracking the maximum upstream pipe size and enforcing it as a floor throughout traversal.

⸻

⚙️ Technical Details
	•	Language: C# (.NET Framework)
	•	Platform: Revit 2024+
	•	API: Revit ExternalCommand
	•	Input: RBS_PIPE_FLOW_PARAM (pipe flow in internal units → converted to GPM)
	•	Output: RBS_PIPE_DIAMETER_PARAM updated directly via Revit API
	•	No external dependencies

⸻

📘 IPC Code References Implemented

### 📘 IPC Code References Implemented

| IPC Reference    | Description                                        | Code Function                   |
|------------------|----------------------------------------------------|---------------------------------|
| **Table 1106.2** | Flow/slope sizing for horizontal drains            | `GetDiameterFrom1106_2()`       |
| **Table 1106.3** | Flow-based vertical leader sizing                  | `GetDiameterFrom1106_3()`       |
| **§1101.7**      | No pipe size reductions in flow direction          | `Execute()` diameter control    |
| **§704.1**       | Minimum slope guidelines (used for fallback)       | Implicit in slope safety logic  |

🧩 Integration Tips
	•	Pairs well with roof drain families that calculate flow rate from 1. rainfall rate and 2. roof area as inputs to the family connector:
  
  GPM = Area (ft²) × Rainfall (in/hr) × 0.0104
  
Can be extended to support:
	•	IPC Table 1106.1 (Rainfall rates)
	•	Multiple storm systems (overflow, secondary)
	•	CSV export and pipe QA reports

⸻

👨‍💻 About the Developer

Derek Eubanks, PE
Licensed Mechanical Engineer | Georgia Tech MSCS Candidate (Machine Learning)
Specializing in Revit API development, advanced MEP workflows, and standards-compliant automation.
I build tools that encode engineering logic into software—turning code books into BIM-native automation.

 

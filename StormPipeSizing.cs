// Revit ExternalCommand: Storm Pipe Sizer per IPC
// Author: Derek Eubanks, PE | GT MSCS
// Applies IPC Tables 1106.2 (Horizontal) and 1106.3 (Vertical)
// Enforces IPC §1101.7: No downstream pipe size reductions

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;

namespace StormPipeSizing
{
    [Transaction(TransactionMode.Manual)]
    public class SizeStormPipes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Step 1: Collect all storm pipes (PipeSystemType.Global assumed)
            var pipes = new FilteredElementCollector(doc)
                .OfClass(typeof(Pipe))
                .Cast<Pipe>()
                .Where(p => p.PipeSystemType == PipeSystemType.Global)
                .OrderByDescending(p => GetPipeZElevation(p)) // Step 4: top-down flow direction
                .ToList();

            double maxUpstreamDiameter = 0.0; // Track largest pipe seen upstream (feet)

            using (Transaction tx = new Transaction(doc, "Size Storm Pipes per IPC"))
            {
                tx.Start();

                foreach (Pipe pipe in pipes)
                {
                    double flowGPM = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_FLOW_PARAM).AsDouble() * 448.831;
                    if (flowGPM < 0.01) continue;

                    double slope = GetPipeSlope(pipe);

                    // Step 3: Use IPC Table 1106.3 for vertical leaders, Table 1106.2 for horizontal
                    bool isVertical = slope >= 1.0; // ~12 in/ft or steeper

                    double requiredDiameterIn = isVertical
                        ? GetDiameterFrom1106_3(flowGPM)
                        : GetDiameterFrom1106_2(flowGPM, slope);

                    // Step 5: Enforce IPC §1101.7 – no downstream reductions
                    double finalDiameterFt = Math.Max(requiredDiameterIn / 12.0, maxUpstreamDiameter);

                    Parameter diaParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                    if (diaParam != null && diaParam.StorageType == StorageType.Double)
                        diaParam.Set(finalDiameterFt);

                    maxUpstreamDiameter = Math.Max(maxUpstreamDiameter, finalDiameterFt);
                }

                tx.Commit();
            }

            TaskDialog.Show("Storm Pipe Sizer", "Storm pipes sized using IPC Tables 1106.2 (horizontal) and 1106.3 (vertical). No size reductions were allowed per IPC §1101.7.");
            return Result.Succeeded;
        }

        private double GetPipeZElevation(Pipe pipe)
        {
            var curve = (pipe.Location as LocationCurve)?.Curve;
            return curve == null ? 0.0 : Math.Max(curve.GetEndPoint(0).Z, curve.GetEndPoint(1).Z);
        }

        private double GetPipeSlope(Pipe pipe)
        {
            // Try slope parameter first (in ft/ft)
            Parameter slopeParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_SLOPE_PARAM);
            if (slopeParam != null && slopeParam.StorageType == StorageType.Double && slopeParam.AsDouble() > 0.0001)
                return slopeParam.AsDouble();

            // Fallback: calculate slope from geometry
            LocationCurve curve = pipe.Location as LocationCurve;
            if (curve == null) return 0.01;
            XYZ start = curve.Curve.GetEndPoint(0);
            XYZ end = curve.Curve.GetEndPoint(1);
            double rise = Math.Abs(end.Z - start.Z);
            double run = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            return run == 0 ? 0.01 : rise / run;
        }

        // IPC Table 1106.2 — Horizontal leader sizing by flow and slope
        private double GetDiameterFrom1106_2(double gpm, double slope)
        {
            var sizingTable = new List<(double slope, List<(double gpm, double diaIn)>>)
            {
                (0.0104, new List<(double, double)> {
                    (79, 3), (111, 4), (237, 5), (387, 6), (1111, 8), (2400, 10), (3879, 12)
                }),
                (0.0208, new List<(double, double)> {
                    (111, 3), (156, 4), (331, 5), (541, 6), (1447, 8), (3150, 10), (5095, 12)
                })
            };

            foreach (var row in sizingTable)
            {
                if (Math.Abs(slope - row.slope) < 0.001)
                {
                    foreach (var (limitGPM, diaIn) in row.Item2)
                    {
                        if (gpm <= limitGPM)
                            return diaIn;
                    }
                    return row.Item2.Last().diaIn;
                }
            }

            return 4; // fallback
        }

        // IPC Table 1106.3 — Vertical leader sizing by flow rate only
        private double GetDiameterFrom1106_3(double gpm)
        {
            var table = new List<(double gpm, double diaIn)>
            {
                (96, 2), (163, 3), (344, 4), (736, 5), (1208, 6), (2568, 8), (5312, 10), (8650, 12)
            };

            foreach (var (limitGPM, diaIn) in table)
            {
                if (gpm <= limitGPM)
                    return diaIn;
            }

            return 12; // fallback
        }
    }
}
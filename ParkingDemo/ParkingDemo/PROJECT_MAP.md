# ParkingDemo project map

Updated: 2026-09-18. This is persistent project context requested by the user.

ParkingDemo is a C# Rhino/Grasshopper plugin for rule-based, matrix-driven parking layout generation, scoring, visualization, baking, and PNG export, developed for Fateme's thesis. The user confirmed that `GHFile/ExampleFile.gh` is the main working Grasshopper definition. Binary Grasshopper/Rhino documents were inventoried, not opened; their internal wiring and geometry have not been verified.

The repository root is two directories above this document. The tree below covers its project files; generated build/IDE files are listed separately. Git's internal `.git` database is omitted because it stores repository history and metadata rather than project implementation.

`[excluded]` means explicitly excluded from compilation by the active `ParkingDemo.csproj`; it does not mean deleted. Asset descriptions use resource references and filenames, not visual inspection.

```text
ParkingDemo/                                      Repository root
|-- README.md                                    Project overview, thesis context, features, installation, and usage instructions.

`-- ParkingDemo/
    |-- .gitignore                               Excludes IDE state, build outputs, user settings, packages, and caches from Git.
    |-- ParkingDemo.sln                          Visual Studio solution containing the ParkingDemo project and build configurations.
    `-- ParkingDemo/                             C# project root and current workspace
        |-- AGENTS.md                            Directs future Codex tasks to this saved project context.
        |-- PROJECT_MAP.md                       This annotated file inventory, build notes, and project workflow.
        |-- ParkingDemo.csproj                   Builds a .NET Framework 4.8.1/C# 8 Grasshopper .gha with component exclusions and a deployment step.
        |-- ParkingDemo - Backup.csproj          Older .NET Framework 4.8 project configuration without the current component exclusions.
        |-- ParkingDemo.csproj.user              Local Visual Studio project settings; currently an empty property group.
        |-- ParkingDemoInfo.cs                   Registers the Grasshopper assembly name and GUID; descriptive author fields are empty.
        |-- Component/
        |   |-- Analyze/
        |   |   |-- DeconstrucParking.cs          Outputs a Parking object's geometry, matrix, score, ramp data, counts, and path metrics.
        |   |   |-- DeconstructGenerationCollection.cs  Outputs collection IDs/scores and exports a score-ranked CSV plus PNG chart through one button and save dialog.
        |   |   |-- GetPathLength.cs             [excluded] Outputs maximum and average entrance-to-lot traversal grades.
        |   |   |-- SelectParking.cs             Selects a Parking object from a generation collection by its GUID.
        |   |   `-- SetOptimizationWeights.cs    [excluded] Recalculates collection scores using supplied optimization weights.
        |   |-- Export/
        |   |   `-- ExportImage.cs               Triggers numbered PNG exports through ParkingPreview when Reset Preview is true.
        |   |-- Generation/
        |   |   |-- ColumnGenerator.cs           [excluded] Computes horizontal and vertical column-grid coordinates while avoiding circulation.
        |   |   |-- ParkingPathConnection.cs     [excluded] Connects a Parking object's paths and recalculates its score.
        |   |   |-- ParkingPathsTest.cs          [excluded] Small diagnostic component constructing path/cell objects and outputting a string.
        |   |   |-- PathConnection.cs            [excluded] Evaluates the feasibility and parking-lot gain of bridging two matrix cells.
        |   |   `-- SortCollection.cs            Generates and connects paths, evaluates layouts, stores valid results, and sorts by descending score.
        |   |-- Preview/
        |   |   |-- BakeParkingPreview.cs        [excluded] Older button-driven baker for car blocks, path-cell outlines, and path lines.
        |   |   |-- BakeParkingResult.cs         Bakes cars, graded cells, paths, exclusions, entrance, and walls onto Rhino layers.
        |   |   |-- PreviewParking.cs            [excluded] Earlier live viewport preview component with custom display controls.
        |   |   `-- PreviewParkingResult.cs      Live parking preview with visibility controls, car-block display, and a bake button.
        |   `-- Start/
        |       |-- GeneralRampInformation.cs    Outputs predefined ramp-type points and ramp-orientation transforms.
        |       |-- StartGeneration.cs           [excluded] Earlier CreateGrid component preparing a Parking object from outline, exclusions, and ramp settings.
        |       `-- StartGenerationAdv.cs        Prepares the parking grid/entrance and schedules repeated generation with ramp, side, and interval settings.
        |-- GUI/
        |   `-- StartGenerationAttributes.cs     Draws and handles the advanced generator's ramp, entrance-side, interval, start, and stop controls.
        |-- Utils/
        |   |-- ColumnGrid.cs                    Calculates eligible column positions, circulation exclusions, and structural grid spacing.
        |   |-- GenerationCollection.cs          Stores a list of generated Parking objects.
        |   |-- Optimization.cs                  Scores layouts using lot count, average travel grade, and turns, normalized by accessible cells.
        |   |-- Parking.cs                       Central layout model holding cells, paths, cars, scores, cell size, existing route metrics, and preview geometry.
        |   |-- ParkingExportRow.cs              Takes score-ranked scalar snapshots and formats CSV rows with model-unit distances and route metrics.
        |   |-- ParkingPreview.cs                Creates/reuses a Rhino top-view layout and saves its preview as a numbered PNG.
        |   |-- ParkingResultsExporter.cs        Saves a ranked CSV and PNG with four metric charts plus a six-axis parallel coordinates chart, without overwriting files.
        |   |-- ParkingUtils.cs                  Core grid/matrix, boundary, ramp-placement, pathfinding, cell/path-model, and empty-cell utilities.
        |   |-- ParkingVisualizationBuilder.cs   Alternative geometry/color builder for cells, paths, and walls; no callers found in current source.
        |   |-- PathConnection.cs                Implements mainPathConnection to choose and build bridges between paths using feasibility and lot gain.
        |   |-- PathFinderPro.cs                 Unfinished pathfinder placeholder that only reads a parking matrix and current start cell.
        |   |-- PathLength.cs                    Traverses paths from the entrance to compute grades, turns, car transforms, and graded cells.
        |   |-- VerticalAccess.cs                Finds adjacent circulation sides for a vertical-access cell and chooses an orientation/transform.
        |   `-- Display/
        |       |-- BakeResultsUtils.cs          Rhino baking and geometry helpers for colored cells, continuous path ribbons, entrance, and walls.
        |       |-- ParkingDisplayData.cs        Alternative sectioned display-data containers with colors, geometry, car data, and clipping bounds; no external callers found.
        |       |-- ParkingPreviewGeometry.cs    Holds geometry/color pairs for cached cells, exclusions, paths, entrance, and walls.
        |       `-- ParkingPreviewGeometryBuilder.cs  Builds and attaches preview/bake geometry to a Parking object without adding Rhino objects.
        |-- Tests/
        |   `-- ParkingExportTests.cs.test       Standalone CSV/chart regression harness using a scalar Parking stub; excluded from plugin compilation by its extension.
        |-- Properties/
        |   |-- launchSettings.json              Configures debugging to launch the locally installed Rhino 8 executable.
        |   |-- Resources.resx                   Maps component resource names to image files for embedding in the assembly.
        |   |-- Resources.Designer.cs            Generated strongly typed accessors for embedded component icons.
        |   |-- Settings.settings                Application-settings schema, currently without defined settings.
        |   `-- Settings.Designer.cs             Generated application-settings wrapper and default instance.
        |-- Resources/
        |   |-- BakeParkin.ico                   Alternate bake icon file, not referenced by the current Resources.resx.
        |   |-- BakeParking.png                  Embedded icon used by the bake-result component and an excluded preview component.
        |   |-- ColumnGenerator.png              Embedded icon for the excluded column-grid component.
        |   |-- DeconstrucParking.png            Alternate deconstruction icon asset, not referenced by current Resources.resx.
        |   |-- DeconstructParking.png           Embedded icon for the parking-deconstruction component.
        |   |-- DeconstructParkingCollection .png  Embedded collection-deconstruction icon; its filename includes a space before .png.
        |   |-- DeconstructParkingCollection.png Alternate collection-deconstruction icon, not referenced by current Resources.resx.
        |   |-- ExportImage.png                  Embedded icon for the PNG-export component.
        |   |-- ParkingCollection.png            Embedded icon for the generation-and-sorting component.
        |   |-- ParkingGenerator.png             Generator icon asset, not referenced by current Resources.resx.
        |   |-- ParkingSolver.png                Embedded icon for StartGenerationAdv.
        |   |-- preview.png                      Embedded icon for PreviewParkingResult.
        |   |-- ramp1.png                        Alternate ramp icon asset, not referenced by current Resources.resx.
        |   |-- RampInfo.png                     Embedded icon for the ramp-information component.
        |   |-- selectP.png                      Alternate selection icon asset, not referenced by current Resources.resx.
        |   |-- SelectParking.png                Embedded icon for selecting a parking result by ID.
        |   |-- start.png                        Alternate start icon asset, not referenced by current Resources.resx.
        |   `-- StartGeneration.png              Embedded icon for the excluded original start-generation component.
        `-- GHFile/
            |-- ExampleFile.gh                  Main working Grasshopper definition, confirmed by the user.
            |-- TestForOutput.gh                Grasshopper definition whose name suggests output testing; exact contents unverified.
            |-- 3DBase.3dm                      Rhino model supplied with the Grasshopper files; internal geometry unverified.
            |-- 3DBase.3dmbak                   Backup copy of the 3DBase Rhino model.
            |-- 3DBase.3dm.rhl                  Rhino lock/ownership sidecar for the 3DBase model.
            `-- 3DBase_embedded_files/
                |-- Clipboard-336de33a-07a6-42a9-adbe-3db7705abdbc.png  Extracted/associated clipboard image for 3DBase; visual content unverified.
                `-- Clipboard-c67d4c1a-3692-4e28-9250-9a26c9f4b328.png  Extracted/associated clipboard image for 3DBase; visual content unverified.
```

## Main code workflow

1. `StartGenerationAdv` duplicates/translates the input curves, constructs a 5-unit grid, applies exclusions and optional ramp placement, and outputs a `Parking` object. Its custom attributes control repeated solves.
2. `SortCollection` calls `ParkingUtils.PathFinder4` and `PathFinder3`, connects paths through `mainPathConnection`, computes metrics with `PathLength.GetPathLength2`, calculates scores, and accumulates valid results in descending score order.
3. Collection deconstruction and `SelectParking` expose IDs/scores and retrieve a particular result; `DeconstrucParking` exposes detailed geometry and metrics.
4. `PreviewParkingResult` builds/displays preview geometry and can bake it; `BakeParkingResult` provides dedicated baking. Cars use a referenced Rhino block definition and the layout's car transforms.
5. `ExportImage` calls `ParkingPreview.PrintResult` to save the Rhino layout preview as a PNG.
6. `DeconstructGenerationCollection` keeps its existing collection input and ID/score outputs; its Export CSV + chart button saves a ranked CSV and a PNG with four metric charts plus a parallel coordinates chart using a save dialog, without triggering another solve. Ordinary solves only refresh the snapshot and never write export files.

This describes code responsibilities, not verified wiring inside ExampleFile.gh.

## Details to remember

- The active project targets net481 and references Grasshopper package 7.13.21348.13001; the debug launch profile opens Rhino 8.
- The post-build target copies the .gha into a hard-coded user Grasshopper Libraries folder and erases the original build output. Running a build can therefore deploy the plugin.
- Nine component source files are excluded from the active build; the backup project has different settings.
- Stored path-length metrics are traversal grades/cell steps; export multiplies them by Parking.CellSize (currently 5) to report Rhino model-unit distances.
- User-confirmed metric definitions: LotNumber is the parking count; TotalDirShift / (double)LotNumber is average turns; PathDirectionShift is the existing value to export as maximum turns; TotalLengthGrade / (double)LotNumber * CellSize is average physical path length; MaxLengthGrade * CellSize is maximum physical path length; Score is the stored ranking score. Reuse these fields rather than introducing another turn-tracking calculation.
- Distances are in model units (meters when the model uses meters). CSV headings explicitly say ModelUnits, and the PNG labels the active Rhino model unit system. Zero-lot rows leave route metrics blank to avoid dividing by zero.
- The results export uses an immutable scalar snapshot, stable descending score order, invariant CSV numbers, and four metric charts of count, score, average path, and average turns. The same PNG also includes a six-axis parallel coordinates chart (count, average/max distance, average/max turns, score): each axis is independently normalized, original-unit ticks remain visible, better values are at the top, and the top five finite-score options have distinct colors and CSV-rank legend labels. Other options are translucent gray; constant axes use the midpoint and missing values break lines. Existing output files are never overwritten.
- Optimization exposes NonFuncW, but the current scoring formula does not use it.
- ParkingPreview's resetPlan argument is unused inside the helper; ExportImage uses that input as its export trigger.
- ParkingPreview saves GetPreviewImage output, not the separately created ViewCapture bitmap; it modifies the first Rhino layout/detail and concatenates folder/filename directly.
- ParkingVisualizationBuilder and ParkingDisplayData have no callers elsewhere in the inspected source; do not assume they are the active display pipeline.
- Tests/ParkingExportTests.cs.test exercises the actual CSV/chart helpers without Rhino; ParkingPathsTest is a separate excluded diagnostic Grasshopper component.
- Recheck source when making changes: this map is a dated snapshot, not a replacement for reading current code.

## Generated build and IDE files

The following inventory includes each existing file under bin, obj, and the solution's .vs directory. Descriptions reflect generated file roles; caches and binary contents were not inspected. These files can change after a build or IDE session.

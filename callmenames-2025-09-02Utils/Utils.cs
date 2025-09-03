using VL.Lib.Collections;
using Stride.Core.Mathematics; // Wichtig: Diese using-Anweisung muss vorhanden sein

namespace Main;

public static class Utils
{
    public static float DemoNode(float a, float b)
    {
        return a + b;
    }

    /// <summary>
    /// Erstellt eine Spread von Transformationsmatrizen aus den gegebenen Translationen, Rotationen und Skalierungen.
    /// </summary>
    /// <param name="translations">Ein Spread von Vektoren, die die Position (Translation) definieren.</param>
    /// <param name="rotations">Ein Spread von Vektoren, die die Rotation als Euler-Winkel (Pitch, Yaw, Roll) im Bogenmaß definieren.</param>
    /// <param name="scales">Ein Spread von Vektoren, die die Skalierung entlang der X-, Y- und Z-Achse definieren.</param>
    /// <returns>Ein Spread von kombinierten SRT-Transformationsmatrizen.</returns>
    public static Spread<Matrix> CreateTransformsSRT(Spread<Vector3> translations, Spread<Vector3> rotations, Spread<Vector3> scales)
    {
        var spreadBuilder = new SpreadBuilder<Matrix>();

        // Finde die maximale Anzahl von Slices, um sicherzustellen, dass wir nicht außerhalb der Grenzen zugreifen.
        // Der Modulo-Operator (%) sorgt dafür, dass kleinere Spreads wiederholt werden (vvvv-Verhalten).
        int sliceCount = Math.Max(translations.Count, Math.Max(rotations.Count, scales.Count));

        for (int i = 0; i < sliceCount; i++)
        {
            // 1. Hole die Transformationskomponenten für den aktuellen Slice
            Vector3 t = translations[i % translations.Count];
            Vector3 r = rotations[i % rotations.Count];
            Vector3 s = scales[i % scales.Count];

            // 2. Erstelle die einzelnen Matrizen
            Matrix scaleMatrix = Matrix.Scaling(s);
            Matrix rotationMatrix = Matrix.RotationYawPitchRoll(r.Y, r.X, r.Z);
            Matrix translationMatrix = Matrix.Translation(t);

            // 3. Kombiniere die Matrizen in der korrekten Reihenfolge: Scale * Rotation * Translation
            Matrix finalMatrix = scaleMatrix * rotationMatrix * translationMatrix;

            // 4. Füge die fertige Matrix dem Builder hinzu
            spreadBuilder.Add(finalMatrix);
        }

        return spreadBuilder.ToSpread();
    }

    // We use a helper class here to store the builder, 
    // as C# doesn't allow passing a SpreadBuilder as a 'ref' parameter in this context.
    public class MatrixBuilderWrapper
    {
        public SpreadBuilder<Matrix> Builder = new SpreadBuilder<Matrix>();
    }

    // This is the main function you call from vvvv.
    public static Spread<Matrix> Fractalizer(
        Spread<Matrix> childRules,
        Matrix placementTransform,
        int depth)
    {
        // A wrapper to hold our builder, which will collect all matrices.
        var wrapper = new MatrixBuilderWrapper();

        // The recursion starts from the world origin, represented by the Identity matrix.
        RecursiveGenerate(Matrix.Identity, childRules, placementTransform, depth, wrapper);

        return wrapper.Builder.ToSpread();
    }

    // This is the private helper function that does the actual recursion.
    private static void RecursiveGenerate(
        Matrix basis,                          // The transformation of the parent cylinder
        Spread<Matrix> childRules,             // The set of rules for creating children
        Matrix placementTransform,             // The transform to move to the end of the parent
        int depth,                             // The remaining depth to generate
        MatrixBuilderWrapper matrixCollector)  // The object that collects ALL matrices
    {
        // 1. Base Case: If we have reached the desired depth, stop this branch.
        if (depth <= 0)
        {
            return;
        }

        // 2. Generate the children for the current 'basis'
        foreach (Matrix rule in childRules)
        {
            // This is the key multiplication, just as you described:
            // Final = Child's_Rule * Placement_At_End_Of_Parent * Parent's_Own_Transform
            Matrix newMatrix = rule * placementTransform * basis;

            // 3. Add the newly created matrix to our master list. THIS IS THE CRUCIAL STEP.
            matrixCollector.Builder.Add(newMatrix);

            // 4. Recurse: Use this new matrix as the 'basis' for the next generation.
            //    We also decrease the depth by 1.
            RecursiveGenerate(newMatrix, childRules, placementTransform, depth - 1, matrixCollector);
        }
    }
}
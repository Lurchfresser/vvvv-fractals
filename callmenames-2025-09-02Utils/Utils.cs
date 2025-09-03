namespace Main;

public static class Utils
{
    // The wrapper now holds a builder for matrices AND a builder for integers (depths).
    public class FractalDataWrapper
    {
        public SpreadBuilder<Matrix> MatrixBuilder = new SpreadBuilder<Matrix>();
        public SpreadBuilder<int> DepthBuilder = new SpreadBuilder<int>();
    }

    // The main function signature changes to use 'out' for multiple outputs.
    public static void Fractalizer(
        Spread<Matrix> childRules,
        Matrix placementTransform,
        int totalDepth, // Renamed for clarity
        out Spread<Matrix> matrices,
        out Spread<int> depths)
    {
        var wrapper = new FractalDataWrapper();

        // The recursion starts from the world origin.
        // Note: We don't add the root matrix (Identity) to the output,
        // generation starts at level 1.
        RecursiveGenerate(Matrix.Identity, childRules, placementTransform, totalDepth, totalDepth, wrapper);

        // Assign the collected data to the output pins.
        matrices = wrapper.MatrixBuilder.ToSpread();
        depths = wrapper.DepthBuilder.ToSpread();
    }

    // The recursive helper now tracks the total depth to calculate the current level.
    private static void RecursiveGenerate(
        Matrix basis,
        Spread<Matrix> childRules,
        Matrix placementTransform,
        int remainingDepth,
        int totalDepth, // Pass total depth through
        FractalDataWrapper dataCollector)
    {
        if (remainingDepth <= 0)
        {
            return;
        }

        // Calculate the current depth level (e.g., 1 for the first set, 2 for the second, etc.)
        int currentLevel = totalDepth - remainingDepth + 1;

        foreach (Matrix rule in childRules)
        {
            Matrix newMatrix = rule * placementTransform * basis;

            // *** THE KEY CHANGE ***
            // Add the matrix AND its corresponding depth level to the collectors.
            // Because we add them one after another, their order is guaranteed to match.
            dataCollector.MatrixBuilder.Add(newMatrix);
            dataCollector.DepthBuilder.Add(currentLevel);

            // Recurse for the next level.
            RecursiveGenerate(newMatrix, childRules, placementTransform, remainingDepth - 1, totalDepth, dataCollector);
        }
    }
}
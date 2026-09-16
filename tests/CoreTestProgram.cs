using System;
using System.Collections.Generic;

internal static class CoreTestProgram {
    public static int Main(string[] args) {
        try {
            var suites = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase) {
                { "Identity", IdentityTests.Run }
            };
            var requestedSuites = ParseSuites(args);

            if (requestedSuites.Count == 1 && string.Equals(requestedSuites[0], "All", StringComparison.OrdinalIgnoreCase)) {
                foreach (var suite in suites.Values) {
                    suite();
                }
            } else {
                foreach (var suiteName in requestedSuites) {
                    Action suite;
                    if (!suites.TryGetValue(suiteName, out suite)) {
                        throw new ArgumentException("Unknown suite '" + suiteName + "'.");
                    }

                    suite();
                }
            }

            Console.WriteLine("TOTAL: " + AssertEx.PassedCount + " passed");
            return 0;
        } catch (Exception exception) {
            Console.Error.WriteLine("FAIL: " + exception.Message);
            return 1;
        }
    }

    private static List<string> ParseSuites(string[] args) {
        if (args.Length != 2 || !string.Equals(args[0], "--suite", StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Expected --suite All or a comma-separated suite list.");
        }

        return new List<string>(args[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
    }
}

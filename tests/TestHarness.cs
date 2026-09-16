using System;
using System.Collections.Generic;

internal static class AssertEx {
    private static int passed;

    public static int PassedCount {
        get { return passed; }
    }

    public static void Equal<T>(T expected, T actual, string message) {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) {
            throw new InvalidOperationException(message + ": expected '" + expected + "', got '" + actual + "'.");
        }

        passed++;
    }

    public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message) {
        using (var expectedEnumerator = expected.GetEnumerator())
        using (var actualEnumerator = actual.GetEnumerator()) {
            while (true) {
                var expectedHasValue = expectedEnumerator.MoveNext();
                var actualHasValue = actualEnumerator.MoveNext();
                if (expectedHasValue != actualHasValue) {
                    throw new InvalidOperationException(message + ": sequences have different lengths.");
                }

                if (!expectedHasValue) {
                    break;
                }

                if (!EqualityComparer<T>.Default.Equals(expectedEnumerator.Current, actualEnumerator.Current)) {
                    throw new InvalidOperationException(message + ": sequences differ.");
                }
            }
        }

        passed++;
    }

    public static void True(bool condition, string message) {
        if (!condition) {
            throw new InvalidOperationException(message + ": expected true.");
        }

        passed++;
    }

    public static void False(bool condition, string message) {
        if (condition) {
            throw new InvalidOperationException(message + ": expected false.");
        }

        passed++;
    }

    public static void Near(double expected, double actual, double tolerance, string message) {
        if (double.IsNaN(expected) || double.IsInfinity(expected) ||
            double.IsNaN(actual) || double.IsInfinity(actual) ||
            double.IsNaN(tolerance) || double.IsInfinity(tolerance) || tolerance < 0.0) {
            throw new ArgumentOutOfRangeException("tolerance", message + ": expected, actual, and tolerance must be finite; tolerance must be non-negative.");
        }

        if (Math.Abs(expected - actual) > tolerance) {
            throw new InvalidOperationException(message + ": expected '" + expected + "' +/- " + tolerance + ", got '" + actual + "'.");
        }

        passed++;
    }

    public static void Throws<TException>(Action action, string message) where TException : Exception {
        try {
            action();
        } catch (TException) {
            passed++;
            return;
        }

        throw new InvalidOperationException(message + ": expected " + typeof(TException).Name + ".");
    }
}

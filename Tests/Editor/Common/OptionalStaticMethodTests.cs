using System;
using dev.limitex.avatar.compressor.editor;
using NUnit.Framework;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal class OptionalStaticMethodTests
    {
        // The target type is resolved by its full name string, exactly like an external package's
        // type would be — the test assembly plays the role of the optional package.
        private const string TargetTypeName =
            "dev.limitex.avatar.compressor.tests.OptionalStaticMethodTestTarget";

        [SetUp]
        public void SetUp()
        {
            OptionalStaticMethodTestTarget.CallCount = 0;
        }

        [Test]
        public void Status_Resolved_WhenMethodExists()
        {
            var method = new OptionalStaticMethod(TargetTypeName, "Echo", typeof(string));

            Assert.That(method.Status, Is.EqualTo(OptionalStaticMethod.ResolutionStatus.Resolved));
            Assert.That(method.IsAvailable, Is.True);
        }

        [Test]
        public void Status_TypeNotFound_WhenTypeIsMissing()
        {
            var method = new OptionalStaticMethod("No.Such.Namespace.NoSuchType", "Echo");

            Assert.That(
                method.Status,
                Is.EqualTo(OptionalStaticMethod.ResolutionStatus.TypeNotFound)
            );
            Assert.That(method.IsAvailable, Is.False);
        }

        [Test]
        public void Status_MethodNotFound_WhenNameIsMissing()
        {
            var method = new OptionalStaticMethod(TargetTypeName, "NoSuchMethod");

            Assert.That(
                method.Status,
                Is.EqualTo(OptionalStaticMethod.ResolutionStatus.MethodNotFound)
            );
            Assert.That(method.IsAvailable, Is.False);
        }

        [Test]
        public void Status_MethodNotFound_WhenSignatureDiffers()
        {
            var method = new OptionalStaticMethod(TargetTypeName, "Echo", typeof(int));

            Assert.That(
                method.Status,
                Is.EqualTo(OptionalStaticMethod.ResolutionStatus.MethodNotFound)
            );
            Assert.That(method.IsAvailable, Is.False);
        }

        [Test]
        public void Constructor_NoParameterTypes_ResolvesParameterlessMethod()
        {
            var method = new OptionalStaticMethod(TargetTypeName, "NoArgs");

            Assert.That(method.Status, Is.EqualTo(OptionalStaticMethod.ResolutionStatus.Resolved));
        }

        [Test]
        public void TryInvoke_InvokesMethod_AndReturnsResult()
        {
            var method = new OptionalStaticMethod(TargetTypeName, "Echo", typeof(string));

            bool invoked = method.TryInvoke(
                new object[] { "hello" },
                out object result,
                out Exception error
            );

            Assert.That(invoked, Is.True);
            Assert.That(result, Is.EqualTo("hello"));
            Assert.That(error, Is.Null);
            Assert.That(OptionalStaticMethodTestTarget.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void TryInvoke_ReturnsFalse_WithNullError_WhenUnavailable()
        {
            var method = new OptionalStaticMethod("No.Such.Namespace.NoSuchType", "Echo");

            bool invoked = method.TryInvoke(
                Array.Empty<object>(),
                out object result,
                out Exception error
            );

            Assert.That(invoked, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryInvoke_OnException_SurfacesInnerException_AndDisables()
        {
            var method = new OptionalStaticMethod(TargetTypeName, "Throw", typeof(string));

            bool invoked = method.TryInvoke(
                new object[] { "boom" },
                out object result,
                out Exception error
            );

            Assert.That(invoked, Is.False);
            Assert.That(result, Is.Null);
            // The TargetInvocationException wrapper is unwrapped so callers can log the real cause.
            Assert.That(error, Is.TypeOf<InvalidOperationException>());
            Assert.That(error.Message, Is.EqualTo("boom"));
            Assert.That(method.IsAvailable, Is.False);
        }

        [Test]
        public void TryInvoke_AfterFailure_StaysDisabled_AndDoesNotInvoke()
        {
            var method = new OptionalStaticMethod(TargetTypeName, "Throw", typeof(string));
            method.TryInvoke(new object[] { "boom" }, out _, out _);

            bool invoked = method.TryInvoke(new object[] { "again" }, out _, out Exception error);

            Assert.That(invoked, Is.False);
            Assert.That(error, Is.Null, "a disabled method must not re-invoke or re-report");
            // Status keeps the resolution result; only availability reflects the runtime failure.
            Assert.That(method.Status, Is.EqualTo(OptionalStaticMethod.ResolutionStatus.Resolved));
        }
    }

    /// <summary>
    /// Stand-in for an optional external package's static API, looked up by full type name.
    /// </summary>
    internal static class OptionalStaticMethodTestTarget
    {
        public static int CallCount;

        public static string Echo(string value)
        {
            CallCount++;
            return value;
        }

        public static int NoArgs()
        {
            return 42;
        }

        public static void Throw(string message)
        {
            throw new InvalidOperationException(message);
        }
    }
}

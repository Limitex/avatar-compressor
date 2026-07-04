using System;
using System.Reflection;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// A public static method on an optional, external package, resolved once by reflection so the
    /// project compiles and runs whether or not that package is installed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the shared mechanism for integrations with packages that cannot be compile-time
    /// dependencies (see <c>Editor/TextureCompressor/Integrations</c>). Reflection is deliberate:
    /// asmdef Version Defines only react to packages registered in a manifest, while packages such
    /// as lilToon are also distributed as <c>.unitypackage</c> straight into <c>Assets/</c>, and a
    /// change in the external API must degrade to a skipped feature instead of a compile error in
    /// the user's project.
    /// </para>
    /// <para>
    /// <see cref="Status"/> reports how resolution went, letting callers distinguish "package not
    /// installed" (expected: stay silent) from "package installed but incompatible" (surprising:
    /// warn the user). A failed invocation disables the method for the lifetime of this instance
    /// (<see cref="IsAvailable"/> becomes false) so a broken external API degrades to a single
    /// warning rather than throwing at every call site — callers typically create one instance
    /// per build, scoping the disable to that build.
    /// </para>
    /// </remarks>
    internal sealed class OptionalStaticMethod
    {
        internal enum ResolutionStatus
        {
            /// <summary>The declaring type does not exist: the package is not installed.</summary>
            TypeNotFound,

            /// <summary>The type exists but no method matched: incompatible package version.</summary>
            MethodNotFound,

            /// <summary>The method was resolved and can be invoked.</summary>
            Resolved,
        }

        private MethodInfo _method;

        public OptionalStaticMethod(
            string typeName,
            string methodName,
            params Type[] parameterTypes
        )
        {
            Type type = FindType(typeName);
            if (type == null)
            {
                Status = ResolutionStatus.TypeNotFound;
                return;
            }

            _method = type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: parameterTypes ?? Type.EmptyTypes,
                modifiers: null
            );
            Status = _method != null ? ResolutionStatus.Resolved : ResolutionStatus.MethodNotFound;
        }

        /// <summary>
        /// How constructor-time resolution went. Fixed at construction: a later invocation
        /// failure turns <see cref="IsAvailable"/> false but does not change this value, so it
        /// always answers "was the package there, and did its API match" rather than "is the
        /// method currently usable".
        /// </summary>
        public ResolutionStatus Status { get; }

        /// <summary>
        /// True while the method can be invoked: resolution succeeded and no invocation has
        /// failed yet.
        /// </summary>
        public bool IsAvailable => _method != null;

        /// <summary>
        /// Invokes the method. Returns false without invoking when <see cref="IsAvailable"/> is
        /// false (<paramref name="error"/> stays null). When the call itself throws, the method
        /// is disabled for the lifetime of this instance and <paramref name="error"/> carries the
        /// unwrapped exception so the caller can log it once.
        /// </summary>
        public bool TryInvoke(object[] arguments, out object result, out Exception error)
        {
            result = null;
            error = null;

            if (_method == null)
                return false;

            try
            {
                result = _method.Invoke(null, arguments);
                return true;
            }
            catch (Exception ex)
            {
                _method = null;
                error = ex.InnerException ?? ex;
                return false;
            }
        }

        /// <summary>
        /// Resolves a type from any loaded assembly, or null when the declaring package is not
        /// installed. Shared with integrations that need more than a static method from the
        /// optional package (e.g. reading a constant field).
        /// </summary>
        public static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}

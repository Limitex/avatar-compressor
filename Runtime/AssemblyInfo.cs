using System.Runtime.CompilerServices;

// The runtime types are internal so other packages cannot reference them; this
// package's own editor and test assemblies still need access.
[assembly: InternalsVisibleTo("dev.limitex.avatar-compressor.editor")]
[assembly: InternalsVisibleTo("dev.limitex.avatar-compressor.editor.tests")]

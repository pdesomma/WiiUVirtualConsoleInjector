#if !NET5_0_OR_GREATER
// Lets net48 use init-only setters and records; the runtime type is only a compiler marker.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif

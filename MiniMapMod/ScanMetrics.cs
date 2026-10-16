namespace MiniMapMod
{
    // Counters from the latest scan, printed every few seconds with LogLevel=debug.
    internal sealed class ScanMetrics
    {
        public int Invocations, RegistryCandidates, SceneCandidates, Unique, Classified, Reused;
        public int Inactive, Disabled, Duplicates, Local, Dead, PreferredInteraction, InvalidTeam, Errors;
        public bool RegistryAvailable;

        public void Begin()
        {
            Invocations++;
            RegistryAvailable = false;
            RegistryCandidates = SceneCandidates = Unique = Classified = Reused = 0;
            Inactive = Disabled = Duplicates = Local = Dead = PreferredInteraction = InvalidTeam = Errors = 0;
        }

        public override string ToString() => $"calls={Invocations} registryAvailable={RegistryAvailable} registry={RegistryCandidates}"
            + $" sceneCached={SceneCandidates} unique={Unique} classifiedNew={Classified} reused={Reused}"
            + $" filtered(inactive={Inactive},disabled={Disabled},duplicate={Duplicates},local={Local},dead={Dead},"
            + $"interaction={PreferredInteraction},invalidTeam={InvalidTeam}) errors={Errors}";
    }
}
